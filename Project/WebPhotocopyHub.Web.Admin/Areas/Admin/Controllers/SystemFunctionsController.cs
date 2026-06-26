using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.Security;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Admin.Authorization;
using WebPhotocopyHub.Web.Admin.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class SystemFunctionsController : Controller
{
    private readonly ISystemAdministrationService _systemAdministrationService;

    public SystemFunctionsController(
        ISystemAdministrationService systemAdministrationService)
    {
        _systemAdministrationService = systemAdministrationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var functions = await _systemAdministrationService
            .GetFunctionsAsync(cancellationToken);

        var rows = new List<SystemFunctionRowViewModel>();
        var lookup = functions.ToLookup(x => x.ParentId);

        void Append(Guid? parentId, int level)
        {
            foreach (var function in lookup[parentId]
                         .OrderBy(x => x.SortOrder)
                         .ThenBy(x => x.Name))
            {
                var permissions = new List<string>();
                if (function.SupportsView) permissions.Add("Xem");
                if (function.SupportsCreate) permissions.Add("Thêm");
                if (function.SupportsEdit) permissions.Add("Sửa");
                if (function.SupportsDelete) permissions.Add("Xóa");
                if (function.SupportsExport) permissions.Add("Xuất");

                rows.Add(new SystemFunctionRowViewModel
                {
                    Id = function.Id,
                    Level = level,
                    Code = function.Code,
                    Name = function.Name,
                    Route = string.IsNullOrWhiteSpace(function.Controller)
                        ? "Nhóm menu"
                        : $"/{function.Area}/{function.Controller}/{function.Action ?? "Index"}",
                    PermissionSummary = permissions.Count == 0
                        ? "Không áp dụng"
                        : string.Join(", ", permissions),
                    IsActive = function.IsActive,
                    IsSystemFunction = function.IsSystemFunction,
                    SortOrder = function.SortOrder
                });

                Append(function.Id, level + 1);
            }
        }

        Append(null, 0);
        ViewBag.CanCreate = await HasPermissionAsync(
            SystemPermissionActions.Create,
            cancellationToken);
        ViewBag.CanEdit = await HasPermissionAsync(
            SystemPermissionActions.Edit,
            cancellationToken);
        ViewBag.CanDelete = await HasPermissionAsync(
            SystemPermissionActions.Delete,
            cancellationToken);
        ViewBag.CanViewPermissions = await _systemAdministrationService
            .HasPermissionAsync(
                User,
                "Admin",
                "SystemPermissions",
                SystemPermissionActions.View,
                cancellationToken);

        return View(new SystemFunctionListViewModel
        {
            Functions = rows
        });
    }

    [HttpGet]
    [SystemPermissionAction(SystemPermissionActions.Create)]
    public async Task<IActionResult> Create(
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        return View("Edit", new SystemFunctionEditViewModel
        {
            ParentId = parentId,
            ParentOptions = await GetParentOptionsAsync(
                Guid.Empty,
                cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Create)]
    public Task<IActionResult> Create(
        SystemFunctionEditViewModel model,
        CancellationToken cancellationToken)
    {
        model.Id = Guid.Empty;
        model.IsSystemFunction = false;
        return SaveAsync(model, true, cancellationToken);
    }

    [HttpGet]
    [SystemPermissionAction(SystemPermissionActions.Edit)]
    public async Task<IActionResult> Edit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var function = await _systemAdministrationService
            .GetFunctionAsync(id, cancellationToken);
        if (function is null)
        {
            return NotFound();
        }

        var parentOptions = await GetParentOptionsAsync(
            function.Id,
            cancellationToken);
        return View(ToEditModel(function, parentOptions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Edit)]
    public Task<IActionResult> Edit(
        SystemFunctionEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                "Thiếu định danh chức năng cần cập nhật.");
        }

        return SaveAsync(model, false, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SystemPermissionAction(SystemPermissionActions.Delete)]
    public async Task<IActionResult> SetActive(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            await _systemAdministrationService.SetFunctionActiveAsync(
                id,
                isActive,
                User.GetUserId(),
                cancellationToken);
            TempData["Success"] = isActive
                ? "Đã kích hoạt chức năng."
                : "Đã vô hiệu hóa chức năng.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(
        SystemFunctionEditViewModel model,
        bool isCreate,
        CancellationToken cancellationToken)
    {
        model.ParentOptions = await GetParentOptionsAsync(
            model.Id,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        try
        {
            var existing = isCreate
                ? null
                : await _systemAdministrationService
                    .GetFunctionAsync(model.Id, cancellationToken);
            if (!isCreate && existing is null)
            {
                return NotFound();
            }

            var entity = new SystemFunction
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                ParentId = model.ParentId,
                Area = model.Area,
                Controller = model.Controller,
                Action = model.Action,
                IconKey = model.IconKey,
                RequiredBranchFeatureCode = model.RequiredBranchFeatureCode,
                SortOrder = model.SortOrder,
                RequiresBranchSelection = model.RequiresBranchSelection,
                IsMenuItem = model.IsMenuItem,
                IsActive = model.IsActive,
                IsSystemFunction = existing?.IsSystemFunction ?? false,
                SupportsView = model.SupportsView,
                SupportsCreate = model.SupportsCreate,
                SupportsEdit = model.SupportsEdit,
                SupportsDelete = model.SupportsDelete,
                SupportsExport = model.SupportsExport
            };

            await _systemAdministrationService.SaveFunctionAsync(
                entity,
                User.GetUserId(),
                cancellationToken);

            TempData["Success"] = isCreate
                ? "Đã tạo chức năng hệ thống."
                : "Đã cập nhật chức năng hệ thống.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Edit", model);
        }
    }

    private Task<bool> HasPermissionAsync(
        string action,
        CancellationToken cancellationToken)
    {
        return _systemAdministrationService.HasPermissionAsync(
            User,
            "Admin",
            "SystemFunctions",
            action,
            cancellationToken);
    }

    private async Task<IReadOnlyList<SystemFunction>> GetParentOptionsAsync(
        Guid excludedId,
        CancellationToken cancellationToken)
    {
        return (await _systemAdministrationService
                .GetFunctionsAsync(cancellationToken))
            .Where(x => x.Id != excludedId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static SystemFunctionEditViewModel ToEditModel(
        SystemFunction function,
        IReadOnlyList<SystemFunction> parentOptions)
    {
        return new SystemFunctionEditViewModel
        {
            Id = function.Id,
            Code = function.Code,
            Name = function.Name,
            Description = function.Description,
            ParentId = function.ParentId,
            Area = function.Area,
            Controller = function.Controller,
            Action = function.Action,
            IconKey = function.IconKey,
            RequiredBranchFeatureCode = function.RequiredBranchFeatureCode,
            SortOrder = function.SortOrder,
            RequiresBranchSelection = function.RequiresBranchSelection,
            IsMenuItem = function.IsMenuItem,
            IsActive = function.IsActive,
            IsSystemFunction = function.IsSystemFunction,
            SupportsView = function.SupportsView,
            SupportsCreate = function.SupportsCreate,
            SupportsEdit = function.SupportsEdit,
            SupportsDelete = function.SupportsDelete,
            SupportsExport = function.SupportsExport,
            ParentOptions = parentOptions
        };
    }
}
