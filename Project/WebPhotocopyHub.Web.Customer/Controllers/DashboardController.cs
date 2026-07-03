using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Customer.Models;

namespace WebPhotocopyHub.Web.Controllers;

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

        var printJobs = (await _printJobService.GetUserOrdersAsync(userId, 1, 1000, cancellationToken)).Items;
        var productOrders = (await _productOrderService.GetUserOrdersAsync(userId, 1, 1000, cancellationToken)).Items;
        var supportOrders = (await _supportServiceOrderService.GetUserOrdersAsync(userId, 1, 1000, cancellationToken)).Items;
        var transactions = (await _walletService.GetUserTransactionsAsync(userId, 1, 1000, cancellationToken)).Items;
        var topUpRequests = await _topUpService.GetUserRequestsAsync(userId, cancellationToken);

        var vm = new DashboardViewModel
        {
            CurrentBalance = await _walletService.GetCurrentBalanceAsync(userId, cancellationToken),
            PendingTopUpCount = topUpRequests.Count(x =>
                x.Status == WebPhotocopyHub.Domain.Enums.TopUpStatus.Pending ||
                x.Status == WebPhotocopyHub.Domain.Enums.TopUpStatus.PendingAdminApproval),
            PrintJobsCount = printJobs.Count,
            ProductOrdersCount = productOrders.Count,
            SupportOrdersCount = supportOrders.Count,
            RecentTransactions = transactions.Take(5).ToList()
        };

        return View(vm);
    }
}