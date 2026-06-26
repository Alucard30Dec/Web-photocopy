using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Customer.Models;

namespace WebPhotocopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class PrintJobsController : Controller
{
    private const int MaxFilesPerBatch = 5;
    private const int MaxPagesPerFile = 10000;
    private const long MaxFileSizeBytes = 20L * 1024 * 1024;
    private const long MaxBatchUploadSizeBytes = 100L * 1024 * 1024;
    private const long MaxOfficePreviewRequestSizeBytes = MaxFileSizeBytes + (1L * 1024 * 1024);

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private readonly IPrintJobService _printJobService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOfficePreviewService _officePreviewService;
    private readonly ILogger<PrintJobsController> _logger;

    public PrintJobsController(
        IPrintJobService printJobService,
        IFileStorageService fileStorageService,
        IOfficePreviewService officePreviewService,
        ILogger<PrintJobsController> logger)
    {
        _printJobService = printJobService;
        _fileStorageService = fileStorageService;
        _officePreviewService = officePreviewService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _printJobService.GetUserOrdersAsync(User.GetUserId(), cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var vm = new CreatePrintJobViewModel
        {
            ExistingFiles = await _fileStorageService.GetFilesByOwnerAsync(User.GetUserId(), cancellationToken)
        };

        return View(vm);
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePrintJobViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        model.ExistingFileIds ??= new List<Guid>();
        model.UploadFiles ??= new List<IFormFile>();
        model.ExistingFilePageCounts ??= new Dictionary<Guid, int?>();
        model.UploadPageCounts ??= new List<int?>();
        model.UploadPageFroms ??= new List<int?>();
        model.UploadPageTos ??= new List<int?>();
        model.ExistingFiles = await _fileStorageService.GetFilesByOwnerAsync(userId, cancellationToken);

        var existingIds = model.ExistingFileIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        var uploadFiles = model.UploadFiles
            .Where(x => x is { Length: > 0 })
            .ToList();
        var requestedFileCount = existingIds.Count + uploadFiles.Count;

        if (requestedFileCount == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một tài liệu để in.");
        }

        if (requestedFileCount > MaxFilesPerBatch)
        {
            ModelState.AddModelError(string.Empty, $"Mỗi lần chỉ được gửi tối đa {MaxFilesPerBatch} tài liệu.");
        }

        if (model.DeliveryMethod == DeliveryMethod.Shipping && string.IsNullOrWhiteSpace(model.DeliveryAddress))
        {
            ModelState.AddModelError(nameof(model.DeliveryAddress), "Vui lòng nhập địa chỉ giao hàng.");
        }

        var ownedFilesById = model.ExistingFiles.ToDictionary(x => x.Id);
        var invalidExistingIds = existingIds.Where(x => !ownedFilesById.ContainsKey(x)).ToList();
        if (invalidExistingIds.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "Có file đã chọn không tồn tại hoặc không thuộc tài khoản của bạn.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var preparedFiles = new List<PreparedPrintFile>();
        var bufferedUploads = new List<BufferedUpload>();
        var createdCount = 0;

        try
        {
            foreach (var existingId in existingIds)
            {
                var metadata = ownedFilesById[existingId];
                model.ExistingFilePageCounts.TryGetValue(existingId, out var manualPageCount);
                var totalPages = await ResolveExistingPageCountAsync(metadata, manualPageCount, cancellationToken);

                if (totalPages is null)
                {
                    ModelState.AddModelError(
                        $"ExistingFilePageCounts[{existingId}]",
                        $"Vui lòng nhập số trang thực tế của file {metadata.OriginalFileName}.");
                    continue;
                }

                preparedFiles.Add(new PreparedPrintFile(
                    metadata.Id,
                    metadata.OriginalFileName,
                    totalPages.Value,
                    1,
                    totalPages.Value,
                    totalPages.Value));
            }

            long batchUploadSize = 0;
            for (var index = 0; index < uploadFiles.Count; index++)
            {
                var uploadFile = uploadFiles[index];
                var safeFileName = Path.GetFileName(uploadFile.FileName);
                var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
                var manualPageCount = index < model.UploadPageCounts.Count
                    ? model.UploadPageCounts[index]
                    : null;
                var pageFrom = index < model.UploadPageFroms.Count
                    ? model.UploadPageFroms[index]
                    : null;
                var pageTo = index < model.UploadPageTos.Count
                    ? model.UploadPageTos[index]
                    : null;

                if (string.IsNullOrWhiteSpace(safeFileName) || !AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, $"File {safeFileName} không thuộc định dạng được hỗ trợ.");
                    continue;
                }

                if (uploadFile.Length > MaxFileSizeBytes)
                {
                    ModelState.AddModelError(string.Empty, $"File {safeFileName} vượt quá giới hạn 20 MB.");
                    continue;
                }

                batchUploadSize += uploadFile.Length;
                if (batchUploadSize > MaxBatchUploadSizeBytes)
                {
                    ModelState.AddModelError(string.Empty, "Tổng dung lượng file mới vượt quá 100 MB.");
                    break;
                }

                var memory = new MemoryStream();
                await uploadFile.CopyToAsync(memory, cancellationToken);
                memory.Position = 0;

                var totalPages = ResolveBufferedPageCount(extension, memory, manualPageCount);
                if (totalPages is null)
                {
                    await memory.DisposeAsync();
                    ModelState.AddModelError(
                        nameof(model.UploadPageCounts),
                        $"Vui lòng nhập số trang thực tế của file {safeFileName}.");
                    continue;
                }

                if (!TryValidateUploadPageRange(
                        index,
                        safeFileName,
                        totalPages.Value,
                        pageFrom,
                        pageTo,
                        out var selectedPageCount))
                {
                    await memory.DisposeAsync();
                    continue;
                }

                bufferedUploads.Add(new BufferedUpload(
                    safeFileName,
                    ResolveContentType(safeFileName, uploadFile.ContentType),
                    uploadFile.Length,
                    totalPages.Value,
                    pageFrom!.Value,
                    pageTo!.Value,
                    selectedPageCount,
                    memory));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            foreach (var bufferedUpload in bufferedUploads)
            {
                bufferedUpload.Content.Position = 0;
                var uploaded = await _fileStorageService.SaveAsync(new CreateUploadedFileDto
                {
                    OwnerUserId = userId,
                    OriginalFileName = bufferedUpload.FileName,
                    ContentType = bufferedUpload.ContentType,
                    Size = bufferedUpload.Size,
                    Content = bufferedUpload.Content,
                    IsForPrintJob = true
                }, cancellationToken);

                preparedFiles.Add(new PreparedPrintFile(
                    uploaded.Id,
                    uploaded.OriginalFileName,
                    bufferedUpload.DocumentPageCount,
                    bufferedUpload.PageFrom,
                    bufferedUpload.PageTo,
                    bufferedUpload.SelectedPageCount));
            }

            for (var index = 0; index < preparedFiles.Count; index++)
            {
                var item = preparedFiles[index];
                await _printJobService.CreateAndSubmitAsync(new CreatePrintJobDto
                {
                    UserId = userId,
                    UploadedFileId = item.FileId,
                    PaperSize = model.PaperSize,
                    PrintSide = model.PrintSide,
                    ColorMode = model.ColorMode,
                    IsPhoto = model.IsPhoto,
                    Copies = model.Copies,
                    TotalPages = item.SelectedPageCount,
                    Notes = BuildPrintJobNotes(
                        model.Notes,
                        item.PageFrom,
                        item.PageTo,
                        item.DocumentPageCount),
                    IdempotencyKey = BuildItemIdempotencyKey(model.IdempotencyKey, index),
                    DeliveryMethod = model.DeliveryMethod,
                    DeliveryAddress = model.DeliveryAddress
                }, cancellationToken);

                createdCount++;
            }

            TempData["Success"] = createdCount == 1
                ? "Đã gửi đơn in. Tiệm sẽ xác nhận file trước khi trừ tiền ví."
                : $"Đã tạo {createdCount} đơn in từ {createdCount} tài liệu. Tiệm sẽ xác nhận từng file trước khi trừ tiền ví.";
            return RedirectToRoute(
                "shop-branch-customer",
                new
                {
                    branchSlug = RouteData.Values["branchSlug"]?.ToString(),
                    controller = "PrintJobs",
                    action = "Index"
                });
        }
        catch (BusinessException ex)
        {
            var message = createdCount > 0
                ? $"Đã tạo {createdCount} đơn trước khi gặp lỗi: {ex.Message} Vui lòng kiểm tra danh sách đơn trước khi gửi lại."
                : ex.Message;
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }
        finally
        {
            foreach (var bufferedUpload in bufferedUploads)
            {
                await bufferedUpload.Content.DisposeAsync();
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _printJobService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (item.UserId != User.GetUserId() && !User.IsInRole(RoleConstants.Admin))
        {
            return Forbid();
        }

        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Files(CancellationToken cancellationToken)
    {
        var files = await _fileStorageService.GetFilesByOwnerAsync(User.GetUserId(), cancellationToken);
        return View(files);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewFile(Guid id, CancellationToken cancellationToken)
    {
        var metadata = await _fileStorageService.GetMetadataAsync(id, cancellationToken);
        if (metadata is null)
        {
            return NotFound();
        }

        if (metadata.OwnerUserId != User.GetUserId() && !User.IsInRole(RoleConstants.Admin))
        {
            return Forbid();
        }

        var safeFileName = Path.GetFileName(metadata.OriginalFileName);

        if (IsOfficeFile(safeFileName))
        {
            await using var sourceStream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
            return await BuildOfficePreviewFileResultAsync(sourceStream, safeFileName, cancellationToken);
        }

        var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
        var contentType = ResolveContentType(safeFileName, metadata.ContentType);
        ApplyInlinePreviewHeaders(safeFileName);
        return File(stream, contentType, enableRangeProcessing: true);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxOfficePreviewRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxOfficePreviewRequestSizeBytes)]
    public IActionResult ReadUploadPageCount(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn file cần đọc số trang." });
        }

        var safeFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(safeFileName) || !AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Định dạng file không được hỗ trợ." });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = "File vượt quá giới hạn 20 MB."
            });
        }

        if (ImageExtensions.Contains(extension))
        {
            return Json(new { pageCount = 1 });
        }

        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "File Office được đọc số trang qua tiến trình chuẩn bị bản xem trước."
            });
        }

        using var stream = file.OpenReadStream();
        var pageCount = _fileStorageService.TryGetPdfPageCount(stream);
        if (!IsValidPageCount(pageCount))
        {
            return BadRequest(new
            {
                message = "Không đọc được số trang PDF. Hãy kiểm tra lại file hoặc xuất lại PDF."
            });
        }

        return Json(new { pageCount = pageCount!.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxOfficePreviewRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxOfficePreviewRequestSizeBytes)]
    public async Task<IActionResult> PreviewOfficeUpload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn một file Office để xem trước." });
        }

        var safeFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(safeFileName) || !_officePreviewService.IsSupportedExtension(extension))
        {
            return BadRequest(new { message = "Chỉ hỗ trợ xem trước Word, Excel và PowerPoint." });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = "File vượt quá giới hạn 20 MB."
            });
        }

        try
        {
            await using var sourceStream = file.OpenReadStream();
            var preview = await _officePreviewService.ConvertToPdfAsync(
                sourceStream,
                safeFileName,
                cancellationToken);

            using var pdfStream = new MemoryStream(preview.PdfBytes, writable: false);
            var pageCount = _fileStorageService.TryGetPdfPageCount(pdfStream);
            if (IsValidPageCount(pageCount))
            {
                Response.Headers["X-Preview-Page-Count"] = pageCount!.Value.ToString();
            }

            ApplyInlinePreviewHeaders(Path.ChangeExtension(safeFileName, ".pdf"));

            return File(preview.PdfBytes, "application/pdf");
        }
        catch (OfficePreviewUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (TimeoutException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                message = "Quá thời gian chuyển đổi file Office. Hãy thử lại hoặc lưu file thành PDF."
            });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(
                ex,
                "Office preview conversion failed for {FileName}. CorrelationId: {CorrelationId}",
                safeFileName,
                correlationId);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = $"Không thể tạo bản xem trước file Office. Mã theo dõi: {correlationId}."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        var metadata = await _fileStorageService.GetMetadataAsync(id, cancellationToken);
        if (metadata is null)
        {
            return NotFound();
        }

        if (metadata.OwnerUserId != User.GetUserId() && !User.IsInRole(RoleConstants.Admin))
        {
            return Forbid();
        }

        var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
        var safeFileName = Path.GetFileName(metadata.OriginalFileName);
        return File(stream, ResolveContentType(safeFileName, metadata.ContentType), safeFileName);
    }

    private async Task<int?> ResolveExistingPageCountAsync(
        UploadedFileMetadata metadata,
        int? manualPageCount,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(metadata.OriginalFileName).ToLowerInvariant();
        if (ImageExtensions.Contains(extension))
        {
            return 1;
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
            var detectedPageCount = _fileStorageService.TryGetPdfPageCount(stream);
            if (IsValidPageCount(detectedPageCount))
            {
                return detectedPageCount;
            }
        }

        return IsValidPageCount(manualPageCount) ? manualPageCount : null;
    }

    private int? ResolveBufferedPageCount(string extension, MemoryStream content, int? manualPageCount)
    {
        if (ImageExtensions.Contains(extension))
        {
            return 1;
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var detectedPageCount = _fileStorageService.TryGetPdfPageCount(content);
            content.Position = 0;
            if (IsValidPageCount(detectedPageCount))
            {
                return detectedPageCount;
            }
        }

        return IsValidPageCount(manualPageCount) ? manualPageCount : null;
    }

    private async Task<IActionResult> BuildOfficePreviewFileResultAsync(
        Stream sourceStream,
        string safeFileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // ChatGPT 2026-06-12: Office file da luu can convert sang PDF de iframe/browser xem truoc duoc truoc khi in.
            var preview = await _officePreviewService.ConvertToPdfAsync(
                sourceStream,
                safeFileName,
                cancellationToken);

            using var pdfStream = new MemoryStream(preview.PdfBytes, writable: false);
            var pageCount = _fileStorageService.TryGetPdfPageCount(pdfStream);
            if (IsValidPageCount(pageCount))
            {
                Response.Headers["X-Preview-Page-Count"] = pageCount!.Value.ToString();
            }

            ApplyInlinePreviewHeaders(Path.ChangeExtension(safeFileName, ".pdf"));
            return File(preview.PdfBytes, "application/pdf");
        }
        catch (OfficePreviewUnavailableException ex)
        {
            return BuildOfficePreviewTextError(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
        catch (TimeoutException)
        {
            return BuildOfficePreviewTextError(
                StatusCodes.Status504GatewayTimeout,
                "Quá thời gian chuyển đổi file Office. Hãy thử lại hoặc tải file gốc.");
        }
        catch (InvalidDataException ex)
        {
            return BuildOfficePreviewTextError(StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(
                ex,
                "Stored Office preview conversion failed for {FileName}. CorrelationId: {CorrelationId}",
                safeFileName,
                correlationId);

            return BuildOfficePreviewTextError(
                StatusCodes.Status500InternalServerError,
                $"Không thể tạo bản xem trước file Office. Mã theo dõi: {correlationId}.");
        }
    }

    private bool IsOfficeFile(string fileName)
    {
        return _officePreviewService.IsSupportedExtension(Path.GetExtension(fileName).ToLowerInvariant());
    }

    private void ApplyInlinePreviewHeaders(string safeFileName)
    {
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self';";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(safeFileName)}";
    }

    private ContentResult BuildOfficePreviewTextError(int statusCode, string message)
    {
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self';";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";

        var result = Content(message, "text/plain; charset=utf-8");
        result.StatusCode = statusCode;
        return result;
    }

    private bool TryValidateUploadPageRange(
        int index,
        string fileName,
        int documentPageCount,
        int? pageFrom,
        int? pageTo,
        out int selectedPageCount)
    {
        selectedPageCount = 0;
        var isValid = true;

        if (pageFrom is null || pageFrom < 1 || pageFrom > documentPageCount)
        {
            ModelState.AddModelError(
                $"UploadPageFroms[{index}]",
                $"Trang bắt đầu của file {fileName} phải từ 1 đến {documentPageCount}.");
            isValid = false;
        }

        if (pageTo is null || pageTo < 1 || pageTo > documentPageCount)
        {
            ModelState.AddModelError(
                $"UploadPageTos[{index}]",
                $"Trang kết thúc của file {fileName} phải từ 1 đến {documentPageCount}.");
            isValid = false;
        }

        if (isValid && pageFrom > pageTo)
        {
            ModelState.AddModelError(
                $"UploadPageTos[{index}]",
                $"Trang bắt đầu của file {fileName} không được lớn hơn trang kết thúc.");
            isValid = false;
        }

        if (isValid)
        {
            selectedPageCount = pageTo!.Value - pageFrom!.Value + 1;
        }

        return isValid;
    }

    private static string BuildPrintJobNotes(
        string? userNotes,
        int pageFrom,
        int pageTo,
        int documentPageCount)
    {
        const int maxLength = 500;
        var rangeNote = $"Phạm vi in: trang {pageFrom}-{pageTo}/{documentPageCount}.";
        var normalizedUserNotes = userNotes?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserNotes))
        {
            return rangeNote;
        }

        var separator = Environment.NewLine;
        var availableLength = maxLength - rangeNote.Length - separator.Length;
        if (availableLength <= 0)
        {
            return rangeNote[..Math.Min(rangeNote.Length, maxLength)];
        }

        if (normalizedUserNotes.Length > availableLength)
        {
            normalizedUserNotes = normalizedUserNotes[..availableLength];
        }

        return $"{rangeNote}{separator}{normalizedUserNotes}";
    }

    private static bool IsValidPageCount(int? value)
    {
        return value is >= 1 and <= MaxPagesPerFile;
    }

    private static string ResolveContentType(string fileName, string? fallbackContentType)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => string.IsNullOrWhiteSpace(fallbackContentType)
                ? "application/octet-stream"
                : fallbackContentType
        };
    }

    private static string BuildItemIdempotencyKey(string? batchKey, int index)
    {
        var normalizedBatchKey = string.IsNullOrWhiteSpace(batchKey)
            ? Guid.NewGuid().ToString("N")
            : batchKey.Trim();
        return $"{normalizedBatchKey}:{index + 1:D2}";
    }

    private object? BranchRouteValues()
    {
        var branchSlug = RouteData.Values["branchSlug"]?.ToString();
        return string.IsNullOrWhiteSpace(branchSlug) ? null : new { branchSlug };
    }

    private sealed record PreparedPrintFile(
        Guid FileId,
        string FileName,
        int DocumentPageCount,
        int PageFrom,
        int PageTo,
        int SelectedPageCount);

    private sealed record BufferedUpload(
        string FileName,
        string ContentType,
        long Size,
        int DocumentPageCount,
        int PageFrom,
        int PageTo,
        int SelectedPageCount,
        MemoryStream Content);
}
