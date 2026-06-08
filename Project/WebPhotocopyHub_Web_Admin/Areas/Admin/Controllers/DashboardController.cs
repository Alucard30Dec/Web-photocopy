using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class DashboardController : Controller
{
    [HttpGet("/Admin/Dashboard")]
    public IActionResult AdminDashboardCanonicalRedirect()
    {
        return LocalRedirect("/Admin");
    }

    private readonly IBackOfficeDashboardQueryService _dashboardQueryService;

    public DashboardController(IBackOfficeDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _dashboardQueryService.GetAdminSummaryAsync(cancellationToken);
        ViewBag.TotalUsers = summary.TotalUsers;
        ViewBag.PendingTopUps = summary.PendingTopUps;
        ViewBag.PrintJobsPending = summary.PrintJobsPending;
        ViewBag.TotalWalletTransactions = summary.TotalWalletTransactions;
        ViewBag.ActiveProducts = summary.ActiveProducts;
        ViewBag.ActiveSupportServices = summary.ActiveSupportServices;

        return View();
    }
}
