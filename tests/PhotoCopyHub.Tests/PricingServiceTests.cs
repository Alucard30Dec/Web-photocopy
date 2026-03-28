using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Services;

namespace PhotoCopyHub.Tests;

public class PricingServiceTests
{
    [Fact]
    public async Task CalculatePrintPriceAsync_ShouldReturnExpectedTotal()
    {
        var setup = TestDbFactory.CreateContext();
        await using var connection = setup.Connection;
        await using var db = setup.Context;

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

        var service = new PricingService(db);

        var result = await service.CalculatePrintPriceAsync(new PricingCalculationRequestDto
        {
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.BlackWhite,
            IsPhoto = false,
            Copies = 2,
            TotalPages = 5,
            DeliveryMethod = DeliveryMethod.PickupAtStore,
            ShippingFee = 15000
        });

        Assert.Equal(1000, result.UnitPrice);
        Assert.Equal(10000, result.SubTotal);
        Assert.Equal(0, result.ShippingFee);
        Assert.Equal(10000, result.TotalAmount);
    }
}
