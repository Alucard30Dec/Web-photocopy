using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;
using PhotoCopyHub.Infrastructure.Options;

namespace PhotoCopyHub.Infrastructure.Services;

public class TopUpService : ITopUpService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWalletService _walletService;
    private readonly BusinessOptions _businessOptions;

    public TopUpService(
        ApplicationDbContext dbContext,
        IWalletService walletService,
        IOptions<BusinessOptions> businessOptions)
    {
        _dbContext = dbContext;
        _walletService = walletService;
        _businessOptions = businessOptions.Value;
    }

    public async Task<TopUpRequest> CreateRequestAsync(CreateTopUpRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("Số tiền nạp phải lớn hơn 0.");
        }

        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey) ?? Guid.NewGuid().ToString("N");

        var userExists = await _dbContext.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new BusinessException("Tài khoản không tồn tại.");
        }

        var existingRequest = await _dbContext.TopUpRequests
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                x.Channel == TopUpChannel.BankTransfer &&
                x.CreateIdempotencyKey == idempotencyKey, cancellationToken);
        if (existingRequest is not null)
        {
            if (existingRequest.Amount != request.Amount
                || !string.Equals(existingRequest.TransferContent, request.TransferContent, StringComparison.Ordinal)
                || !string.Equals(existingRequest.TransactionReferenceCode, request.TransactionReferenceCode, StringComparison.Ordinal))
            {
                throw new BusinessException("Idempotency key đã được dùng cho payload khác.");
            }

            return existingRequest;
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var topUp = new TopUpRequest
        {
            UserId = request.UserId,
            Amount = request.Amount,
            TransferContent = request.TransferContent,
            TransactionReferenceCode = request.TransactionReferenceCode,
            ProofFileId = request.ProofFileId,
            CreateIdempotencyKey = idempotencyKey,
            Status = TopUpStatus.Pending,
            Channel = TopUpChannel.BankTransfer
        };

        _dbContext.TopUpRequests.Add(topUp);
        var currentBalance = await _walletService.GetCurrentBalanceAsync(request.UserId, cancellationToken);

        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            UserId = request.UserId,
            TransactionType = WalletTransactionType.TopUpPending,
            Amount = 0,
            BalanceBefore = currentBalance,
            BalanceAfter = currentBalance,
            ReferenceType = nameof(TopUpRequest),
            ReferenceId = topUp.Id,
            Note = $"Yêu cầu nạp tiền chờ duyệt: {request.Amount:N0}",
            IdempotencyKey = idempotencyKey
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return topUp;
    }

    public async Task<TopUpRequest> CreateCounterTopUpAsync(CreateCounterTopUpDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("Số tiền nạp phải lớn hơn 0.");
        }

        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey) ?? Guid.NewGuid().ToString("N");

        var userExists = await _dbContext.Users.AnyAsync(x => x.Id == request.TargetUserId, cancellationToken);
        if (!userExists)
        {
            throw new BusinessException("Tài khoản khách hàng không tồn tại.");
        }

        var existingRequest = await _dbContext.TopUpRequests
            .FirstOrDefaultAsync(x =>
                x.UserId == request.TargetUserId &&
                x.Channel == TopUpChannel.CounterCash &&
                x.CreateIdempotencyKey == idempotencyKey, cancellationToken);
        if (existingRequest is not null)
        {
            if (existingRequest.Amount != request.Amount)
            {
                throw new BusinessException("Idempotency key đã được dùng cho payload khác.");
            }

            return existingRequest;
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var topUp = new TopUpRequest
        {
            UserId = request.TargetUserId,
            Amount = request.Amount,
            TransferContent = "Nạp tại quầy",
            TransactionReferenceCode = $"COUNTER-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CreateIdempotencyKey = idempotencyKey,
            Channel = TopUpChannel.CounterCash,
            Status = TopUpStatus.Approved,
            ReviewedByAdminId = request.OperatorUserId,
            ReviewedAt = DateTime.UtcNow,
            ReviewNote = request.Note
        };

        _dbContext.TopUpRequests.Add(topUp);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var walletTxn = await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = request.TargetUserId,
            Amount = request.Amount,
            TransactionType = WalletTransactionType.TopUpApproved,
            ReferenceType = nameof(TopUpRequest),
            ReferenceId = topUp.Id,
            Note = request.Note ?? "Nạp tiền trực tiếp tại quầy",
            IdempotencyKey = idempotencyKey,
            PerformedByAdminId = request.OperatorUserId
        }, cancellationToken);

        topUp.ApprovedWalletTransactionId = walletTxn.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return topUp;
    }

    public Task<List<TopUpRequest>> GetUserRequestsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TopUpRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TopUpRequest>> GetAllRequestsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.TopUpRequests
            .AsNoTracking()
            .Include(x => x.User)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TopUpRequest> ReviewRequestAsync(ReviewTopUpRequestDto reviewDto, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = NormalizeIdempotencyKey(reviewDto.IdempotencyKey) ?? Guid.NewGuid().ToString("N");
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var request = await _dbContext.TopUpRequests
            .FirstOrDefaultAsync(x => x.Id == reviewDto.TopUpRequestId, cancellationToken);

        if (request is null)
        {
            throw new BusinessException("Không tìm thấy yêu cầu nạp tiền.");
        }

        if (string.Equals(request.LastReviewIdempotencyKey, idempotencyKey, StringComparison.Ordinal))
        {
            return request;
        }

        if (request.Status == TopUpStatus.PendingAdminApproval)
        {
            if (!reviewDto.IsAdminReviewer)
            {
                throw new BusinessException("Yêu cầu này đang chờ admin duyệt bước 2.");
            }

            if (string.Equals(request.ReviewedByAdminId, reviewDto.ReviewerUserId, StringComparison.Ordinal))
            {
                throw new BusinessException("Người duyệt bước 2 phải khác người đã duyệt bước 1.");
            }

            request.SecondReviewedByAdminId = reviewDto.ReviewerUserId;
            request.SecondReviewedAt = DateTime.UtcNow;
            request.SecondReviewNote = reviewDto.Note;
            request.LastReviewIdempotencyKey = idempotencyKey;

            if (reviewDto.IsApprove)
            {
                request.Status = TopUpStatus.Approved;

                var walletTxn = await _walletService.CreditAsync(new WalletOperationRequestDto
                {
                    UserId = request.UserId,
                    Amount = request.Amount,
                    TransactionType = WalletTransactionType.TopUpApproved,
                    ReferenceType = nameof(TopUpRequest),
                    ReferenceId = request.Id,
                    Note = reviewDto.Note ?? "Admin duyệt nạp tiền bước 2",
                    IdempotencyKey = idempotencyKey,
                    PerformedByAdminId = reviewDto.ReviewerUserId
                }, cancellationToken);

                request.ApprovedWalletTransactionId = walletTxn.Id;
            }
            else
            {
                request.Status = TopUpStatus.Rejected;
                await AddRejectedRecordAsync(request, reviewDto.Note, reviewDto.ReviewerUserId, idempotencyKey, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return request;
        }

        if (request.Status != TopUpStatus.Pending)
        {
            throw new BusinessException("Yêu cầu này đã được xử lý trước đó.");
        }

        request.ReviewedByAdminId = reviewDto.ReviewerUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNote = reviewDto.Note;
        request.LastReviewIdempotencyKey = idempotencyKey;

        if (!reviewDto.IsApprove)
        {
            request.Status = TopUpStatus.Rejected;
            await AddRejectedRecordAsync(request, reviewDto.Note, reviewDto.ReviewerUserId, idempotencyKey, cancellationToken);
        }
        else
        {
            var requiresAdminStep = _businessOptions.TopUpRequireAdminApprovalThreshold > 0
                                    && request.Amount >= _businessOptions.TopUpRequireAdminApprovalThreshold
                                    && !reviewDto.IsAdminReviewer;
            request.RequiresAdminApproval = requiresAdminStep;

            if (requiresAdminStep)
            {
                request.Status = TopUpStatus.PendingAdminApproval;
            }
            else
            {
                if (_businessOptions.TopUpRequireAdminApprovalThreshold > 0
                    && request.Amount >= _businessOptions.TopUpRequireAdminApprovalThreshold
                    && reviewDto.IsAdminReviewer)
                {
                    throw new BusinessException("Khoản nạp vượt ngưỡng cần ShopOperator duyệt bước 1 trước.");
                }

                request.Status = TopUpStatus.Approved;
                var walletTxn = await _walletService.CreditAsync(new WalletOperationRequestDto
                {
                    UserId = request.UserId,
                    Amount = request.Amount,
                    TransactionType = WalletTransactionType.TopUpApproved,
                    ReferenceType = nameof(TopUpRequest),
                    ReferenceId = request.Id,
                    Note = reviewDto.Note ?? "Duyệt yêu cầu nạp tiền",
                    IdempotencyKey = idempotencyKey,
                    PerformedByAdminId = reviewDto.ReviewerUserId
                }, cancellationToken);

                request.ApprovedWalletTransactionId = walletTxn.Id;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return request;
    }

    private async Task AddRejectedRecordAsync(
        TopUpRequest request,
        string? note,
        string reviewerId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var currentBalance = await _walletService.GetCurrentBalanceAsync(request.UserId, cancellationToken);
        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            UserId = request.UserId,
            TransactionType = WalletTransactionType.TopUpRejected,
            Amount = 0,
            BalanceBefore = currentBalance,
            BalanceAfter = currentBalance,
            ReferenceType = nameof(TopUpRequest),
            ReferenceId = request.Id,
            Note = note ?? "Từ chối yêu cầu nạp tiền",
            IdempotencyKey = idempotencyKey,
            PerformedByAdminId = reviewerId
        });
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
