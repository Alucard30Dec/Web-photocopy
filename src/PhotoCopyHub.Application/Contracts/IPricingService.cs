using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Application.Contracts;

public interface IPricingService
{
    Task<PricingCalculationResultDto> CalculatePrintPriceAsync(PricingCalculationRequestDto request, CancellationToken cancellationToken = default);
    Task<List<PricingRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
}
