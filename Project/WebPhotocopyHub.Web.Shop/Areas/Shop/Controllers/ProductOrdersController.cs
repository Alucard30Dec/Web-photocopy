using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Shop.Models;

namespace WebPhotocopyHub.Web.Areas.Shop.Controllers;

[Area("Shop")]
[Authorize(Policy = AppPolicies.ShopOperation)]
public class ProductOrdersController : Controller
{
    private readonly IProductOrderService _productOrderService;
    private readonly IAuditLogService _auditLogService;

    public ProductOrdersController(IProductOrderService productOrderService, IAuditLogService auditLogService)
    {
        _productOrderService = productOrderService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _productOrderService.GetAllOrdersAsync(cancellationToken);
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
            await _productOrderService.UpdateOrderStatusAsync(
                model.OrderId,
                model.Status,
                User.GetUserId(),
                model.Note,
                cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "ShopUpdateProductOrderStatus",
                EntityName = nameof(ProductOrder),
                EntityId = model.OrderId.ToString(),
                Details = $"Status: {model.Status}; Note: {model.Note}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Đã cập nhật trạng thái đơn văn phòng phẩm.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
