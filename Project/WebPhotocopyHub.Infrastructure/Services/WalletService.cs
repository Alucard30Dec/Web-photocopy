using System.Data;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBranchContext _branchContext;

    public WalletService(ApplicationDbContext dbContext, IBranchContext branchContext)
    {
        _dbContext = dbContext;
        _branchContext = branchContext;
    }

    public async Task<decimal> GetCurrentBalanceAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var branchId = GetCurrentBranchId();

        return await _dbContext.WalletAccounts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BranchId == branchId)
            .Select(x => (decimal?)x.Balance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
    }

    public async Task<PagedResult<WalletTransaction>> GetUserTransactionsAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var branchId = GetCurrentBranchId();
        var query = _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && x.UserId == userId);
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<WalletTransaction>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<WalletTransaction>> GetAllTransactionsAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var branchId = GetCurrentBranchId();
        var query = _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(x => x.BranchId == branchId);
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<WalletTransaction>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Task<WalletTransaction> CreditAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("Số tiền cộng ví phải lớn hơn 0.");
        }

        return ApplyTransactionAsync(request, request.Amount, ensureNonNegative: false, cancellationToken);
    }

    public Task<WalletTransaction> DebitAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("Số tiền trừ ví phải lớn hơn 0.");
        }

        return ApplyTransactionAsync(request, -request.Amount, ensureNonNegative: true, cancellationToken);
    }

    public Task<WalletTransaction> ManualAdjustAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount == 0)
        {
            throw new BusinessException("Số tiền điều chỉnh phải khác 0.");
        }

        if (string.IsNullOrWhiteSpace(request.PerformedByAdminId))
        {
            throw new BusinessException("Thiếu thông tin người thực hiện điều chỉnh số dư.");
        }

        if (string.IsNullOrWhiteSpace(request.Note))
        {
            throw new BusinessException("Vui lòng nhập lý do điều chỉnh số dư.");
        }

        return ApplyTransactionAsync(request, request.Amount, ensureNonNegative: true, cancellationToken);
    }

    private async Task<WalletTransaction> ApplyTransactionAsync(
        WalletOperationRequestDto request,
        decimal signedAmount,
        bool ensureNonNegative,
        CancellationToken cancellationToken)
    {
        var branchId = GetCurrentBranchId();
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        var hasOuterTransaction = _dbContext.Database.CurrentTransaction is not null;
        await using var tx = hasOuterTransaction
            ? null
            : await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(normalizedIdempotencyKey))
            {
                var existing = await _dbContext.WalletTransactions
                    .FirstOrDefaultAsync(x =>
                        x.BranchId == branchId &&
                        x.UserId == request.UserId &&
                        x.TransactionType == request.TransactionType &&
                        x.IdempotencyKey == normalizedIdempotencyKey, cancellationToken);

                if (existing is not null)
                {
                    if (!string.Equals(existing.ReferenceType, request.ReferenceType, StringComparison.Ordinal) ||
                        existing.ReferenceId != request.ReferenceId ||
                        existing.Amount != signedAmount)
                    {
                        throw new BusinessException("Idempotency key đã được sử dụng với payload khác.");
                    }

                    return existing;
                }
            }

            await EnsureUserExistsAsync(request.UserId, cancellationToken);

            var account = await _dbContext.WalletAccounts
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.UserId == request.UserId, cancellationToken);

            var accountCreated = false;
            if (account is null)
            {
                account = new WalletAccount
                {
                    BranchId = branchId,
                    UserId = request.UserId,
                    Balance = 0,
                    Version = 1
                };
                _dbContext.WalletAccounts.Add(account);
                accountCreated = true;
            }

            var before = account.Balance;
            var after = before + signedAmount;

            if (ensureNonNegative && after < 0)
            {
                throw new BusinessException("Số dư ví của chi nhánh không đủ để thực hiện giao dịch.");
            }

            account.Balance = after;
            if (!accountCreated)
            {
                account.Version += 1;
            }

            var transaction = new WalletTransaction
            {
                BranchId = branchId,
                WalletAccountId = account.Id,
                UserId = request.UserId,
                TransactionType = request.TransactionType,
                Amount = signedAmount,
                BalanceBefore = before,
                BalanceAfter = after,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                Note = request.Note,
                IdempotencyKey = normalizedIdempotencyKey,
                PerformedByAdminId = request.PerformedByAdminId
            };

            _dbContext.WalletTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return transaction;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            throw new BusinessException("Giao dịch ví thất bại do xung đột dữ liệu. Vui lòng thử lại.");
        }
        catch
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private async Task EnsureUserExistsAsync(string userId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId, cancellationToken))
        {
            throw new BusinessException("Tài khoản không tồn tại.");
        }
    }

    private Guid GetCurrentBranchId()
    {
        return _branchContext.BranchId ?? BranchDefaults.PrimaryBranchId;
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
