using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Web.Controllers;

[AllowAnonymous]
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

    [HttpGet("/{branchSlug}/Register")]
    public IActionResult Register(string branchSlug)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;
        ViewData["LoginScope"] = "Khách hàng";
        return View(new RegisterViewModel());
    }

    [HttpPost("/{branchSlug}/Register")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string branchSlug, RegisterViewModel model)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;
        ViewData["LoginScope"] = "Khách hàng";

        if (!ModelState.IsValid)
        {
            return View("Register", model);
        }

        var email = model.Email.Trim();
        var userName = model.UserName.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Gmail/Email này đã được sử dụng.");
            return View("Register", model);
        }

        if (await _userManager.FindByNameAsync(userName) is not null)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập này đã được sử dụng.");
            return View("Register", model);
        }

        try
        {
            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                Email = email,
                UserName = userName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                EmailConfirmed = true,
                PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(model.PhoneNumber),
                IsActive = true,
                CurrentBalance = 0
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View("Register", model);
            }

            var roleResult = await EnsureCustomerRoleAndAssignAsync(user);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                return View("Register", model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Đăng ký tài khoản khách hàng thành công.";
            return LocalRedirect($"/{branch.Slug}/Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đăng ký tài khoản khách hàng thất bại cho email {Email}.", email);
            ModelState.AddModelError(string.Empty, "Đăng ký thất bại do lỗi hệ thống. Vui lòng thử lại sau.");
            return View("Register", model);
        }
    }

    [HttpGet("/{branchSlug}/Login")]
    public IActionResult Login(string branchSlug, string? returnUrl = null)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;
        ViewData["LoginScope"] = "Khách hàng";

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl ?? $"/{branch.Slug}/Dashboard"
        });
    }

    [HttpPost("/{branchSlug}/Login")]
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
        ViewData["LoginScope"] = "Khách hàng";

        return await LoginForRolesAsync(
            model,
            new[] { RoleConstants.Customer },
            $"/{branch.Slug}/Dashboard",
            "Khách hàng");
    }

    [HttpPost("/{branchSlug}/ExternalLogin")]
    [EnableRateLimiting("auth")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string branchSlug, string provider, string? returnUrl = null)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            TempData["Error"] = "Nhà cung cấp đăng nhập không hợp lệ.";
            return LocalRedirect($"/{branch.Slug}/Login");
        }

        var safeReturnUrl = BuildSafeReturnUrl(returnUrl, $"/{branch.Slug}/Dashboard");
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new
        {
            branchSlug = branch.Slug,
            returnUrl = safeReturnUrl
        });

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("/{branchSlug}/ExternalLoginCallback")]
    public async Task<IActionResult> ExternalLoginCallback(string branchSlug, string? returnUrl = null, string? remoteError = null)
    {
        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;
        ViewData["LoginScope"] = "Khách hàng";

        var safeReturnUrl = BuildSafeReturnUrl(returnUrl, $"/{branch.Slug}/Dashboard");

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            TempData["Error"] = "Đăng nhập Google/Gmail thất bại: " + remoteError;
            return LocalRedirect($"/{branch.Slug}/Login?returnUrl={Uri.EscapeDataString(safeReturnUrl)}");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["Error"] = "Không lấy được thông tin đăng nhập Google/Gmail.";
            return LocalRedirect($"/{branch.Slug}/Login?returnUrl={Uri.EscapeDataString(safeReturnUrl)}");
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var linkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (linkedUser is null || !linkedUser.IsActive || !await _userManager.IsInRoleAsync(linkedUser, RoleConstants.Customer))
            {
                await _signInManager.SignOutAsync();
                TempData["Error"] = "Tài khoản Google/Gmail này không thuộc khu vực khách hàng hoặc đã bị khóa.";
                return LocalRedirect($"/{branch.Slug}/Login");
            }

            TempData["Success"] = "Đăng nhập Google/Gmail thành công.";
            return LocalRedirect(safeReturnUrl);
        }

        if (signInResult.IsLockedOut)
        {
            TempData["Error"] = "Tài khoản đang bị khóa tạm thời. Vui lòng thử lại sau.";
            return LocalRedirect($"/{branch.Slug}/Login");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Google/Gmail không trả về email. Không thể tạo tài khoản tự động.";
            return LocalRedirect($"/{branch.Slug}/Login");
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = email;
            }

            existingUser = new ApplicationUser
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                IsActive = true,
                CurrentBalance = 0
            };

            var createResult = await _userManager.CreateAsync(existingUser);
            if (!createResult.Succeeded)
            {
                TempData["Error"] = "Không thể tạo tài khoản từ Google/Gmail.";
                AddIdentityErrors(createResult);
                return LocalRedirect($"/{branch.Slug}/Register");
            }
        }

        if (!existingUser.IsActive)
        {
            TempData["Error"] = "Tài khoản này đã bị khóa.";
            return LocalRedirect($"/{branch.Slug}/Login");
        }

        var roleResult = await EnsureCustomerRoleAndAssignAsync(existingUser);
        if (!roleResult.Succeeded)
        {
            TempData["Error"] = "Không thể gán quyền khách hàng cho tài khoản Google/Gmail.";
            return LocalRedirect($"/{branch.Slug}/Login");
        }

        var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
        if (!addLoginResult.Succeeded)
        {
            var alreadyLinked = addLoginResult.Errors.Any(x => string.Equals(x.Code, "LoginAlreadyAssociated", StringComparison.OrdinalIgnoreCase));
            if (!alreadyLinked)
            {
                TempData["Error"] = "Không thể liên kết tài khoản Google/Gmail.";
                return LocalRedirect($"/{branch.Slug}/Login");
            }
        }

        await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
        TempData["Success"] = "Đăng nhập Google/Gmail thành công.";
        return LocalRedirect(safeReturnUrl);
    }

    [Authorize]
    [HttpPost("/Account/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Bạn đã đăng xuất.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
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

        var identifier = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(identifier);
        if (user is null)
        {
            user = await _userManager.FindByNameAsync(identifier);
        }

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại hoặc đã bị khóa.");
            return View("Login", model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần. Vui lòng thử lại sau.");
                return View("Login", model);
            }

            ModelState.AddModelError(string.Empty, "Email/Gmail, tên đăng nhập hoặc mật khẩu không đúng.");
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
            ModelState.AddModelError(string.Empty, $"Tài khoản này không thuộc khu vực đăng nhập {loginScope}.");
            return View("Login", model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return LocalRedirect(defaultRedirectUrl);
    }

    private async Task<IdentityResult> EnsureCustomerRoleAndAssignAsync(ApplicationUser user)
    {
        if (!await _roleManager.RoleExistsAsync(RoleConstants.Customer))
        {
            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(RoleConstants.Customer));
            if (!createRoleResult.Succeeded)
            {
                return createRoleResult;
            }
        }

        if (await _userManager.IsInRoleAsync(user, RoleConstants.Customer))
        {
            return IdentityResult.Success;
        }

        return await _userManager.AddToRoleAsync(user, RoleConstants.Customer);
    }

    private string BuildSafeReturnUrl(string? returnUrl, string defaultRedirectUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return defaultRedirectUrl;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, LocalizeIdentityError(error));
        }
    }

    private static string LocalizeIdentityError(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" => "Gmail/Email này đã được sử dụng.",
            "DuplicateUserName" => "Tên đăng nhập đã tồn tại.",
            "PasswordTooShort" => "Mật khẩu quá ngắn, cần ít nhất 8 ký tự.",
            "PasswordRequiresDigit" => "Mật khẩu cần có ít nhất 1 chữ số.",
            "PasswordRequiresLower" => "Mật khẩu cần có ít nhất 1 chữ thường.",
            "PasswordRequiresUpper" => "Mật khẩu cần có ít nhất 1 chữ hoa.",
            _ => error.Description
        };
    }
}