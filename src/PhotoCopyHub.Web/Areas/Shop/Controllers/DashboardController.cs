using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;
using PhotoCopyHub.Web;

namespace PhotoCopyHub.Web.Areas.Shop.Controllers;

[Area("Shop")]
[Authorize(Policy = AppPolicies.ShopOperation)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.PendingTopUp = await _dbContext.TopUpRequests.CountAsync(x => x.Status == TopUpStatus.Pending, cancellationToken);
        ViewBag.PendingAdminTopUp = await _dbContext.TopUpRequests.CountAsync(x => x.Status == TopUpStatus.PendingAdminApproval, cancellationToken);
        ViewBag.PrintQueue = await _dbContext.PrintJobs.CountAsync(
            x => x.Status == PrintJobStatus.Submitted
                 || x.Status == PrintJobStatus.ConfirmedByShop
                 || x.Status == PrintJobStatus.Paid
                 || x.Status == PrintJobStatus.Processing,
            cancellationToken);
        ViewBag.ProductOrdersWaiting = await _dbContext.ProductOrders.CountAsync(
            x => x.Status == OrderStatus.Submitted || x.Status == OrderStatus.Processing,
            cancellationToken);
        ViewBag.SupportOrdersWaiting = await _dbContext.SupportServiceOrders.CountAsync(
            x => x.Status == OrderStatus.Submitted || x.Status == OrderStatus.Processing,
            cancellationToken);
        ViewBag.LowStockProducts = await _dbContext.Products.CountAsync(x => x.IsActive && x.StockQuantity <= 10, cancellationToken);

        var latestAlerts = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.StockQuantity <= 10)
            .OrderBy(x => x.StockQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);

        return View(latestAlerts);
    }
}
