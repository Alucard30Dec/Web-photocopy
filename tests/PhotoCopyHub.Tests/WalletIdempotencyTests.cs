using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Services;

namespace PhotoCopyHub.Tests;

public class WalletIdempotencyTests
{
    [Fact]
    public async Task Debit_WithSameIdempotencyKey_ShouldNotCreateDuplicateTransaction()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "wallet-idem@test.local",
            Email = "wallet-idem@test.local",
            FullName = "Wallet Idempotency",
            CurrentBalance = 100000
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new WalletService(db);
        var request = new WalletOperationRequestDto
        {
            UserId = user.Id,
            Amount = 10000,
            TransactionType = WalletTransactionType.DebitForOrder,
            ReferenceType = "TestOrder",
            ReferenceId = Guid.NewGuid(),
            IdempotencyKey = "debit-test-001",
            Note = "Idempotency test"
        };

        var first = await service.DebitAsync(request);
        var second = await service.DebitAsync(request);

        var balance = await service.GetCurrentBalanceAsync(user.Id);
        var txCount = db.WalletTransactions.Count(x => x.UserId == user.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(90000, balance);
        Assert.Equal(1, txCount);
    }
}
