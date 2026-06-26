using Microsoft.AspNetCore.Http;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.Branching;

namespace WebPhotocopyHub.Infrastructure.Middleware;

public sealed class BranchContextMiddleware
{
    private readonly RequestDelegate _next;

    public BranchContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IBranchContext branchContext,
        IBranchManagementService branchManagementService)
    {
        branchContext.Clear();

        var routeSlug = httpContext.Request.RouteValues["branchSlug"]?.ToString();
        if (!string.IsNullOrWhiteSpace(routeSlug))
        {
            var routeBranch = await branchManagementService.GetBySlugAsync(routeSlug, httpContext.RequestAborted);
            if (routeBranch is not null)
            {
                branchContext.Set(routeBranch);
            }
        }

        var area = httpContext.Request.RouteValues["area"]?.ToString();
        if (branchContext.BranchId is null && string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var cookieValue = httpContext.Request.Cookies[BranchContextConstants.AdminBranchCookieName];
            if (Guid.TryParse(cookieValue, out var branchId))
            {
                var selectedBranch = await branchManagementService.GetByIdAsync(branchId, httpContext.RequestAborted);
                if (selectedBranch is not null)
                {
                    branchContext.Set(selectedBranch);
                }
            }
        }

        await _next(httpContext);
    }
}
