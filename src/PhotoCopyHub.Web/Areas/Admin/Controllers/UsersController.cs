using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWalletService _walletService;
    private readonly IAuditLogService _auditLogService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWalletService walletService,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _walletService = walletService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var roleMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            roleMap[user.Id] = roles.FirstOrDefault() ?? string.Empty;
        }

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
            TempData["Error"] = "Không thể tự khóa tài khoản của chính bạn.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

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
        if (!new[] { RoleConstants.Customer, RoleConstants.ShopOperator, RoleConstants.Admin }.Contains(roleName))
        {
            TempData["Error"] = "Role không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            TempData["Error"] = "Role chưa tồn tại trong hệ thống.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removableRoles = currentRoles.Where(x =>
            x == RoleConstants.Customer ||
            x == RoleConstants.ShopOperator ||
            x == RoleConstants.Admin).ToList();

        if (removableRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, removableRoles);
        }

        await _userManager.AddToRoleAsync(user, roleName);

        await _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = User.GetUserId(),
            Action = "SetUserRole",
            EntityName = nameof(ApplicationUser),
            EntityId = user.Id,
            Details = $"Role: {roleName}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);

        TempData["Success"] = "Cập nhật role người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }
}
