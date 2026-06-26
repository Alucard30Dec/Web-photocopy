using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Report;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class TopUpRequestsController : Controller
{
    private readonly ITopUpService _topUpService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAdminCsvReportService _reportService;

    public TopUpRequestsController(
        ITopUpService topUpService,
        IAuditLogService auditLogService,
        IAdminCsvReportService reportService)
    {
        _topUpService = topUpService;
        _auditLogService = auditLogService;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _topUpService.GetAllRequestsAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var items = await _topUpService.GetAllRequestsAsync(cancellationToken);
        var bytes = _reportService.BuildTopUpRequestsCsv(items);
        return File(bytes, "text/csv; charset=utf-8", $"topup-requests-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewTopUpViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
            {
                TopUpRequestId = model.TopUpRequestId,
                IsApprove = model.IsApprove,
                Note = model.Note,
                IdempotencyKey = model.IdempotencyKey,
                ReviewerUserId = User.GetUserId(),
                IsAdminReviewer = true
            }, cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = model.IsApprove ? "ApproveTopUp" : "RejectTopUp",
                EntityName = "TopUpRequest",
                EntityId = model.TopUpRequestId.ToString(),
                Details = model.Note,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = model.IsApprove ? "Đã duyệt yêu cầu nạp tiền." : "Đã từ chối yêu cầu nạp tiền.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
