using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class PrintJobsController : Controller
{
    private readonly IPrintJobService _printJobService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;

    public PrintJobsController(
        IPrintJobService printJobService,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _printJobService = printJobService;
        _fileStorageService = fileStorageService;
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

        var stream = await _fileStorageService.OpenReadAsync(metadata.Id, cancellationToken);
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self';";
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(metadata.OriginalFileName)}";
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
}
