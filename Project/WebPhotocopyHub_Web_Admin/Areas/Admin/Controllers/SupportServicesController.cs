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
public class SupportServicesController : Controller
{
    private readonly ISupportServiceOrderService _supportServiceOrderService;
    private readonly IAuditLogService _auditLogService;

    public SupportServicesController(ISupportServiceOrderService supportServiceOrderService, IAuditLogService auditLogService)
    {
        _supportServiceOrderService = supportServiceOrderService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _supportServiceOrderService.GetAllServicesAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return View(new EditSupportServiceViewModel());
        }

        var item = await _supportServiceOrderService.GetServiceByIdAsync(id.Value, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return View(new EditSupportServiceViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            UnitPrice = item.UnitPrice,
            FeeType = item.FeeType,
            IsActive = item.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditSupportServiceViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        SupportService entity;
        if (model.Id.HasValue)
        {
            entity = await _supportServiceOrderService.GetServiceByIdAsync(model.Id.Value, cancellationToken)
                     ?? new SupportService();
        }
        else
        {
            entity = new SupportService();
        }

        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.UnitPrice = model.UnitPrice;
        entity.FeeType = model.FeeType;
        entity.IsActive = model.IsActive;

        var saved = await _supportServiceOrderService.UpsertServiceAsync(entity, cancellationToken);

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "UpsertSupportService",
            EntityName = nameof(SupportService),
            EntityId = saved.Id.ToString(),
            Details = $"Name: {saved.Name}, UnitPrice: {saved.UnitPrice}, FeeType: {saved.FeeType}"
        }, cancellationToken);

        TempData["Success"] = "Lưu dịch vụ hỗ trợ thành công.";
        return RedirectToAction(nameof(Index));
    }
}
