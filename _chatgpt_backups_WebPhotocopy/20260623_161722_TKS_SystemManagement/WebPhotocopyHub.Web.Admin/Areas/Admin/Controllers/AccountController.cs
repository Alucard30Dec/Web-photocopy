using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet("/Admin/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl ?? "/Admin"
        });
    }

    [HttpPost("/Admin/Login")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        return await LoginForRolesAsync(
            model,
            new[] { RoleConstants.Admin },
            "/Admin",
            "Admin hệ thống");
    }

    private async Task<IActionResult> LoginForRolesAsync(
        LoginViewModel model,
        IReadOnlyCollection<string> allowedRoles,
        string defaultRedirectUrl,
        string loginScope)
    {
        if (!ModelState.IsValid)
        {
            return View("Login", model);
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại hoặc đã bị khóa.");
            return View("Login", model);
        }

        var result = await _signInManager.PasswordSignInAsync(email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần. Vui lòng thử lại sau.");
                return View("Login", model);
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View("Login", model);
        }

        var roleAllowed = false;
        foreach (var role in allowedRoles)
        {
            if (await _userManager.IsInRoleAsync(user, role))
            {
                roleAllowed = true;
                break;
            }
        }

        if (!roleAllowed)
        {
            await _signInManager.SignOutAsync();
            _logger.LogWarning("User {Email} attempted to login to {LoginScope} without allowed role.", email, loginScope);
            ModelState.AddModelError(string.Empty, $"Tài khoản này không thuộc khu vực đăng nhập {loginScope}.");
            return View("Login", model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return LocalRedirect(defaultRedirectUrl);
    }
}