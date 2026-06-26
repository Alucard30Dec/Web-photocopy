using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebPhotocopyHub.Application.Branching;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;

namespace WebPhotocopyHub.Web.Authorization;

public sealed class BranchAccessFilter : IAsyncAuthorizationFilter
{
    private readonly IBranchContext _branchContext;
    private readonly IBranchManagementService _branchManagementService;

    public BranchAccessFilter(
        IBranchContext branchContext,
        IBranchManagementService branchManagementService)
    {
        _branchContext = branchContext;
        _branchManagementService = branchManagementService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Filters.Any(x => x is Microsoft.AspNetCore.Mvc.Authorization.IAllowAnonymousFilter))
        {
            return;
        }

        var area = context.RouteData.Values["area"]?.ToString() ?? string.Empty;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        var branchSlug = context.RouteData.Values["branchSlug"]?.ToString();
        var accessArea = string.IsNullOrWhiteSpace(area) && !string.IsNullOrWhiteSpace(branchSlug)
            ? "Customer"
            : area;
        var rule = BranchAccessMap.Resolve(accessArea, controller, action);
        if (rule is null)
        {
            return;
        }

        if (rule.RequiresSelectedBranch && _branchContext.BranchId is null)
        {
            if (area.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("Index", "Branches", new { area = "Admin", reason = "select" });
                return;
            }

            context.Result = new NotFoundResult();
            return;
        }

        if (!_branchContext.IsActive)
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (accessArea.Equals("Customer", StringComparison.OrdinalIgnoreCase) &&
            !_branchContext.IsAcceptingOrders &&
            IsCustomerOrderCreation(controller, action, context.HttpContext.Request.Method))
        {
            context.Result = new ForbidResult();
            return;
        }

        var branchId = _branchContext.BranchId!.Value;
        if (!string.IsNullOrWhiteSpace(rule.FeatureCode) &&
            !await _branchManagementService.IsFeatureEnabledAsync(branchId, rule.FeatureCode, context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (context.HttpContext.User.IsInRole(RoleConstants.Admin))
        {
            return;
        }

        if (accessArea.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(rule.PermissionCode))
        {
            context.Result = new ForbidResult();
            return;
        }

        var allowed = await _branchManagementService.UserHasPermissionAsync(
            userId,
            branchId,
            rule.PermissionCode,
            context.HttpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }

    private static bool IsCustomerOrderCreation(string controller, string action, string method)
    {
        if (controller.Equals("PrintJobs", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("Create", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (controller.Equals("SupportOrders", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("Create", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return controller.Equals("Products", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("Index", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(method);
    }
}
