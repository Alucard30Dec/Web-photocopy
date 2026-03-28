using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Options;
using PhotoCopyHub.Infrastructure.Services;

namespace PhotoCopyHub.Tests;

public class TopUpMultiStepApprovalTests
{
    [Fact]
    public async Task HighAmountTopUp_ShouldRequireAdminSecondStep_AndBeIdempotentOnRetry()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "topup-multistep@test.local",
            Email = "topup-multistep@test.local",
            FullName = "TopUp MultiStep",
            CurrentBalance = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var walletService = new WalletService(db);
        var topUpService = new TopUpService(
            db,
            walletService,
            Options.Create(new BusinessOptions
            {
                TopUpRequireAdminApprovalThreshold = 10000
            }));

        var request = await topUpService.CreateRequestAsync(new CreateTopUpRequestDto
        {
            UserId = user.Id,
            Amount = 20000,
            TransferContent = "TOPUP",
            TransactionReferenceCode = "TX-MULTI-001",
            IdempotencyKey = "topup-create-001"
        });

        var step1 = await topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
        {
            TopUpRequestId = request.Id,
            ReviewerUserId = "operator-001",
            IsAdminReviewer = false,
            IsApprove = true,
            IdempotencyKey = "topup-review-step1-001",
            Note = "Shop verified"
        });

        Assert.Equal(TopUpStatus.PendingAdminApproval, step1.Status);
        Assert.Equal(0, await walletService.GetCurrentBalanceAsync(user.Id));

        var step2 = await topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
        {
            TopUpRequestId = request.Id,
            ReviewerUserId = "admin-001",
            IsAdminReviewer = true,
            IsApprove = true,
            IdempotencyKey = "topup-review-step2-001",
            Note = "Admin approved"
        });

        var step2Retry = await topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
        {
            TopUpRequestId = request.Id,
            ReviewerUserId = "admin-001",
            IsAdminReviewer = true,
            IsApprove = true,
            IdempotencyKey = "topup-review-step2-001",
            Note = "Retry same key"
        });

        var transactions = await db.WalletTransactions
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(TopUpStatus.Approved, step2.Status);
        Assert.Equal(step2.Id, step2Retry.Id);
        Assert.Equal(20000, await walletService.GetCurrentBalanceAsync(user.Id));
        Assert.Equal(2, transactions.Count);
        Assert.Equal(1, transactions.Count(x => x.TransactionType == WalletTransactionType.TopUpApproved));
    }

    [Fact]
    public async Task HighAmountTopUp_SecondReviewerMustBeDifferentFromFirstReviewer()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "topup-4eyes@test.local",
            Email = "topup-4eyes@test.local",
            FullName = "TopUp 4 Eyes",
            CurrentBalance = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var walletService = new WalletService(db);
        var topUpService = new TopUpService(
            db,
            walletService,
            Options.Create(new BusinessOptions
            {
                TopUpRequireAdminApprovalThreshold = 10000
            }));

        var request = await topUpService.CreateRequestAsync(new CreateTopUpRequestDto
        {
            UserId = user.Id,
            Amount = 30000,
            TransferContent = "TOPUP",
            TransactionReferenceCode = "TX-4EYES-001",
            IdempotencyKey = "topup-create-4eyes"
        });

        await topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
        {
            TopUpRequestId = request.Id,
            ReviewerUserId = "same-reviewer",
            IsAdminReviewer = false,
            IsApprove = true,
            IdempotencyKey = "topup-review-step1-4eyes",
            Note = "Step 1"
        });

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
            {
                TopUpRequestId = request.Id,
                ReviewerUserId = "same-reviewer",
                IsAdminReviewer = true,
                IsApprove = true,
                IdempotencyKey = "topup-review-step2-4eyes",
                Note = "Step 2 same actor"
            }));
    }
}
