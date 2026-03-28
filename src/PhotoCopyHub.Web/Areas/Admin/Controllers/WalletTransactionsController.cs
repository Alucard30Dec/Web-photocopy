using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;

namespace PhotoCopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class WalletTransactionsController : Controller
{
    private readonly IWalletService _walletService;

    public WalletTransactionsController(IWalletService walletService)
    {
        _walletService = walletService;
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
        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAtUtc,UserId,TransactionType,Amount,BalanceBefore,BalanceAfter,ReferenceType,ReferenceId,IdempotencyKey,PerformedByAdminId,Note");
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.CreatedAt.ToString("O")),
                EscapeCsv(item.UserId),
                EscapeCsv(item.TransactionType.ToString()),
                EscapeCsv(item.Amount.ToString()),
                EscapeCsv(item.BalanceBefore.ToString()),
                EscapeCsv(item.BalanceAfter.ToString()),
                EscapeCsv(item.ReferenceType),
                EscapeCsv(item.ReferenceId?.ToString()),
                EscapeCsv(item.IdempotencyKey),
                EscapeCsv(item.PerformedByAdminId),
                EscapeCsv(item.Note)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"wallet-transactions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
