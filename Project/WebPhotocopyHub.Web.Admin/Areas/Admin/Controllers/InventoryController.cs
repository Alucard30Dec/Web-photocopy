using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Web.Admin.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public sealed class InventoryController : Controller
{
    private readonly IProductOrderService _productOrderService;
    private readonly IAuditLogService _auditLogService;

    public InventoryController(
        IProductOrderService productOrderService,
        IAuditLogService auditLogService)
    {
        _productOrderService = productOrderService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new AdminInventoryViewModel
        {
            Products = await _productOrderService.GetAllProductsAsync(cancellationToken),
            RecentMovements = await _productOrderService.GetRecentStockMovementsAsync(150, cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(
        AdjustAdminStockViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.QuantityDelta == 0)
        {
            ModelState.AddModelError(nameof(model.QuantityDelta), "Số lượng thay đổi phải khác 0.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("; ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _productOrderService.AdjustStockAsync(new AdjustProductStockDto
            {
                ProductId = model.ProductId,
                QuantityDelta = model.QuantityDelta,
                ActorUserId = User.GetUserId(),
                Note = model.Note
            }, cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "AdminAdjustStock",
                EntityName = "Product",
                EntityId = model.ProductId.ToString(),
                Details = $"Delta: {model.QuantityDelta}; Note: {model.Note}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Đã cập nhật tồn kho và ghi audit.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
