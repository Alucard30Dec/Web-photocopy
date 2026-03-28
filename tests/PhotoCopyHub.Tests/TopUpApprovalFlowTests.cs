using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Services;
using PhotoCopyHub.Infrastructure.Options;

namespace PhotoCopyHub.Tests;

public class TopUpApprovalFlowTests
{
    [Fact]
    public async Task ApproveTopUp_ShouldIncreaseBalanceAndCreateTransactions()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "customer@test.local",
            Email = "customer@test.local",
            FullName = "Customer",
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
                TopUpRequireAdminApprovalThreshold = 999999999
            }));

        var request = await topUpService.CreateRequestAsync(new CreateTopUpRequestDto
        {
            UserId = user.Id,
            Amount = 30000,
            TransferContent = "TOPUP-CUSTOMER",
            TransactionReferenceCode = "TX123"
        });

        await topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
        {
            TopUpRequestId = request.Id,
            ReviewerUserId = "operator-id",
            IsAdminReviewer = false,
            IsApprove = true,
            Note = "Approved"
        });

        var updatedUser = await db.Users.FirstAsync(x => x.Id == user.Id);
        var updatedRequest = await db.TopUpRequests.FirstAsync(x => x.Id == request.Id);
        var transactions = await db.WalletTransactions.Where(x => x.UserId == user.Id).ToListAsync();

        Assert.Equal(30000, updatedUser.CurrentBalance);
        Assert.Equal(TopUpStatus.Approved, updatedRequest.Status);
        Assert.Equal(2, transactions.Count);
        Assert.Contains(transactions, x => x.TransactionType == WalletTransactionType.TopUpPending);
        Assert.Contains(transactions, x => x.TransactionType == WalletTransactionType.TopUpApproved);
    }
}
