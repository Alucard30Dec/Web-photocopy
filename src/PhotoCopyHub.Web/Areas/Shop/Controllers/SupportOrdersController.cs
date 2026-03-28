using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Areas.Shop.Controllers;

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
        var items = await _supportServiceOrderService.GetAllOrdersAsync(cancellationToken);
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
}
