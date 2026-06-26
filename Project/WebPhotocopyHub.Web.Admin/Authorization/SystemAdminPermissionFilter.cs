using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.Security;

namespace WebPhotocopyHub.Web.Admin.Authorization;

public sealed class SystemAdminPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly ISystemAdministrationService _systemAdministrationService;

    public SystemAdminPermissionFilter(
        ISystemAdministrationService systemAdministrationService)
    {
        _systemAdministrationService = systemAdministrationService;
    }

    public async Task OnAuthorizationAsync(
        AuthorizationFilterContext context)
    {
        var area = context.RouteData.Values["area"]?.ToString();
        if (!string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any())
        {
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (string.IsNullOrWhiteSpace(controller)
            || string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var actionName = context.RouteData.Values["action"]?.ToString() ?? "Index";

        if (string.Equals(controller, "Branches", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "ClearSelection", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.HttpContext.User.IsInRole("Admin"))
            {
                context.Result = new ForbidResult();
            }

            return;
        }

        if (string.Equals(controller, "Branches", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "Select", StringComparison.OrdinalIgnoreCase))
        {
            var canUseAdminPortal = await _systemAdministrationService
                .CanAccessAdminPortalAsync(
                    context.HttpContext.User,
                    context.HttpContext.RequestAborted);
            if (!canUseAdminPortal)
            {
                context.Result = new ForbidResult();
            }

            return;
        }

        if (string.Equals(controller, "Dashboard", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "Index", StringComparison.OrdinalIgnoreCase))
        {
            var canViewDashboard = await _systemAdministrationService
                .HasPermissionAsync(
                    context.HttpContext.User,
                    "Admin",
                    controller,
                    SystemPermissionActions.View,
                    context.HttpContext.RequestAborted);
            if (canViewDashboard)
            {
                return;
            }

            var fallbackPath = await _systemAdministrationService
                .GetFirstAccessibleAdminPathAsync(
                    context.HttpContext.User,
                    context.HttpContext.RequestAborted);
            if (!string.IsNullOrWhiteSpace(fallbackPath)
                && !string.Equals(
                    fallbackPath,
                    "/Admin/Dashboard/Index",
                    StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectResult(fallbackPath);
                return;
            }

            context.Result = new ForbidResult();
            return;
        }

        var explicitPermission = context.ActionDescriptor.EndpointMetadata
            .OfType<SystemPermissionActionAttribute>()
            .LastOrDefault()
            ?.PermissionAction;

        var permissionAction = explicitPermission
            ?? await ResolvePermissionActionAsync(context, actionName);

        var allowed = await _systemAdministrationService.HasPermissionAsync(
            context.HttpContext.User,
            "Admin",
            controller,
            permissionAction,
            context.HttpContext.RequestAborted);

        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }

    private static async Task<string> ResolvePermissionActionAsync(
        AuthorizationFilterContext context,
        string actionName)
    {
        var request = context.HttpContext.Request;

        if (ContainsAny(actionName, "Delete", "Remove"))
        {
            return SystemPermissionActions.Delete;
        }

        if (ContainsAny(actionName, "Export", "Download"))
        {
            return SystemPermissionActions.Export;
        }

        if (ContainsAny(actionName, "Create", "New"))
        {
            return SystemPermissionActions.Create;
        }

        if (string.Equals(actionName, "Edit", StringComparison.OrdinalIgnoreCase)
            && await IsCreateStyleEditAsync(request))
        {
            return SystemPermissionActions.Create;
        }

        if (ContainsAny(
                actionName,
                "Edit",
                "Update",
                "Toggle",
                "Set",
                "Assign",
                "Adjust",
                "Review",
                "Refund",
                "Features",
                "RolePermissions",
                "ResetPassword",
                "Save"))
        {
            return SystemPermissionActions.Edit;
        }

        if (!HttpMethods.IsGet(request.Method)
            && !HttpMethods.IsHead(request.Method))
        {
            return SystemPermissionActions.Edit;
        }

        return SystemPermissionActions.View;
    }

    private static async Task<bool> IsCreateStyleEditAsync(
        HttpRequest request)
    {
        if (request.RouteValues.TryGetValue("id", out var routeId)
            && HasRealIdentifier(routeId?.ToString()))
        {
            return false;
        }

        if (request.Query.TryGetValue("id", out var queryId)
            && HasRealIdentifier(queryId.ToString()))
        {
            return false;
        }

        if (!request.HasFormContentType)
        {
            return true;
        }

        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
        if (!form.TryGetValue("Id", out var formId))
        {
            return true;
        }

        return !HasRealIdentifier(formId.ToString());
    }

    private static bool HasRealIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "0", StringComparison.Ordinal))
        {
            return false;
        }

        if (Guid.TryParse(value, out var guidValue))
        {
            return guidValue != Guid.Empty;
        }

        return true;
    }

    private static bool ContainsAny(
        string source,
        params string[] tokens)
    {
        return tokens.Any(token =>
            source.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
