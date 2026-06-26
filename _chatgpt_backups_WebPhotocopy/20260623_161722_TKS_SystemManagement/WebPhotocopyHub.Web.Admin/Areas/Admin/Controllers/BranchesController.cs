using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Branching;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public sealed class BranchesController : Controller
{
    private readonly IBranchManagementService _branchManagementService;
    private readonly IBranchContext _branchContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public BranchesController(
        IBranchManagementService branchManagementService,
        IBranchContext branchContext,
        UserManager<ApplicationUser> userManager)
    {
        _branchManagementService = branchManagementService;
        _branchContext = branchContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? reason, CancellationToken cancellationToken)
    {
        ViewBag.CurrentBranchId = _branchContext.BranchId;
        if (string.Equals(reason, "select", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Warning"] = "Hãy chọn một cơ sở trước khi mở chức năng nghiệp vụ.";
        }

        return View(await _branchManagementService.GetAllAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue)
        {
            return View(new BranchEditViewModel());
        }

        var branch = await _branchManagementService.GetByIdAsync(id.Value, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        return View(ToEditModel(branch));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BranchEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var saved = await _branchManagementService.SaveAsync(
                new Branch
                {
                    Id = model.Id ?? Guid.Empty,
                    Code = model.Code,
                    Slug = model.Slug,
                    Name = model.Name,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    OpenHours = model.OpenHours,
                    ShortDescription = model.ShortDescription,
                    CustomerNote = model.CustomerNote,
                    PopularServices = model.PopularServices,
                    QuickOptions = model.QuickOptions,
                    IsActive = model.IsActive,
                    IsAcceptingOrders = model.IsAcceptingOrders
                },
                GetActorUserId(),
                cancellationToken);

            TempData["Success"] = "Đã lưu thông tin cơ sở.";
            return RedirectToAction(nameof(Edit), new { id = saved.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Features(Guid id, CancellationToken cancellationToken)
    {
        var branch = await _branchManagementService.GetByIdAsync(id, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        var enabled = await _branchManagementService.GetEnabledFeaturesAsync(id, cancellationToken);
        return View(new BranchFeaturesViewModel
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            Features = BranchFeatureCodes.All.Select(x => new BranchFeatureOptionViewModel
            {
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsEnabled = enabled.Contains(x.Code)
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Features(BranchFeaturesViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || await _branchManagementService.GetByIdAsync(model.BranchId, cancellationToken) is null)
        {
            TempData["Error"] = "Cấu hình chức năng không hợp lệ hoặc cơ sở không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        var enabledCodes = model.Features.Where(x => x.IsEnabled).Select(x => x.Code);
        await _branchManagementService.SetFeaturesAsync(
            model.BranchId,
            enabledCodes,
            GetActorUserId(),
            cancellationToken);
        TempData["Success"] = "Đã cập nhật các chức năng được phép sử dụng tại cơ sở.";
        return RedirectToAction(nameof(Features), new { id = model.BranchId });
    }

    [HttpGet]
    public async Task<IActionResult> Users(Guid id, CancellationToken cancellationToken)
    {
        var branch = await _branchManagementService.GetByIdAsync(id, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        var users = await _userManager.Users
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);

        return View(new BranchUsersViewModel
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            Users = users,
            Roles = await _branchManagementService.GetRolesAsync(branch.Id, cancellationToken),
            Memberships = await _branchManagementService.GetMembershipsAsync(branch.Id, cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> RolePermissions(Guid id, CancellationToken cancellationToken)
    {
        var role = await _branchManagementService.GetRoleAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        var branch = await _branchManagementService.GetByIdAsync(role.BranchId, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        var granted = role.Permissions.Select(x => x.PermissionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return View(new BranchRolePermissionsViewModel
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            BranchRoleId = role.Id,
            RoleName = role.Name,
            RoleDescription = role.Description,
            Permissions = BranchPermissionCodes.Definitions.Select(x => new BranchPermissionOptionViewModel
            {
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsGranted = granted.Contains(x.Code)
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RolePermissions(BranchRolePermissionsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ma trận quyền chưa hợp lệ.";
            return RedirectToAction(nameof(Users), new { id = model.BranchId });
        }

        var selected = model.Permissions.Where(x => x.IsGranted).Select(x => x.Code);
        await _branchManagementService.SetRolePermissionsAsync(
            model.BranchRoleId,
            selected,
            GetActorUserId(),
            cancellationToken);
        TempData["Success"] = "Đã cập nhật quyền của vai trò tại cơ sở.";
        return RedirectToAction(nameof(RolePermissions), new { id = model.BranchRoleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignUser(AssignBranchUserViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin phân công chưa hợp lệ.";
            return RedirectToAction(nameof(Users), new { id = model.BranchId });
        }

        try
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null || !user.IsActive)
            {
                throw new BusinessException("Không tìm thấy người dùng đang hoạt động.");
            }

            if (!await _userManager.IsInRoleAsync(user, RoleConstants.Admin) &&
                !await _userManager.IsInRoleAsync(user, RoleConstants.ShopOperator))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.ShopOperator);
                if (!roleResult.Succeeded)
                {
                    throw new BusinessException(string.Join("; ", roleResult.Errors.Select(x => x.Description)));
                }
            }

            await _branchManagementService.AssignUserAsync(
                model.BranchId,
                model.UserId,
                model.BranchRoleId,
                model.IsPrimary,
                GetActorUserId(),
                cancellationToken);
            TempData["Success"] = "Đã phân công người dùng vào cơ sở và cấp quyền đăng nhập khu vận hành.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { id = model.BranchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(Guid membershipId, Guid branchId, CancellationToken cancellationToken)
    {
        try
        {
            await _branchManagementService.RemoveUserAsync(membershipId, GetActorUserId(), cancellationToken);
            TempData["Success"] = "Đã thu hồi quyền truy cập cơ sở.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { id = branchId });
    }

    [HttpGet]
    public async Task<IActionResult> Select(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var branch = await _branchManagementService.GetByIdAsync(id, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        Response.Cookies.Append(
            BranchContextConstants.AdminBranchCookieName,
            branch.Id.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

        TempData["Success"] = $"Đang quản lý cơ sở: {branch.Name}.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpGet]
    public IActionResult ClearSelection()
    {
        Response.Cookies.Delete(BranchContextConstants.AdminBranchCookieName);
        TempData["Success"] = "Đã chuyển về phạm vi toàn hệ thống.";
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    private string GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Không xác định được người dùng hiện tại.");
    }

    private static BranchEditViewModel ToEditModel(Branch branch)
    {
        return new BranchEditViewModel
        {
            Id = branch.Id,
            Code = branch.Code,
            Slug = branch.Slug,
            Name = branch.Name,
            Address = branch.Address,
            PhoneNumber = branch.PhoneNumber,
            Email = branch.Email,
            OpenHours = branch.OpenHours,
            ShortDescription = branch.ShortDescription,
            CustomerNote = branch.CustomerNote,
            PopularServices = branch.PopularServices,
            QuickOptions = branch.QuickOptions,
            IsActive = branch.IsActive,
            IsAcceptingOrders = branch.IsAcceptingOrders
        };
    }
}
