using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Application.Contracts;

public interface IPricingRuleService
{
    Task<List<PricingRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PricingRule> UpsertAsync(PricingRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
