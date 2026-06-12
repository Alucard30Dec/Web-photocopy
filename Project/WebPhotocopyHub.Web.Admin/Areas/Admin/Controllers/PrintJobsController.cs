using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class PrintJobsController : Controller
{
    private readonly IPrintJobService _printJobService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOfficePreviewService _officePreviewService;
    private readonly IAuditLogService _auditLogService;

    public PrintJobsController(
        IPrintJobService printJobService,
        IFileStorageService fileStorageService,
        IOfficePreviewService officePreviewService,
        IAuditLogService auditLogService)
    {
        _printJobService = printJobService;
        _fileStorageService = fileStorageService;
        _officePreviewService = officePreviewService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _printJobService.GetAllOrdersAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _printJobService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdatePrintJobStatusViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id = model.PrintJobId });
        }

        try
        {
            await _printJobService.UpdateStatusAsync(model.PrintJobId, model.Status, User.GetUserId(), actorIsAdmin: true, model.Note, cancellationToken);
            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "UpdatePrintJobStatus",
                EntityName = nameof(PrintJob),
                EntityId = model.PrintJobId.ToString(),
                Details = $"Status: {model.Status}; Note: {model.Note}"
            }, cancellationToken);

            TempData["Success"] = "Đã cập nhật trạng thái đơn in.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.PrintJobId });
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await _printJobService.RefundAsync(id, User.GetUserId(), actorIsAdmin: true, reason, cancellationToken);
            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "RefundPrintJob",
                EntityName = nameof(PrintJob),
                EntityId = id.ToString(),
                Details = reason
            }, cancellationToken);

            TempData["Success"] = "Hoàn tiền thành công.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.DownloadPrintFile)]
    public async Task<IActionResult> PreviewFile(Guid id, CancellationToken cancellationToken)
    {
        var item = await _printJobService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var metadata = await _fileStorageService.GetMetadataAsync(item.UploadedFileId, cancellationToken);
        if (metadata is null)
        {
            return NotFound();
        }

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "PreviewPrintFile",
            EntityName = nameof(PrintJob),
            EntityId = id.ToString(),
            Details = metadata.OriginalFileName,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        var safeFileName = Path.GetFileName(metadata.OriginalFileName);
        if (IsOfficeFile(safeFileName))
        {
            await using var sourceStream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
            return await BuildOfficePreviewFileResultAsync(sourceStream, safeFileName, cancellationToken);
        }

        var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
        ApplyInlinePreviewHeaders(safeFileName);
        return File(stream, metadata.ContentType);
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.DownloadPrintFile)]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        var item = await _printJobService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var metadata = await _fileStorageService.GetMetadataAsync(item.UploadedFileId, cancellationToken);
        if (metadata is null)
        {
            return NotFound();
        }

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "DownloadPrintFile",
            EntityName = nameof(PrintJob),
            EntityId = id.ToString(),
            Details = metadata.OriginalFileName,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
        return File(stream, metadata.ContentType, metadata.OriginalFileName);
    }

    private async Task<IActionResult> BuildOfficePreviewFileResultAsync(
        Stream sourceStream,
        string safeFileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // ChatGPT 2026-06-12: admin can inspect Office print files in iframe by converting them to PDF preview.
            var preview = await _officePreviewService.ConvertToPdfAsync(
                sourceStream,
                safeFileName,
                cancellationToken);

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
        catch (Exception)
        {
            return BuildOfficePreviewTextError(
                StatusCodes.Status500InternalServerError,
                "Không thể tạo bản xem trước file Office. Hãy tải file gốc để kiểm tra.");
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
}
