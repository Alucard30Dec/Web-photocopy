using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IPricingService _pricingService;

    public HomeController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public async Task<IActionResult> Pricing(CancellationToken cancellationToken)
    {
        var rules = await _pricingService.GetActiveRulesAsync(cancellationToken);
        return View(rules);
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
