using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Application.Security;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Web.Admin.Authorization;
using WebPhotocopyHub.Web.Admin.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class SystemPermissionsController : Controller
{
    private readonly ISystemAdministrationService _systemAdministrationService;

    public SystemPermissionsController(
        ISystemAdministrationService systemAdministrationService)
    {
        _systemAdministrationService = systemAdministrationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? roleId,
        CancellationToken cancellationToken)
    {
        ViewBag.CanEditPermissions = await _systemAdministrationService
            .HasPermissionAsync(
                User,
                "Admin",
                "SystemPermissions",
                SystemPermissionActions.Edit,
                cancellationToken);
        ViewBag.CanViewRoles = await _systemAdministrationService
            .HasPermissionAsync(
                User,
                "Admin",
                "SystemRoles",
                SystemPermissionActions.View,
                cancellationToken);
        ViewBag.CanViewFunctions = await _systemAdministrationService
            .HasPermissionAsync(
                User,
                "Admin",
                "SystemFunctions",
                SystemPermissionActions.View,
                cancellationToken);

        var roles = await _systemAdministrationService
            .GetRolesAsync(cancellationToken);
        var activeRoles = roles
            .Where(x => x.IsActive)
            .OrderBy(x => x.IsSystemRole)
            .ThenBy(x => x.DisplayName)
            .ToList();

        roleId ??= activeRoles
            .FirstOrDefault(x => !string.Equals(
                x.RoleName,
                RoleConstants.Admin,
                StringComparison.Ordinal))
            ?.RoleId
            ?? activeRoles.FirstOrDefault()?.RoleId;

        if (string.IsNullOrWhiteSpace(roleId))
        {
            return View(new SystemPermissionMatrixViewModel
            {
                AvailableRoles = activeRoles
            });
        }

        var selectedRole = roles.FirstOrDefault(x => x.RoleId == roleId);
        if (selectedRole is null)
        {
            return NotFound();
        }

        return View(new SystemPermissionMatrixViewModel
        {
            RoleId = selectedRole.RoleId,
            RoleName = selectedRole.RoleName,
            RoleDisplayName = selectedRole.DisplayName,
            IsAdminRole = string.Equals(
                selectedRole.RoleName,
                RoleConstants.Admin,
                StringComparison.Ordinal),
            AvailableRoles = activeRoles,
            Permissions = (await _systemAdministrationService
                    .GetPermissionMatrixAsync(roleId, cancellationToken))
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Edit)]
    public async Task<IActionResult> Save(
        SystemPermissionMatrixViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu phân quyền chưa hợp lệ.";
            return RedirectToAction(nameof(Index), new { roleId = model.RoleId });
        }

        try
        {
            await _systemAdministrationService.SavePermissionMatrixAsync(
                model.RoleId,
                model.Permissions.Select(x => new SystemPermissionUpdateDto
                {
                    FunctionId = x.FunctionId,
                    CanView = x.CanView,
                    CanCreate = x.CanCreate,
                    CanEdit = x.CanEdit,
                    CanDelete = x.CanDelete,
                    CanExport = x.CanExport
                }),
                User.GetUserId(),
                cancellationToken);

            TempData["Success"] = "Đã cập nhật ma trận phân quyền.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { roleId = model.RoleId });
    }
}
