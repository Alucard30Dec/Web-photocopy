using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;

namespace PhotoCopyHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = AppPolicies.CustomerPortal)]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public class MeApiController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IPrintJobService _printJobService;

    public MeApiController(IWalletService walletService, IPrintJobService printJobService)
    {
        _walletService = walletService;
        _printJobService = printJobService;
    }

    [HttpGet("wallet")]
    [ProducesResponseType(typeof(WalletSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WalletSummaryResponse>> GetWallet(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Bạn cần đăng nhập để dùng API này." });
        }

        var balance = await _walletService.GetCurrentBalanceAsync(userId, cancellationToken);
        return Ok(new WalletSummaryResponse(balance));
    }

    [HttpGet("wallet/transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<WalletTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<WalletTransactionResponse>>> GetWalletTransactions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Bạn cần đăng nhập để dùng API này." });
        }

        var transactions = await _walletService.GetUserTransactionsAsync(userId, cancellationToken);

        var response = transactions
            .Select(transaction => new WalletTransactionResponse(
                transaction.Id,
                transaction.TransactionType.ToString(),
                transaction.Amount,
                transaction.BalanceBefore,
                transaction.BalanceAfter,
                transaction.ReferenceType,
                transaction.ReferenceId,
                transaction.Note,
                transaction.CreatedAt))
            .ToList();

        return Ok(response);
    }

    [HttpGet("print-jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<PrintJobResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PrintJobResponse>>> GetPrintJobs(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Bạn cần đăng nhập để dùng API này." });
        }

        var printJobs = await _printJobService.GetUserOrdersAsync(userId, cancellationToken);

        var response = printJobs
            .Select(job => new PrintJobResponse(
                job.Id,
                job.UploadedFileId,
                job.PaperSize.ToString(),
                job.PrintSide.ToString(),
                job.ColorMode.ToString(),
                job.IsPhoto,
                job.Copies,
                job.TotalPages,
                job.DeliveryMethod.ToString(),
                job.TotalAmount,
                job.Status.ToString(),
                job.CreatedAt,
                job.UpdatedAt))
            .ToList();

        return Ok(response);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

public sealed record WalletSummaryResponse(decimal Balance);

public sealed record WalletTransactionResponse(
    Guid Id,
    string TransactionType,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note,
    DateTime CreatedAt);

public sealed record PrintJobResponse(
    Guid Id,
    Guid UploadedFileId,
    string PaperSize,
    string PrintSide,
    string ColorMode,
    bool IsPhoto,
    int Copies,
    int TotalPages,
    string DeliveryMethod,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
