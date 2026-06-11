using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IPricingService
{
    Task<PricingCalculationResultDto> CalculatePrintPriceAsync(PricingCalculationRequestDto request, CancellationToken cancellationToken = default);
    Task<List<PricingRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
}
