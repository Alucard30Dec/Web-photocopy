using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web;

namespace PhotoCopyHub.Web.Areas.Shop.Controllers;

[Area("Shop")]
[Authorize(Policy = AppPolicies.ShopOperation)]
public class DashboardController : Controller
{
    [HttpGet("/{branchSlug}/Admin/Dashboard")]
    public IActionResult ShopDashboardCanonicalRedirect(string branchSlug)
    {
        return LocalRedirect($"/{branchSlug}/Admin");
    }

    private readonly IBackOfficeDashboardQueryService _dashboardQueryService;

    public DashboardController(IBackOfficeDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _dashboardQueryService.GetShopSummaryAsync(cancellationToken);
        ViewBag.PendingTopUp = summary.PendingTopUp;
        ViewBag.PendingAdminTopUp = summary.PendingAdminTopUp;
        ViewBag.PrintQueue = summary.PrintQueue;
        ViewBag.ProductOrdersWaiting = summary.ProductOrdersWaiting;
        ViewBag.SupportOrdersWaiting = summary.SupportOrdersWaiting;
        ViewBag.LowStockProducts = summary.LowStockProducts;

        return View(summary.LatestLowStockProducts);
    }
}
