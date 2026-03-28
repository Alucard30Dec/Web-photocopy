using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

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
                Items = selectedItems.Where(x => x.Quantity > 0)
                    .Select(x => new CreateProductOrderItemDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity
                    }).ToList()
            }, cancellationToken);

            TempData["Success"] = "Đặt mua văn phòng phẩm thành công.";
            return RedirectToAction(nameof(Orders));
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
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
    {
        var orders = await _productOrderService.GetUserOrdersAsync(User.GetUserId(), cancellationToken);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var orders = await _productOrderService.GetUserOrdersAsync(User.GetUserId(), cancellationToken);
        var item = orders.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        return View(item);
    }
}
