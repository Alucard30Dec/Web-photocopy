using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class PrintJobsController : Controller
{
    private readonly IPrintJobService _printJobService;
    private readonly IFileStorageService _fileStorageService;

    public PrintJobsController(IPrintJobService printJobService, IFileStorageService fileStorageService)
    {
        _printJobService = printJobService;
        _fileStorageService = fileStorageService;
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
        model.ExistingFiles = await _fileStorageService.GetFilesByOwnerAsync(User.GetUserId(), cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var fileId = model.ExistingFileId;
            int? detectedPages = null;

            if (model.UploadFile is { Length: > 0 })
            {
                await using var mem = new MemoryStream();
                await model.UploadFile.CopyToAsync(mem, cancellationToken);
                mem.Position = 0;

                if (string.Equals(Path.GetExtension(model.UploadFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    detectedPages = _fileStorageService.TryGetPdfPageCount(mem);
                    mem.Position = 0;
                }

                var uploaded = await _fileStorageService.SaveAsync(new CreateUploadedFileDto
                {
                    OwnerUserId = User.GetUserId(),
                    OriginalFileName = model.UploadFile.FileName,
                    ContentType = model.UploadFile.ContentType,
                    Size = model.UploadFile.Length,
                    Content = mem,
                    IsForPrintJob = true
                }, cancellationToken);

                fileId = uploaded.Id;
            }

            if (fileId is null)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn hoặc upload file in.");
                return View(model);
            }

            var totalPages = model.TotalPages ?? detectedPages;
            if (totalPages is null or <= 0)
            {
                ModelState.AddModelError(nameof(model.TotalPages), "Không xác định được số trang tự động, vui lòng nhập thủ công.");
                return View(model);
            }

            await _printJobService.CreateAndSubmitAsync(new CreatePrintJobDto
            {
                UserId = User.GetUserId(),
                UploadedFileId = fileId.Value,
                PaperSize = model.PaperSize,
                PrintSide = model.PrintSide,
                ColorMode = model.ColorMode,
                IsPhoto = model.IsPhoto,
                Copies = model.Copies,
                TotalPages = totalPages.Value,
                Notes = model.Notes,
                IdempotencyKey = model.IdempotencyKey,
                DeliveryMethod = model.DeliveryMethod,
                DeliveryAddress = model.DeliveryAddress
            }, cancellationToken);

            TempData["Success"] = "Đã gửi đơn in. Tiệm sẽ xác nhận file trước khi trừ tiền ví.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
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

        var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self';";
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(metadata.OriginalFileName)}";
        return File(stream, metadata.ContentType);
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
        return File(stream, metadata.ContentType, metadata.OriginalFileName);
    }
}
