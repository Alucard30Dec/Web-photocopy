using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Infrastructure.Services;

public class WalletReconciliationService : IWalletReconciliationService
{
    private readonly ApplicationDbContext _dbContext;

    public WalletReconciliationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletBalanceReconciliationResultDto> ReconcileAsync(bool includeMatched, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.CurrentBalance
            })
            .ToListAsync(cancellationToken);

        var ledger = await _dbContext.WalletTransactions
            .AsNoTracking()
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                UserId = x.Key,
                LedgerBalance = x.Sum(y => y.Amount)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.LedgerBalance, cancellationToken);

        var items = new List<WalletBalanceCheckItemDto>(users.Count);
        foreach (var user in users)
        {
            var ledgerBalance = ledger.TryGetValue(user.Id, out var value) ? value : 0;
            var difference = user.CurrentBalance - ledgerBalance;
            if (!includeMatched && difference == 0)
            {
                continue;
            }

            items.Add(new WalletBalanceCheckItemDto
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentBalance = user.CurrentBalance,
                LedgerBalance = ledgerBalance,
                Difference = difference
            });
        }

        var matchedUsers = users.Count - items.Count(x => x.Difference != 0);
        var mismatchUsers = users.Count - matchedUsers;

        return new WalletBalanceReconciliationResultDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalUsers = users.Count,
            MatchedUsers = matchedUsers,
            MismatchUsers = mismatchUsers,
            Items = items
                .OrderByDescending(x => Math.Abs(x.Difference))
                .ThenBy(x => x.Email)
                .ToList()
        };
    }
}
