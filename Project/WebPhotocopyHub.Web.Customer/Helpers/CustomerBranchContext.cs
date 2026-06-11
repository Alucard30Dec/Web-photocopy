using Microsoft.AspNetCore.Mvc.Rendering;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Web.Customer.Helpers;

public static class CustomerBranchContext
{
    public static string GetSlug(ViewContext viewContext)
    {
        var routeSlug = viewContext.RouteData.Values["branchSlug"]?.ToString();
        var knownRouteBranch = ShopBranchCatalog.Find(routeSlug);
        if (knownRouteBranch is not null)
        {
            return knownRouteBranch.Slug;
        }

        var viewDataBranch = viewContext.ViewData["Branch"] as ShopBranchLinkViewModel;
        if (!string.IsNullOrWhiteSpace(viewDataBranch?.Slug))
        {
            var knownViewDataBranch = ShopBranchCatalog.Find(viewDataBranch.Slug);
            if (knownViewDataBranch is not null)
            {
                return knownViewDataBranch.Slug;
            }

            return viewDataBranch.Slug;
        }

        var currentPath = viewContext.HttpContext.Request.Path.Value ?? string.Empty;
        var firstSegment = currentPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        var knownPathBranch = ShopBranchCatalog.Find(firstSegment);
        if (knownPathBranch is not null)
        {
            return knownPathBranch.Slug;
        }

        return string.Empty;
    }

    public static ShopBranchLinkViewModel GetBranch(ViewContext viewContext)
    {
        var fromViewData = viewContext.ViewData["Branch"] as ShopBranchLinkViewModel;
        if (fromViewData is not null && !string.IsNullOrWhiteSpace(fromViewData.Slug))
        {
            return fromViewData;
        }

        var slug = GetSlug(viewContext);
        var branch = ShopBranchCatalog.Find(slug);
        if (branch is not null)
        {
            return branch;
        }

        return new ShopBranchLinkViewModel
        {
            Slug = slug,
            Name = string.IsNullOrWhiteSpace(slug) ? "Cơ sở photocopy" : slug,
            Address = "Đang cập nhật",
            PhoneNumber = "Đang cập nhật",
            OpenHours = "Đang cập nhật"
        };
    }

    public static string Home(ViewContext viewContext)
    {
        return ToPath(viewContext, string.Empty);
    }

    public static string ToPath(ViewContext viewContext, string relativePath)
    {
        var request = viewContext.HttpContext.Request;
        var pathBase = request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
        var slug = GetSlug(viewContext).Trim('/');

        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.IsNullOrWhiteSpace(pathBase) ? "/" : pathBase + "/";
        }

        var cleanRelativePath = (relativePath ?? string.Empty).Trim('/');

        var url = "/" + slug;
        if (!string.IsNullOrWhiteSpace(cleanRelativePath))
        {
            url += "/" + cleanRelativePath;
        }

        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            url = pathBase + url;
        }

        return url;
    }

    public static string LoginWithReturn(ViewContext viewContext, string returnUrl)
    {
        return ToPath(viewContext, "Login") + "?returnUrl=" + Uri.EscapeDataString(returnUrl);
    }
}