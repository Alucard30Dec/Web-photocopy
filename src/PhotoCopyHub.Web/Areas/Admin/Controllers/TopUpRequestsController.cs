using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class TopUpRequestsController : Controller
{
    private readonly ITopUpService _topUpService;
    private readonly IAuditLogService _auditLogService;

    public TopUpRequestsController(ITopUpService topUpService, IAuditLogService auditLogService)
    {
        _topUpService = topUpService;
        _auditLogService = auditLogService;
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
        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAtUtc,UserEmail,UserId,Amount,Channel,Status,TransferContent,TransactionReferenceCode,RequiresAdminApproval,ReviewedBy,SecondReviewedBy,ReviewNote,SecondReviewNote");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.CreatedAt.ToString("O")),
                EscapeCsv(item.User?.Email),
                EscapeCsv(item.UserId),
                EscapeCsv(item.Amount.ToString()),
                EscapeCsv(item.Channel.ToString()),
                EscapeCsv(item.Status.ToString()),
                EscapeCsv(item.TransferContent),
                EscapeCsv(item.TransactionReferenceCode),
                EscapeCsv(item.RequiresAdminApproval.ToString()),
                EscapeCsv(item.ReviewedByAdminId),
                EscapeCsv(item.SecondReviewedByAdminId),
                EscapeCsv(item.ReviewNote),
                EscapeCsv(item.SecondReviewNote)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
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

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
