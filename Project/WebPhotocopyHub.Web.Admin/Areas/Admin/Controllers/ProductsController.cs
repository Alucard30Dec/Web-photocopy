using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ProductsController : Controller
{
    private readonly IProductOrderService _productOrderService;
    private readonly IAuditLogService _auditLogService;

    public ProductsController(IProductOrderService productOrderService, IAuditLogService auditLogService)
    {
        _productOrderService = productOrderService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _productOrderService.GetAllProductsAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return View(new EditProductViewModel());
        }

        var product = await _productOrderService.GetProductByIdAsync(id.Value, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var vm = new EditProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProductViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Product entity;
        if (model.Id.HasValue)
        {
            entity = await _productOrderService.GetProductByIdAsync(model.Id.Value, cancellationToken)
                     ?? new Product();
        }
        else
        {
            entity = new Product();
        }

        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.Price = model.Price;
        entity.StockQuantity = model.StockQuantity;
        entity.ImageUrl = model.ImageUrl;
        entity.IsActive = model.IsActive;

        var saved = await _productOrderService.UpsertProductAsync(entity, cancellationToken);

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "UpsertProduct",
            EntityName = nameof(Product),
            EntityId = saved.Id.ToString(),
            Details = $"Name: {saved.Name}, Price: {saved.Price}, Stock: {saved.StockQuantity}"
        }, cancellationToken);

        TempData["Success"] = "Lưu sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }
}
