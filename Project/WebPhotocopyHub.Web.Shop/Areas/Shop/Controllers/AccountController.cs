using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Web.Areas.Shop.Controllers;

[Area("Shop")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IBranchManagementService _branchManagementService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IBranchManagementService branchManagementService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _branchManagementService = branchManagementService;
        _logger = logger;
    }

    [HttpGet("/{branchSlug}/Admin/Login")]
    public IActionResult Login(string branchSlug, string? returnUrl = null)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;
        ViewData["LoginScope"] = "Chủ shop";

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl ?? $"/{branch.Slug}/Admin"
        });
    }

    [HttpPost("/{branchSlug}/Admin/Login")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string branchSlug, LoginViewModel model)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;
        ViewData["LoginScope"] = "Chủ shop";

        return await LoginForRolesAsync(
            model,
            new[] { RoleConstants.ShopOperator, RoleConstants.Admin },
            branch.Slug,
            $"/{branch.Slug}/Admin",
            "Chủ shop");
    }

    private async Task<IActionResult> LoginForRolesAsync(
        LoginViewModel model,
        IReadOnlyCollection<string> allowedRoles,
        string branchSlug,
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

        if (!await CanLoginToBranchAsync(user, branchSlug))
        {
            await _signInManager.SignOutAsync();
            _logger.LogWarning("User {Email} attempted to login to branch {BranchSlug} without membership.", email, branchSlug);
            ModelState.AddModelError(string.Empty, "Tài khoản này chưa được phân quyền tại cơ sở đang đăng nhập.");
            return View("Login", model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return LocalRedirect(defaultRedirectUrl);
    }

    private async Task<bool> CanLoginToBranchAsync(ApplicationUser user, string branchSlug)
    {
        if (await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
        {
            return true;
        }

        var branch = await _branchManagementService.GetBySlugAsync(branchSlug, HttpContext.RequestAborted);
        if (branch is null)
        {
            return false;
        }

        var memberships = await _branchManagementService.GetMembershipsAsync(branch.Id, HttpContext.RequestAborted);
        return memberships.Any(x =>
            x.UserId == user.Id &&
            x.IsActive &&
            x.BranchRole?.IsActive == true);
    }
}
