using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Report;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class WalletTransactionsController : Controller
{
    private readonly IWalletService _walletService;
    private readonly IAdminCsvReportService _reportService;

    public WalletTransactionsController(
        IWalletService walletService,
        IAdminCsvReportService reportService)
    {
        _walletService = walletService;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _walletService.GetAllTransactionsAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var items = await _walletService.GetAllTransactionsAsync(cancellationToken);
        var bytes = _reportService.BuildWalletTransactionsCsv(items);
        return File(bytes, "text/csv; charset=utf-8", $"wallet-transactions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
