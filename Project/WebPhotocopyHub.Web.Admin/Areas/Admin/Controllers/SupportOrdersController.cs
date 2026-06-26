using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web.Admin.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class SupportOrdersController : Controller
{
    private readonly ISupportServiceOrderService _supportServiceOrderService;
    private readonly IAuditLogService _auditLogService;

    public SupportOrdersController(
        ISupportServiceOrderService supportServiceOrderService,
        IAuditLogService auditLogService)
    {
        _supportServiceOrderService = supportServiceOrderService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _supportServiceOrderService.GetAllOrdersAsync(cancellationToken);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        UpdateSystemOrderStatusViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu cập nhật trạng thái không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (model.Status == OrderStatus.Refunded)
        {
            TempData["Error"] = "Không thể chỉ đổi trạng thái thành Đã hoàn tiền vì service hiện tại chưa thực hiện hoàn ví cho loại đơn này.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _supportServiceOrderService.UpdateOrderStatusAsync(
                model.OrderId,
                model.Status,
                User.GetUserId(),
                model.Note,
                cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "AdminUpdateSupportOrderStatus",
                EntityName = nameof(SupportServiceOrder),
                EntityId = model.OrderId.ToString(),
                Details = $"Status: {model.Status}; Note: {model.Note}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Đã cập nhật trạng thái đơn dịch vụ hỗ trợ.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
