using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Web.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext dbContext,
        ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Không kết nối được database.");
            }

            var data = new Dictionary<string, object>
            {
                ["provider"] = _dbContext.Database.ProviderName ?? "unknown"
            };

            return HealthCheckResult.Healthy("Database connection is healthy.", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed.");
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}