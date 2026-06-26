using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Admin.ViewComponents;

public sealed class AdminNavigationViewComponent : ViewComponent
{
    private readonly ISystemAdministrationService _systemAdministrationService;

    public AdminNavigationViewComponent(
        ISystemAdministrationService systemAdministrationService)
    {
        _systemAdministrationService = systemAdministrationService;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        IReadOnlyList<WebPhotocopyHub.Application.DTOs.SystemNavigationItemDto>? items = null)
    {
        var currentController =
            ViewComponentContext.ViewContext.RouteData.Values["controller"]
                ?.ToString()
            ?? string.Empty;

        return View(new AdminNavigationViewModel
        {
            CurrentController = currentController,
            Items = items ?? await _systemAdministrationService.GetNavigationAsync(
                HttpContext.User,
                HttpContext.RequestAborted)
        });
    }
}
