using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class ReconciliationController : Controller
{
    private readonly IWalletReconciliationService _walletReconciliationService;

    public ReconciliationController(IWalletReconciliationService walletReconciliationService)
    {
        _walletReconciliationService = walletReconciliationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool includeMatched = false, CancellationToken cancellationToken = default)
    {
        var report = await _walletReconciliationService.ReconcileAsync(includeMatched, cancellationToken);
        ViewBag.IncludeMatched = includeMatched;
        return View(report);
    }
}
