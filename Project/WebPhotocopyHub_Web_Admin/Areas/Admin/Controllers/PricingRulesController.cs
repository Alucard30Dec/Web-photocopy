using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class PricingRulesController : Controller
{
    private readonly IPricingRuleService _pricingRuleService;
    private readonly IAuditLogService _auditLogService;

    public PricingRulesController(IPricingRuleService pricingRuleService, IAuditLogService auditLogService)
    {
        _pricingRuleService = pricingRuleService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _pricingRuleService.GetAllAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return View(new EditPricingRuleViewModel());
        }

        var item = await _pricingRuleService.GetByIdAsync(id.Value, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return View(new EditPricingRuleViewModel
        {
            Id = item.Id,
            PaperSize = item.PaperSize,
            PrintSide = item.PrintSide,
            ColorMode = item.ColorMode,
            IsPhoto = item.IsPhoto,
            UnitPrice = item.UnitPrice,
            IsActive = item.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPricingRuleViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        PricingRule entity;
        if (model.Id.HasValue)
        {
            entity = await _pricingRuleService.GetByIdAsync(model.Id.Value, cancellationToken) ?? new PricingRule();
        }
        else
        {
            entity = new PricingRule();
        }

        entity.PaperSize = model.PaperSize;
        entity.PrintSide = model.PrintSide;
        entity.ColorMode = model.ColorMode;
        entity.IsPhoto = model.IsPhoto;
        entity.UnitPrice = model.UnitPrice;
        entity.IsActive = model.IsActive;

        var saved = await _pricingRuleService.UpsertAsync(entity, cancellationToken);

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "UpsertPricingRule",
            EntityName = nameof(PricingRule),
            EntityId = saved.Id.ToString(),
            Details = $"PaperSize: {saved.PaperSize}, PrintSide: {saved.PrintSide}, ColorMode: {saved.ColorMode}, UnitPrice: {saved.UnitPrice}"
        }, cancellationToken);

        TempData["Success"] = "Lưu bảng giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _pricingRuleService.DeleteAsync(id, cancellationToken);

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "DeletePricingRule",
            EntityName = nameof(PricingRule),
            EntityId = id.ToString()
        }, cancellationToken);

        TempData["Success"] = "Đã xóa bảng giá.";
        return RedirectToAction(nameof(Index));
    }
}
