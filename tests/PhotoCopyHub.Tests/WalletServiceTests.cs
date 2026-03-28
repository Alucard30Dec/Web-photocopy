using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Services;

namespace PhotoCopyHub.Tests;

public class WalletServiceTests
{
    [Fact]
    public async Task CreditAndDebit_ShouldUpdateBalanceCorrectly()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "u1@test.local",
            Email = "u1@test.local",
            FullName = "User 1",
            CurrentBalance = 50000
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new WalletService(db);

        await service.CreditAsync(new WalletOperationRequestDto
        {
            UserId = user.Id,
            Amount = 10000,
            TransactionType = WalletTransactionType.ManualAdjustment,
            Note = "Credit test"
        });

        await service.DebitAsync(new WalletOperationRequestDto
        {
            UserId = user.Id,
            Amount = 20000,
            TransactionType = WalletTransactionType.DebitForOrder,
            Note = "Debit test"
        });

        var balance = await service.GetCurrentBalanceAsync(user.Id);
        Assert.Equal(40000, balance);
    }

    [Fact]
    public async Task Debit_WhenInsufficientBalance_ShouldThrowBusinessException()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "u2@test.local",
            Email = "u2@test.local",
            FullName = "User 2",
            CurrentBalance = 1000
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new WalletService(db);

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await service.DebitAsync(new WalletOperationRequestDto
            {
                UserId = user.Id,
                Amount = 2000,
                TransactionType = WalletTransactionType.DebitForOrder,
                Note = "Should fail"
            }));
    }
}
