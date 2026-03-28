using Microsoft.Extensions.Options;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Options;
using PhotoCopyHub.Infrastructure.Services;

namespace PhotoCopyHub.Tests;

public class PrintJobFlowTests
{
    [Fact]
    public async Task UpdateStatus_ShouldEnforceStateMachine()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "print-flow@test.local",
            Email = "print-flow@test.local",
            FullName = "Print Flow",
            CurrentBalance = 100000
        };

        var file = new UploadedFileMetadata
        {
            OwnerUserId = user.Id,
            OriginalFileName = "sample.pdf",
            StoredFileName = "sample.pdf",
            RelativePath = "2026/03/sample.pdf",
            ContentType = "application/pdf",
            Size = 1024,
            IsForPrintJob = true
        };

        db.Users.Add(user);
        db.UploadedFileMetadatas.Add(file);
        db.PricingRules.Add(new PricingRule
        {
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.BlackWhite,
            IsPhoto = false,
            UnitPrice = 1000,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var walletService = new WalletService(db);
        var pricingService = new PricingService(db);
        var printService = new PrintJobService(
            db,
            pricingService,
            walletService,
            Options.Create(new BusinessOptions
            {
                ShippingFee = 0,
                RefundRequireAdminApprovalThreshold = 1000000
            }));

        var job = await printService.CreateAndSubmitAsync(new CreatePrintJobDto
        {
            UserId = user.Id,
            UploadedFileId = file.Id,
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.BlackWhite,
            IsPhoto = false,
            Copies = 2,
            TotalPages = 3,
            DeliveryMethod = DeliveryMethod.PickupAtStore,
            IdempotencyKey = "print-submit-001"
        });

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await printService.UpdateStatusAsync(job.Id, PrintJobStatus.Paid, "operator-1", actorIsAdmin: false, note: null));

        await printService.UpdateStatusAsync(job.Id, PrintJobStatus.ConfirmedByShop, "operator-1", actorIsAdmin: false, note: "Confirmed");
        await printService.UpdateStatusAsync(job.Id, PrintJobStatus.Paid, "operator-1", actorIsAdmin: false, note: "Paid");

        var balanceAfterPaid = await walletService.GetCurrentBalanceAsync(user.Id);
        Assert.Equal(94000, balanceAfterPaid);
    }

    [Fact]
    public async Task Refund_HighAmount_ShouldRequireAdmin()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "print-refund@test.local",
            Email = "print-refund@test.local",
            FullName = "Print Refund",
            CurrentBalance = 200000
        };

        var file = new UploadedFileMetadata
        {
            OwnerUserId = user.Id,
            OriginalFileName = "sample2.pdf",
            StoredFileName = "sample2.pdf",
            RelativePath = "2026/03/sample2.pdf",
            ContentType = "application/pdf",
            Size = 1024,
            IsForPrintJob = true
        };

        db.Users.Add(user);
        db.UploadedFileMetadatas.Add(file);
        db.PricingRules.Add(new PricingRule
        {
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.Color,
            IsPhoto = false,
            UnitPrice = 5000,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var walletService = new WalletService(db);
        var pricingService = new PricingService(db);
        var printService = new PrintJobService(
            db,
            pricingService,
            walletService,
            Options.Create(new BusinessOptions
            {
                ShippingFee = 0,
                RefundRequireAdminApprovalThreshold = 1000
            }));

        var job = await printService.CreateAndSubmitAsync(new CreatePrintJobDto
        {
            UserId = user.Id,
            UploadedFileId = file.Id,
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.Color,
            IsPhoto = false,
            Copies = 1,
            TotalPages = 2,
            DeliveryMethod = DeliveryMethod.PickupAtStore,
            IdempotencyKey = "print-submit-refund-001"
        });

        await printService.UpdateStatusAsync(job.Id, PrintJobStatus.ConfirmedByShop, "operator-2", actorIsAdmin: false, note: "Confirmed");
        await printService.UpdateStatusAsync(job.Id, PrintJobStatus.Paid, "operator-2", actorIsAdmin: false, note: "Paid");

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await printService.RefundAsync(job.Id, "operator-2", actorIsAdmin: false, reason: "Need refund"));

        await printService.RefundAsync(job.Id, "admin-1", actorIsAdmin: true, reason: "Admin approved refund");

        var balanceAfterRefund = await walletService.GetCurrentBalanceAsync(user.Id);
        Assert.Equal(200000, balanceAfterRefund);
    }
}
