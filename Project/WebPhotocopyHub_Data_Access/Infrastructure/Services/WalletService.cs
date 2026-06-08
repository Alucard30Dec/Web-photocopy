using System.Data;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _dbContext;

    public WalletService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetCurrentBalanceAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new BusinessException("Tài khoản không tồn tại.");
        }

        return user.CurrentBalance;
    }

    public Task<List<WalletTransaction>> GetUserTransactionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<WalletTransaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.WalletTransactions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
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

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

            if (user is null)
            {
                throw new BusinessException("Tài khoản không tồn tại.");
            }

            var before = user.CurrentBalance;
            var after = before + signedAmount;

            if (ensureNonNegative && after < 0)
            {
                throw new BusinessException("Số dư ví không đủ để thực hiện giao dịch.");
            }

            user.CurrentBalance = after;

            var transaction = new WalletTransaction
            {
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

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
