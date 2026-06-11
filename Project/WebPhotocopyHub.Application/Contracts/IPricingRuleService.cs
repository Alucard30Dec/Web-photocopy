using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IPricingRuleService
{
    Task<List<PricingRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PricingRule> UpsertAsync(PricingRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
