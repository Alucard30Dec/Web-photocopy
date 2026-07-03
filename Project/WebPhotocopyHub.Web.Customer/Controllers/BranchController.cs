using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web.Customer.Models;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Web.Controllers;

[AllowAnonymous]
public class BranchController : Controller
{
    private readonly IPricingService _pricingService;
    private readonly IPrintJobService _printJobService;

    public BranchController(IPricingService pricingService, IPrintJobService printJobService)
    {
        _pricingService = pricingService;
        _printJobService = printJobService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string branchSlug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(branchSlug))
        {
            return NotFound();
        }

        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        ViewData["Branch"] = branch;

        var pricingRules = await _pricingService.GetActiveRulesAsync(cancellationToken);

        var recentPrintJobs = new List<WebPhotocopyHub.Domain.Entities.PrintJob>();
        var activePrintJobCount = 0;
        var isSignedIn = User.Identity?.IsAuthenticated == true;

        if (isSignedIn)
        {
            var userPrintJobs = (await _printJobService.GetUserOrdersAsync(User.GetUserId(), 1, 1000, cancellationToken)).Items;
            recentPrintJobs = userPrintJobs
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .ToList();

            activePrintJobCount = userPrintJobs.Count(x =>
                x.Status != PrintJobStatus.Completed &&
                x.Status != PrintJobStatus.Cancelled &&
                x.Status != PrintJobStatus.Refunded);
        }

        var viewModel = new BranchHomeViewModel
        {
            Branch = branch,
            QuickPrices = pricingRules
                .OrderBy(x => x.PaperSize)
                .ThenBy(x => x.ColorMode)
                .ThenBy(x => x.PrintSide)
                .Take(6)
                .Select(x => new BranchQuickPriceViewModel
                {
                    Name = $"{x.PaperSize.GetDisplayName()} · {x.ColorMode.GetDisplayName()}",
                    Description = x.PrintSide.GetDisplayName(),
                    UnitPrice = x.UnitPrice
                })
                .ToList(),
            RecentPrintJobs = recentPrintJobs,
            ActivePrintJobCount = activePrintJobCount,
            IsSignedIn = isSignedIn
        };

        return View(viewModel);
    }
}