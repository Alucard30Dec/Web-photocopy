using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Shop.Models;

namespace WebPhotocopyHub.Web.Areas.Shop.Controllers;

[Area("Shop")]
[Authorize(Policy = AppPolicies.ShopOperation)]
public class SupportOrdersController : Controller
{
    private readonly ISupportServiceOrderService _supportServiceOrderService;
    private readonly IAuditLogService _auditLogService;

    public SupportOrdersController(ISupportServiceOrderService supportServiceOrderService, IAuditLogService auditLogService)
    {
        _supportServiceOrderService = supportServiceOrderService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = (await _supportServiceOrderService.GetAllOrdersAsync(1, 1000, cancellationToken)).Items;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateShopOrderStatusViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu cập nhật không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (model.Status == OrderStatus.Refunded)
        {
            await RefundInternalAsync(model.OrderId, model.Note, cancellationToken);
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
                Action = "ShopUpdateSupportOrderStatus",
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(Guid id, string reason, CancellationToken cancellationToken)
    {
        await RefundInternalAsync(id, reason, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task RefundInternalAsync(Guid orderId, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            await _supportServiceOrderService.RefundAsync(
                orderId,
                User.GetUserId(),
                reason ?? string.Empty,
                cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "ShopRefundSupportOrder",
                EntityName = nameof(SupportServiceOrder),
                EntityId = orderId.ToString(),
                Details = reason,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Đã hoàn tiền đơn dịch vụ hỗ trợ.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }
    }
}
