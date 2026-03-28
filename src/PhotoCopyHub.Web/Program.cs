using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Infrastructure;
using PhotoCopyHub.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRazorPages();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{context.Connection.RemoteIpAddress}-auth",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("money", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{context.Connection.RemoteIpAddress}-money-{context.User.Identity?.Name}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.CustomerPortal, policy =>
        policy.RequireRole(RoleConstants.Customer, RoleConstants.Admin));

    options.AddPolicy(AppPolicies.ShopOperation, policy =>
        policy.RequireRole(RoleConstants.ShopOperator, RoleConstants.Admin));

    options.AddPolicy(AppPolicies.AdminOnly, policy =>
        policy.RequireRole(RoleConstants.Admin));

    options.AddPolicy(AppPolicies.BackOffice, policy =>
        policy.RequireRole(RoleConstants.ShopOperator, RoleConstants.Admin));

    options.AddPolicy(AppPolicies.TopUpReview, policy =>
        policy.RequireRole(RoleConstants.ShopOperator, RoleConstants.Admin));

    options.AddPolicy(AppPolicies.CounterTopUp, policy =>
        policy.RequireRole(RoleConstants.ShopOperator, RoleConstants.Admin));

    options.AddPolicy(AppPolicies.WalletAdjustment, policy =>
        policy.RequireRole(RoleConstants.Admin, RoleConstants.ShopOperator));

    options.AddPolicy(AppPolicies.ManageUsers, policy =>
        policy.RequireRole(RoleConstants.Admin));

    options.AddPolicy(AppPolicies.DownloadPrintFile, policy =>
        policy.RequireRole(RoleConstants.ShopOperator, RoleConstants.Admin));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com data:; " +
        "img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        var userId = userManager.GetUserId(context.User);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var isActive = await userManager.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == userId && x.IsActive);

            if (!isActive)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Account/Login");
                return;
            }
        }
    }

    await next();
});
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
