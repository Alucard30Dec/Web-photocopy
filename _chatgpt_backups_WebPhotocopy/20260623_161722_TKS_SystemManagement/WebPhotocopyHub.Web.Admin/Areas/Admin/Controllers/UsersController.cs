using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web.Admin.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWalletService _walletService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAdminUserQueryService _adminUserQueryService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWalletService walletService,
        IAuditLogService auditLogService,
        IAdminUserQueryService adminUserQueryService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _walletService = walletService;
        _auditLogService = auditLogService;
        _adminUserQueryService = adminUserQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _adminUserQueryService.ListUsersAsync(cancellationToken);
        var roleMap = await _adminUserQueryService.GetPrimaryRoleMapAsync(users, cancellationToken);

        ViewBag.RoleMap = roleMap;
        ViewBag.AvailableRoles = new[] { RoleConstants.Customer, RoleConstants.ShopOperator, RoleConstants.Admin };
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> AdjustBalance(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        ViewBag.TargetUser = user;
        return View(new ManualAdjustBalanceViewModel { UserId = userId });
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustBalance(ManualAdjustBalanceViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return NotFound();
        }

        ViewBag.TargetUser = user;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _walletService.ManualAdjustAsync(new WalletOperationRequestDto
            {
                UserId = model.UserId,
                Amount = model.Amount,
                TransactionType = WalletTransactionType.ManualAdjustment,
                Note = model.Note,
                IdempotencyKey = model.IdempotencyKey,
                PerformedByAdminId = User.GetUserId()
            }, cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = User.GetUserId(),
                Action = "ManualAdjustBalance",
                EntityName = nameof(ApplicationUser),
                EntityId = model.UserId,
                Details = $"Amount: {model.Amount}; Note: {model.Note}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Điều chỉnh số dư thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (string.Equals(User.GetUserId(), userId, StringComparison.Ordinal))
        {
            TempData["Error"] = "Không thể tự khóa tài khoản quản trị đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (user.IsActive && await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
        {
            var activeAdmins = (await _userManager.GetUsersInRoleAsync(RoleConstants.Admin))
                .Count(x => x.IsActive);

            if (activeAdmins <= 1)
            {
                TempData["Error"] = "Không thể khóa quản trị viên hoạt động cuối cùng của hệ thống.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.IsActive = !user.IsActive;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            TempData["Error"] = string.Join("; ", updateResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Index));
        }

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "ToggleUserActive",
            EntityName = nameof(ApplicationUser),
            EntityId = user.Id,
            Details = $"IsActive: {user.IsActive}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string roleName, CancellationToken cancellationToken)
    {
        var allowedRoles = new[] { RoleConstants.Customer, RoleConstants.ShopOperator, RoleConstants.Admin };
        if (!allowedRoles.Contains(roleName, StringComparer.Ordinal))
        {
            TempData["Error"] = "Role không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (string.Equals(User.GetUserId(), userId, StringComparison.Ordinal)
            && !string.Equals(roleName, RoleConstants.Admin, StringComparison.Ordinal))
        {
            TempData["Error"] = "Không thể tự hạ quyền tài khoản quản trị đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            TempData["Error"] = "Role chưa tồn tại trong hệ thống.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(RoleConstants.Admin, StringComparer.Ordinal)
            && !string.Equals(roleName, RoleConstants.Admin, StringComparison.Ordinal)
            && user.IsActive)
        {
            var activeAdmins = (await _userManager.GetUsersInRoleAsync(RoleConstants.Admin))
                .Count(x => x.IsActive);

            if (activeAdmins <= 1)
            {
                TempData["Error"] = "Không thể hạ quyền quản trị viên hoạt động cuối cùng của hệ thống.";
                return RedirectToAction(nameof(Index));
            }
        }

        if (!currentRoles.Contains(roleName, StringComparer.Ordinal))
        {
            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ", addResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        var removableRoles = currentRoles
            .Where(x => allowedRoles.Contains(x, StringComparer.Ordinal)
                        && !string.Equals(x, roleName, StringComparison.Ordinal))
            .ToList();

        if (removableRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, removableRoles);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = "Đã thêm role mới nhưng chưa xóa hết role cũ: "
                                    + string.Join("; ", removeResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "SetUserRole",
            EntityName = nameof(ApplicationUser),
            EntityId = user.Id,
            Details = $"PreviousRoles: {string.Join(",", currentRoles)}; NewRole: {roleName}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        TempData["Success"] = "Cập nhật role người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }
}
