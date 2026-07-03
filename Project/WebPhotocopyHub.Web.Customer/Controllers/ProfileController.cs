using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Customer.Models;

namespace WebPhotocopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.FindByIdAsync(User.GetUserId());
        if (user is null)
        {
            return NotFound();
        }

        return View(ToViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(User.GetUserId());
        if (user is null)
        {
            return NotFound();
        }

        var requestedUserName = model.UserName.Trim();
        var userWithSameName = await _userManager.FindByNameAsync(requestedUserName);
        if (userWithSameName is not null && userWithSameName.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập này đã được sử dụng.");
            model.Email = user.Email ?? string.Empty;
            model.CreatedAt = user.CreatedAt;
            model.IsActive = user.IsActive;
            return View(model);
        }

        user.UserName = requestedUserName;
        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Email = user.Email ?? string.Empty;
            model.CreatedAt = user.CreatedAt;
            model.IsActive = user.IsActive;
            return View(model);
        }

        TempData["Success"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Index), BranchRouteValues());
    }

    [HttpGet("ChangePassword")]
    public IActionResult ChangePassword()
    {
        return View(new WebPhotocopyHub.Web.Models.ChangePasswordViewModel());
    }

    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(WebPhotocopyHub.Web.Models.ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(User.GetUserId());
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        TempData["Success"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Index), BranchRouteValues());
    }

    private static ProfileViewModel ToViewModel(ApplicationUser user)
    {
        return new ProfileViewModel
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }

    private object? BranchRouteValues()
    {
        var branchSlug = RouteData.Values["branchSlug"]?.ToString();
        return string.IsNullOrWhiteSpace(branchSlug) ? null : new { branchSlug };
    }
}