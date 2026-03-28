using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class DashboardController : Controller
{
    private readonly IWalletService _walletService;
    private readonly ITopUpService _topUpService;
    private readonly IPrintJobService _printJobService;
    private readonly IProductOrderService _productOrderService;
    private readonly ISupportServiceOrderService _supportServiceOrderService;

    public DashboardController(
        IWalletService walletService,
        ITopUpService topUpService,
        IPrintJobService printJobService,
        IProductOrderService productOrderService,
        ISupportServiceOrderService supportServiceOrderService)
    {
        _walletService = walletService;
        _topUpService = topUpService;
        _printJobService = printJobService;
        _productOrderService = productOrderService;
        _supportServiceOrderService = supportServiceOrderService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var topUps = await _topUpService.GetUserRequestsAsync(userId, cancellationToken);
        var printJobs = await _printJobService.GetUserOrdersAsync(userId, cancellationToken);
        var productOrders = await _productOrderService.GetUserOrdersAsync(userId, cancellationToken);
        var supportOrders = await _supportServiceOrderService.GetUserOrdersAsync(userId, cancellationToken);

        var vm = new DashboardViewModel
        {
            CurrentBalance = await _walletService.GetCurrentBalanceAsync(userId, cancellationToken),
            PendingTopUpCount = topUps.Count(x =>
                x.Status == TopUpStatus.Pending ||
                x.Status == TopUpStatus.PendingAdminApproval),
            PrintJobsCount = printJobs.Count,
            ProductOrdersCount = productOrders.Count,
            SupportOrdersCount = supportOrders.Count,
            RecentTransactions = (await _walletService.GetUserTransactionsAsync(userId, cancellationToken)).Take(10).ToList()
        };

        return View(vm);
    }
}
