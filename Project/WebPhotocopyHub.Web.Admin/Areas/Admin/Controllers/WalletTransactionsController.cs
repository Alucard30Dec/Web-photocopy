using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Report;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
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
        var items = (await _walletService.GetAllTransactionsAsync(1, 1000, cancellationToken)).Items;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var items = (await _walletService.GetAllTransactionsAsync(1, 10000, cancellationToken)).Items;
        var bytes = _reportService.BuildWalletTransactionsCsv(items);
        return File(bytes, "text/csv; charset=utf-8", $"wallet-transactions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
