using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class DashboardController : Controller
{
    private readonly IPrintJobService _printJobService;
    private readonly IProductOrderService _productOrderService;
    private readonly ISupportServiceOrderService _supportServiceOrderService;
    private readonly IWalletService _walletService;
    private readonly ITopUpService _topUpService;

    public DashboardController(
        IPrintJobService printJobService,
        IProductOrderService productOrderService,
        ISupportServiceOrderService supportServiceOrderService,
        IWalletService walletService,
        ITopUpService topUpService)
    {
        _printJobService = printJobService;
        _productOrderService = productOrderService;
        _supportServiceOrderService = supportServiceOrderService;
        _walletService = walletService;
        _topUpService = topUpService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var printJobs = await _printJobService.GetUserOrdersAsync(userId, cancellationToken);
        var productOrders = await _productOrderService.GetUserOrdersAsync(userId, cancellationToken);
        var supportOrders = await _supportServiceOrderService.GetUserOrdersAsync(userId, cancellationToken);
        var transactions = await _walletService.GetUserTransactionsAsync(userId, cancellationToken);
        var topUpRequests = await _topUpService.GetUserRequestsAsync(userId, cancellationToken);

        var vm = new DashboardViewModel
        {
            CurrentBalance = await _walletService.GetCurrentBalanceAsync(userId, cancellationToken),
            PendingTopUpCount = topUpRequests.Count(x =>
                x.Status == PhotoCopyHub.Domain.Enums.TopUpStatus.Pending ||
                x.Status == PhotoCopyHub.Domain.Enums.TopUpStatus.PendingAdminApproval),
            PrintJobsCount = printJobs.Count,
            ProductOrdersCount = productOrders.Count,
            SupportOrdersCount = supportOrders.Count,
            RecentTransactions = transactions.Take(5).ToList()
        };

        return View(vm);
    }
}