using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Customer.Models;

namespace WebPhotocopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class ProductsController : Controller
{
    private readonly IProductOrderService _productOrderService;

    public ProductsController(IProductOrderService productOrderService)
    {
        _productOrderService = productOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await _productOrderService.GetActiveProductsAsync(cancellationToken);
        var vm = new ProductCatalogViewModel
        {
            Items = products.Select(x => new ProductOrderItemInputViewModel
            {
                ProductId = x.Id,
                Name = x.Name,
                Price = x.Price,
                StockQuantity = x.StockQuantity
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProductCatalogViewModel model, CancellationToken cancellationToken)
    {
        var products = await _productOrderService.GetActiveProductsAsync(cancellationToken);
        var selectedItems = model.Items ?? new List<ProductOrderItemInputViewModel>();
        var validSelectedItems = selectedItems
            .Where(x => x.ProductId != Guid.Empty && x.Quantity > 0)
            .ToList();

        // Codex 2026-07-04: Shipping UI is locked until delivery workflow is developed; scope limited to customer product orders.
        if (model.DeliveryMethod == DeliveryMethod.Shipping)
        {
            ModelState.AddModelError(nameof(model.DeliveryMethod), "Giao tận nơi chưa được phát triển. Vui lòng nhận đơn tại tiệm.");
        }

        if (!ModelState.IsValid)
        {
            model.Items = products.Select(x => new ProductOrderItemInputViewModel
            {
                ProductId = x.Id,
                Name = x.Name,
                Price = x.Price,
                StockQuantity = x.StockQuantity,
                Quantity = selectedItems.FirstOrDefault(i => i.ProductId == x.Id)?.Quantity ?? 0
            }).ToList();

            return View(model);
        }

        try
        {
            await _productOrderService.CreateOrderAsync(new CreateProductOrderDto
            {
                UserId = User.GetUserId(),
                IdempotencyKey = model.IdempotencyKey,
                DeliveryMethod = model.DeliveryMethod,
                DeliveryAddress = model.DeliveryAddress,
                Notes = model.Notes,
                Items = validSelectedItems
                    .Select(x => new CreateProductOrderItemDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity
                    }).ToList()
            }, cancellationToken);

            TempData["Success"] = "Đặt mua văn phòng phẩm thành công.";
            return RedirectToAction(nameof(Orders), BranchRouteValues());
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Items = products.Select(x => new ProductOrderItemInputViewModel
            {
                ProductId = x.Id,
                Name = x.Name,
                Price = x.Price,
                StockQuantity = x.StockQuantity,
                Quantity = selectedItems.FirstOrDefault(i => i.ProductId == x.Id)?.Quantity ?? 0
            }).ToList();

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Orders([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var orders = await _productOrderService.GetUserOrdersAsync(User.GetUserId(), page, 10, cancellationToken);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _productOrderService.GetOrderByIdAsync(id, cancellationToken);
        if (item is null || item.UserId != User.GetUserId())
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _productOrderService.CancelOrderAsync(id, User.GetUserId(), cancellationToken);
            TempData["Success"] = "Đã huỷ đơn hàng thành công.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id, branchSlug = RouteData.Values["branchSlug"]?.ToString() });
    }

    private object? BranchRouteValues()
    {
        var branchSlug = RouteData.Values["branchSlug"]?.ToString();
        return string.IsNullOrWhiteSpace(branchSlug) ? null : new { branchSlug };
    }
}
