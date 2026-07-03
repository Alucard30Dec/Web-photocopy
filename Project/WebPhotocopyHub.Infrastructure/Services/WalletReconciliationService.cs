using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Infrastructure.Services;

public class WalletReconciliationService : IWalletReconciliationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBranchContext _branchContext;

    public WalletReconciliationService(ApplicationDbContext dbContext, IBranchContext branchContext)
    {
        _dbContext = dbContext;
        _branchContext = branchContext;
    }

    public async Task<WalletBalanceReconciliationResultDto> ReconcileAsync(bool includeMatched, CancellationToken cancellationToken = default)
    {
        var branchId = _branchContext.BranchId ?? BranchDefaults.PrimaryBranchId;

        var accounts = await _dbContext.WalletAccounts
            .AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .Join(
                _dbContext.Users.AsNoTracking(),
                account => account.UserId,
                user => user.Id,
                (account, user) => new
                {
                    account.UserId,
                    user.Email,
                    CurrentBalance = account.Balance
                })
            .ToListAsync(cancellationToken);

        var ledger = await _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                UserId = x.Key,
                LedgerBalance = x.Sum(y => y.Amount)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.LedgerBalance, cancellationToken);

        var items = new List<WalletBalanceCheckItemDto>(accounts.Count);
        foreach (var account in accounts)
        {
            var ledgerBalance = ledger.TryGetValue(account.UserId, out var value) ? value : 0;
            var difference = account.CurrentBalance - ledgerBalance;
            if (!includeMatched && difference == 0)
            {
                continue;
            }

            items.Add(new WalletBalanceCheckItemDto
            {
                UserId = account.UserId,
                Email = account.Email,
                CurrentBalance = account.CurrentBalance,
                LedgerBalance = ledgerBalance,
                Difference = difference
            });
        }

        var mismatchUsers = items.Count(x => x.Difference != 0);
        return new WalletBalanceReconciliationResultDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalUsers = accounts.Count,
            MatchedUsers = accounts.Count - mismatchUsers,
            MismatchUsers = mismatchUsers,
            Items = items
                .OrderByDescending(x => Math.Abs(x.Difference))
                .ThenBy(x => x.Email)
                .ToList()
        };
    }
}
