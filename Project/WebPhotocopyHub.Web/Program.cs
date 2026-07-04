using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebPhotocopyHub.DataAccess;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Report;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Models;
using WebPhotocopyHub.Web.HealthChecks;
using WebPhotocopyHub.Web.Authorization;
using WebPhotocopyHub.Web.Admin.Authorization;
using WebPhotocopyHub.Web.Diagnostics;
using WebPhotocopyHub.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddWebPhotocopyHubDataAccess(builder.Configuration);
builder.Services.AddWebPhotocopyHubReports();
builder.Services.AddExceptionHandler<WebPhotocopyHubExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var correlationId = CorrelationIdContext.GetOrCreate(context.HttpContext);
        context.ProblemDetails.Extensions["correlationId"] = correlationId;
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
    };
});

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("Application process is running."),
        tags: new[] { "live", "ready" })
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: new[] { "db", "ready" });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.Filters.AddService<BranchAccessFilter>();
        options.Filters.AddService<SystemAdminPermissionFilter>();
    })
    .AddWebPhotocopyHubWebModules()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<BranchAccessFilter>();
builder.Services.AddScoped<SystemAdminPermissionFilter>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebPhotocopyHub API",
        Version = "v1",
        Description = "HTTP API cho catalog, báo giá, ví và đơn in của WebPhotocopyHub."
    });

    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = ".AspNetCore.Identity.Application",
        Description = "API đang dùng cookie đăng nhập ASP.NET Core Identity hiện có. Đăng nhập trên web rồi mở Swagger trong cùng trình duyệt."
    });
});

var apiCorsOrigins = builder.Configuration.GetSection("Api:Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (apiCorsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ApiCors", policy =>
        {
            policy
                .WithOrigins(apiCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

builder.Services.AddRazorPages();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DevelopmentConsoleLifetimeService>();

    if (builder.Configuration.GetValue<bool>("BrowserLaunch:Enabled"))
    {
        builder.Services.AddHostedService<DevelopmentBrowserLaunchService>();
    }
}

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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        if (CorrelationIdContext.IsApiRequest(context.HttpContext))
        {
            return WriteApiStatusProblemAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Chưa đăng nhập.",
                "Bạn cần đăng nhập để dùng chức năng này.",
                "unauthorized");
        }

        context.Response.Redirect(BuildLoginRedirectPath(context.HttpContext));
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (CorrelationIdContext.IsApiRequest(context.HttpContext))
        {
            return WriteApiStatusProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Không có quyền truy cập.",
                "Bạn không có quyền thực hiện thao tác này.",
                "forbidden");
        }

        context.Response.Redirect(options.AccessDeniedPath);
        return Task.CompletedTask;
    };
});
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
    });
}
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

if (GetBooleanConfigWithLegacyFallback(app.Configuration, "WEBPHOTOCOPYHUB_SEED_ONLY", "PHOTOCOPYHUB_SEED_ONLY"))
{
    app.Logger.LogInformation("WEBPHOTOCOPYHUB_SEED_ONLY=true, database initialization/seed completed. Application will exit without starting HTTP server.");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    if (!CorrelationIdContext.IsApiRequest(httpContext))
    {
        return;
    }

    if (httpContext.Response.HasStarted)
    {
        return;
    }

    var statusCode = httpContext.Response.StatusCode;
    if (statusCode < StatusCodes.Status400BadRequest)
    {
        return;
    }

    await WriteApiStatusProblemAsync(
        httpContext,
        statusCode,
        GetStatusCodeTitle(statusCode),
        GetStatusCodeDetail(statusCode),
        GetStatusCodeErrorCode(statusCode));
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebPhotocopyHub API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "WebPhotocopyHub API";
    });
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    var isSwaggerRequest = context.Request.Path.StartsWithSegments("/swagger");

    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] = isSwaggerRequest
        ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; frame-src 'self' blob:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'"
        : "default-src 'self'; " +
          "script-src 'self' https://cdn.jsdelivr.net; " +
          "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
          "font-src 'self' https://fonts.gstatic.com data:; " +
          "img-src 'self' data: blob:; frame-src 'self' blob:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.Use(async (context, next) =>
{
    if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
        IsCustomerLocalLandingPath(context.Request.Path) &&
        TryGetCustomerBranchSlugFromReferer(context, out var customerBranchSlug))
    {
        context.Response.Redirect(BuildCustomerBranchHomePath(context, customerBranchSlug));
        return;
    }

    await next();
});

if (apiCorsOrigins.Length > 0)
{
    app.UseCors("ApiCors");
}

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthCheckResponseAsync
}).AllowAnonymous();

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponseAsync
}).AllowAnonymous();
app.MapHealthChecks("/healthz/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = WriteHealthCheckResponseAsync
}).AllowAnonymous();

app.UseAuthentication();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    var correlationId = GetCorrelationId(context);
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        return Task.CompletedTask;
    });

    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("WebPhotocopyHub.RequestScope");

    var userId = context.User?.Identity?.IsAuthenticated == true
        ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        : null;

    using (logger.BeginScope(new Dictionary<string, object?>
    {
        ["CorrelationId"] = correlationId,
        ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
        ["UserId"] = userId,
        ["RequestPath"] = context.Request.Path.Value
    }))
    {
        await next();
    }
});

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        var adminUserQueryService = context.RequestServices.GetRequiredService<IAdminUserQueryService>();
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var isActive = await adminUserQueryService.IsActiveAsync(userId, context.RequestAborted);

            if (!isActive)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Home");
                return;
            }
        }
    }

    await next();
});

app.UseMiddleware<BranchContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

const string BranchSlugRouteConstraint =
    "^(?!admin$|Admin$|swagger$|Swagger$|healthz$|Healthz$|Customer$|Shop$|Home$|Account$|Dashboard$|PrintJobs$|Products$|SupportOrders$|Wallet$|Cart$|Orders$|Files$|Api$|api$)[A-Za-z0-9][A-Za-z0-9-]{2,63}$";

app.MapControllerRoute(
    name: "system-admin-login",
    pattern: "Admin/Login",
    defaults: new { area = "Admin", controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "admin-root",
    pattern: "Admin",
    defaults: new { area = "Admin", controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "admin-area",
    pattern: "Admin/{controller}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.MapControllerRoute(
    name: "shop-branch-admin-login",
    pattern: "{branchSlug}/Admin/Login",
    defaults: new { area = "Shop", controller = "Account", action = "Login" },
    constraints: new { branchSlug = BranchSlugRouteConstraint });

app.MapControllerRoute(
    name: "shop-branch-admin-root",
    pattern: "{branchSlug}/Admin",
    defaults: new { area = "Shop", controller = "Dashboard", action = "Index" },
    constraints: new { branchSlug = BranchSlugRouteConstraint });

app.MapControllerRoute(
    name: "shop-branch-admin",
    pattern: "{branchSlug}/Admin/{controller}/{action=Index}/{id?}",
    defaults: new { area = "Shop" },
    constraints: new { branchSlug = BranchSlugRouteConstraint });

app.MapControllerRoute(
    name: "shop-directory",
    pattern: "Shops",
    defaults: new { controller = "Shop", action = "Index" });

app.MapControllerRoute(
    name: "public-shop-portfolio",
    pattern: "Shop",
    defaults: new { controller = "Home", action = "Shop" });

app.MapControllerRoute(
    name: "public-customer",
    pattern: "Customer",
    defaults: new { controller = "Home", action = "Customer" });

app.MapControllerRoute(
    name: "shop-branch-customer-login",
    pattern: "{branchSlug}/Login",
    defaults: new { controller = "Account", action = "Login" },
    constraints: new { branchSlug = BranchSlugRouteConstraint });

app.MapControllerRoute(
    name: "shop-branch-customer-register",
    pattern: "{branchSlug}/Register",
    defaults: new { controller = "Account", action = "Register" },
    constraints: new { branchSlug = BranchSlugRouteConstraint });

app.MapControllerRoute(
    name: "shop-branch-customer",
    pattern: "{branchSlug}/{controller=Branch}/{action=Index}/{id?}",
    constraints: new
    {
        branchSlug = BranchSlugRouteConstraint,
        controller = "^(Branch|Dashboard|PrintJobs|Products|Wallet|SupportOrders|Profile)$"
    });

app.MapControllerRoute(
    name: "account-logout",
    pattern: "Account/Logout",
    defaults: new { controller = "Account", action = "Logout" });

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "home",
    pattern: "Home/{action=Index}/{id?}",
    defaults: new { controller = "Home" });
app.MapRazorPages();

// Codex 2026-07-04: In Development, clean up stale debug hosts that still own the configured port.
if (app.Environment.IsDevelopment())
{
    DevelopmentPortCleanup.StopStaleConfiguredPortOwners(app);
}

app.Run();

static string GetCorrelationId(HttpContext context)
{
    return CorrelationIdContext.GetOrCreate(context);
}

static Task WriteApiStatusProblemAsync(
    HttpContext context,
    int statusCode,
    string title,
    string detail,
    string code)
{
    var correlationId = CorrelationIdContext.GetOrCreate(context);

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json; charset=utf-8";
    context.Response.Headers[CorrelationIdContext.HeaderName] = correlationId;

    var problemDetails = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Type = "/problems/" + code,
        Instance = context.Request.Path
    };

    problemDetails.Extensions["code"] = code;
    problemDetails.Extensions["correlationId"] = correlationId;
    problemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

    return context.Response.WriteAsJsonAsync(problemDetails);
}

static string GetStatusCodeTitle(int statusCode)
{
    return statusCode switch
    {
        StatusCodes.Status400BadRequest => "Request không hợp lệ.",
        StatusCodes.Status401Unauthorized => "Chưa đăng nhập.",
        StatusCodes.Status403Forbidden => "Không có quyền truy cập.",
        StatusCodes.Status404NotFound => "Không tìm thấy dữ liệu.",
        StatusCodes.Status409Conflict => "Xung đột dữ liệu.",
        StatusCodes.Status413PayloadTooLarge => "File hoặc request quá dung lượng.",
        StatusCodes.Status429TooManyRequests => "Quá nhiều yêu cầu.",
        _ => "Có lỗi xảy ra."
    };
}

static string GetStatusCodeDetail(int statusCode)
{
    return statusCode switch
    {
        StatusCodes.Status400BadRequest => "Request không hợp lệ.",
        StatusCodes.Status401Unauthorized => "Bạn cần đăng nhập để dùng chức năng này.",
        StatusCodes.Status403Forbidden => "Bạn không có quyền thực hiện thao tác này.",
        StatusCodes.Status404NotFound => "Không tìm thấy dữ liệu được yêu cầu.",
        StatusCodes.Status409Conflict => "Dữ liệu đã thay đổi hoặc trạng thái không còn phù hợp.",
        StatusCodes.Status413PayloadTooLarge => "File hoặc request vượt quá giới hạn cho phép.",
        StatusCodes.Status429TooManyRequests => "Vui lòng thử lại sau ít phút.",
        _ => "Hệ thống gặp lỗi không mong muốn. Vui lòng cung cấp correlation ID khi cần hỗ trợ."
    };
}

static string GetStatusCodeErrorCode(int statusCode)
{
    return statusCode switch
    {
        StatusCodes.Status400BadRequest => "bad_request",
        StatusCodes.Status401Unauthorized => "unauthorized",
        StatusCodes.Status403Forbidden => "forbidden",
        StatusCodes.Status404NotFound => "not_found",
        StatusCodes.Status409Conflict => "conflict",
        StatusCodes.Status413PayloadTooLarge => "payload_too_large",
        StatusCodes.Status429TooManyRequests => "too_many_requests",
        _ => "unexpected_error"
    };
}

static bool IsCustomerLocalLandingPath(PathString path)
{
    var value = (path.Value ?? string.Empty).TrimEnd('/');

    if (string.IsNullOrWhiteSpace(value))
    {
        return true;
    }

    if (string.Equals(value, "/", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (string.Equals(value, "/Home", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (string.Equals(value, "/Home/Index", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (string.Equals(value, "/Customer", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (string.Equals(value, "/Customer/Index", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return false;
}

static bool TryGetCustomerBranchSlugFromReferer(HttpContext context, out string branchSlug)
{
    branchSlug = string.Empty;

    if (!context.Request.Headers.TryGetValue("Referer", out var refererValues))
    {
        return false;
    }

    var referer = refererValues.ToString();
    if (string.IsNullOrWhiteSpace(referer))
    {
        return false;
    }

    if (!Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
    {
        return false;
    }

    if (!string.Equals(refererUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (context.Request.Host.Port.HasValue && refererUri.Port != context.Request.Host.Port.Value)
    {
        return false;
    }

    var firstSegment = refererUri.AbsolutePath
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault();

    var branch = ShopBranchCatalog.Find(firstSegment);
    if (branch is null)
    {
        return false;
    }

    branchSlug = branch.Slug;
    return true;
}

static string BuildCustomerBranchHomePath(HttpContext context, string branchSlug)
{
    var pathBase = context.Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
    var cleanSlug = branchSlug.Trim('/');

    if (string.IsNullOrWhiteSpace(pathBase))
    {
        return "/" + cleanSlug;
    }

    return pathBase + "/" + cleanSlug;
}
static string BuildLoginRedirectPath(HttpContext context)
{
    var path = context.Request.Path.Value ?? "/";
    var returnUrl = Uri.EscapeDataString(path + context.Request.QueryString);
    var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

    if (segments.Length > 0 && string.Equals(segments[0], "Admin", StringComparison.OrdinalIgnoreCase))
    {
        return $"/Admin/Login?returnUrl={returnUrl}";
    }

    if (segments.Length > 1 &&
        ShopBranchCatalog.IsKnownSlug(segments[0]) &&
        string.Equals(segments[1], "Admin", StringComparison.OrdinalIgnoreCase))
    {
        return $"/{segments[0]}/Admin/Login?returnUrl={returnUrl}";
    }

    if (segments.Length > 0 && ShopBranchCatalog.IsKnownSlug(segments[0]))
    {
        return $"/{segments[0]}/Login?returnUrl={returnUrl}";
    }

    return "/Home";
}

static bool GetBooleanConfigWithLegacyFallback(IConfiguration configuration, string key, string legacyKey)
{
    return configuration.GetValue<bool>(key) || configuration.GetValue<bool>(legacyKey);
}

static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var payload = new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.ToDictionary(
            x => x.Key,
            x => new
            {
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                durationMs = x.Value.Duration.TotalMilliseconds,
                error = x.Value.Exception?.Message
            })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
