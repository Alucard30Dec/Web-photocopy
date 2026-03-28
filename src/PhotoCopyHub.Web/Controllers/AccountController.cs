using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(model);
        }

        try
        {
            var user = new ApplicationUser
            {
                FullName = model.FullName,
                Email = email,
                UserName = email,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, LocalizeIdentityError(error));
                }

                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(RoleConstants.Customer))
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(RoleConstants.Customer));
                if (!createRoleResult.Succeeded)
                {
                    foreach (var error in createRoleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, LocalizeIdentityError(error));
                    }

                    return View(model);
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, RoleConstants.Customer);
            if (!addRoleResult.Succeeded)
            {
                foreach (var error in addRoleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, LocalizeIdentityError(error));
                }

                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Đăng ký thành công.";
            return await RedirectToRoleLandingAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đăng ký tài khoản thất bại cho email {Email}.", email);
            ModelState.AddModelError(string.Empty, "Đăng ký thất bại do lỗi hệ thống. Vui lòng thử lại sau.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại hoặc đã bị khóa.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần. Vui lòng thử lại sau.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return await RedirectToRoleLandingAsync(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Bạn đã đăng xuất.";
        return RedirectToAction("Index", "Home");
    }

    private static string LocalizeIdentityError(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" => "Email này đã được sử dụng.",
            "DuplicateUserName" => "Tên đăng nhập đã tồn tại.",
            "PasswordTooShort" => "Mật khẩu quá ngắn, cần ít nhất 8 ký tự.",
            "PasswordRequiresDigit" => "Mật khẩu cần có ít nhất 1 chữ số.",
            "PasswordRequiresLower" => "Mật khẩu cần có ít nhất 1 chữ thường.",
            "PasswordRequiresUpper" => "Mật khẩu cần có ít nhất 1 chữ hoa.",
            _ => error.Description
        };
    }

    private async Task<IActionResult> RedirectToRoleLandingAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (await _userManager.IsInRoleAsync(user, RoleConstants.ShopOperator))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Shop" });
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
