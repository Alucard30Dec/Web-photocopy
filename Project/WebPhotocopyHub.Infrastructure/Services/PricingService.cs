using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Infrastructure.Services;

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _dbContext;

    public PricingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PricingCalculationResultDto> CalculatePrintPriceAsync(PricingCalculationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Copies <= 0 || request.TotalPages <= 0)
        {
            throw new BusinessException("Số bản in và số trang phải lớn hơn 0.");
        }

        var rule = await _dbContext.PricingRules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IsActive
                     && x.PaperSize == request.PaperSize
                     && x.PrintSide == request.PrintSide
                     && x.ColorMode == request.ColorMode
                     && x.IsPhoto == request.IsPhoto,
                cancellationToken);

        if (rule is null)
        {
            throw new BusinessException("Không tìm thấy bảng giá phù hợp cho cấu hình in đã chọn.");
        }

        var pagesToCharge = request.PrintSide == PrintSide.TwoSide
            ? (int)Math.Ceiling(request.TotalPages / 2m)
            : request.TotalPages;

        var subTotal = rule.UnitPrice * pagesToCharge * request.Copies;
        var shippingFee = request.DeliveryMethod == DeliveryMethod.Shipping ? request.ShippingFee : 0;

        return new PricingCalculationResultDto
        {
            UnitPrice = rule.UnitPrice,
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            TotalAmount = subTotal + shippingFee
        };
    }

    public Task<List<PricingRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PricingRules
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PaperSize)
            .ThenBy(x => x.ColorMode)
            .ThenBy(x => x.PrintSide)
            .ToListAsync(cancellationToken);
    }
}
