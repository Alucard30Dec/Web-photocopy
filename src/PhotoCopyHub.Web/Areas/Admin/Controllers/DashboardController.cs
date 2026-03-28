using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.TotalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        ViewBag.PendingTopUps = await _dbContext.TopUpRequests.CountAsync(
            x => x.Status == TopUpStatus.Pending || x.Status == TopUpStatus.PendingAdminApproval,
            cancellationToken);
        ViewBag.PrintJobsPending = await _dbContext.PrintJobs.CountAsync(
            x => x.Status == PrintJobStatus.Submitted
                 || x.Status == PrintJobStatus.ConfirmedByShop
                 || x.Status == PrintJobStatus.Paid
                 || x.Status == PrintJobStatus.Processing,
            cancellationToken);
        ViewBag.TotalWalletTransactions = await _dbContext.WalletTransactions.CountAsync(cancellationToken);
        ViewBag.ActiveProducts = await _dbContext.Products.CountAsync(x => x.IsActive, cancellationToken);
        ViewBag.ActiveSupportServices = await _dbContext.SupportServices.CountAsync(x => x.IsActive, cancellationToken);

        return View();
    }
}
