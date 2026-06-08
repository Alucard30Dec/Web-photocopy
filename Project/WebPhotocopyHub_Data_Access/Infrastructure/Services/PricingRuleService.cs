using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Infrastructure.Services;

public class PricingRuleService : IPricingRuleService
{
    private readonly ApplicationDbContext _dbContext;

    public PricingRuleService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<PricingRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PricingRules
            .AsNoTracking()
            .OrderBy(x => x.PaperSize)
            .ThenBy(x => x.PrintSide)
            .ThenBy(x => x.ColorMode)
            .ToListAsync(cancellationToken);
    }

    public Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PricingRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PricingRule> UpsertAsync(PricingRule rule, CancellationToken cancellationToken = default)
    {
        if (rule.Id == Guid.Empty || !await _dbContext.PricingRules.AnyAsync(x => x.Id == rule.Id, cancellationToken))
        {
            _dbContext.PricingRules.Add(rule);
        }
        else
        {
            _dbContext.PricingRules.Update(rule);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PricingRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        _dbContext.PricingRules.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
