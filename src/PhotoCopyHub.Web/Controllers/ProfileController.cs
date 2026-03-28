using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

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

        var vm = new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        };

        return View(vm);
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

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["Success"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Index));
    }
}
