using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Application.Security;
using WebPhotocopyHub.Web.Admin.Authorization;
using WebPhotocopyHub.Web.Admin.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class SystemRolesController : Controller
{
    private readonly ISystemAdministrationService _systemAdministrationService;

    public SystemRolesController(
        ISystemAdministrationService systemAdministrationService)
    {
        _systemAdministrationService = systemAdministrationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        ViewBag.CanCreate = await HasPermissionAsync(
            SystemPermissionActions.Create,
            cancellationToken);
        ViewBag.CanEdit = await HasPermissionAsync(
            SystemPermissionActions.Edit,
            cancellationToken);
        ViewBag.CanViewPermissions = await _systemAdministrationService
            .HasPermissionAsync(
                User,
                "Admin",
                "SystemPermissions",
                SystemPermissionActions.View,
                cancellationToken);

        return View(new SystemRoleIndexViewModel
        {
            Roles = await _systemAdministrationService
                .GetRolesAsync(cancellationToken)
        });
    }

    [HttpGet]
    [SystemPermissionAction(SystemPermissionActions.Create)]
    public IActionResult Create()
    {
        return View("Edit", new SystemRoleEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Create)]
    public Task<IActionResult> Create(
        SystemRoleEditViewModel model,
        CancellationToken cancellationToken)
    {
        model.RoleId = null;
        model.IsSystemRole = false;
        return SaveAsync(model, true, cancellationToken);
    }

    [HttpGet]
    [SystemPermissionAction(SystemPermissionActions.Edit)]
    public async Task<IActionResult> Edit(
        string id,
        CancellationToken cancellationToken)
    {
        var role = await _systemAdministrationService
            .GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        var roleList = await _systemAdministrationService
            .GetRolesAsync(cancellationToken);
        var summary = roleList.First(x => x.RoleId == id);

        return View(new SystemRoleEditViewModel
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = summary.IsSystemRole
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Edit)]
    public Task<IActionResult> Edit(
        SystemRoleEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.RoleId))
        {
            ModelState.AddModelError(
                nameof(model.RoleId),
                "Thiếu định danh vai trò cần cập nhật.");
        }

        return SaveAsync(model, false, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Edit)]
    public async Task<IActionResult> SetActive(
        string id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            await _systemAdministrationService.SetRoleActiveAsync(
                id,
                isActive,
                User.GetUserId(),
                cancellationToken);
            TempData["Success"] = isActive
                ? "Đã kích hoạt vai trò."
                : "Đã vô hiệu hóa vai trò.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<bool> HasPermissionAsync(
        string action,
        CancellationToken cancellationToken)
    {
        return _systemAdministrationService.HasPermissionAsync(
            User,
            "Admin",
            "SystemRoles",
            action,
            cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(
        SystemRoleEditViewModel model,
        bool isCreate,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        try
        {
            var roleId = await _systemAdministrationService.SaveRoleAsync(
                new SystemRoleEditDto
                {
                    RoleId = model.RoleId,
                    RoleName = model.RoleName,
                    DisplayName = model.DisplayName,
                    Description = model.Description,
                    IsActive = model.IsActive
                },
                User.GetUserId(),
                cancellationToken);

            TempData["Success"] = isCreate
                ? "Đã tạo vai trò hệ thống."
                : "Đã cập nhật vai trò hệ thống.";
            return RedirectToAction(nameof(Edit), new { id = roleId });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Edit", model);
        }
    }
}
