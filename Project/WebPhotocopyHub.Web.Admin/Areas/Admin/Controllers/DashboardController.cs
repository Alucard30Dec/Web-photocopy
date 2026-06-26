using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IBackOfficeDashboardQueryService _dashboardQueryService;
    private readonly IAuditLogService _auditLogService;
    private readonly HealthCheckService _healthCheckService;
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;

    public DashboardController(
        IBackOfficeDashboardQueryService dashboardQueryService,
        IAuditLogService auditLogService,
        HealthCheckService healthCheckService,
        IEnumerable<EndpointDataSource> endpointDataSources)
    {
        _dashboardQueryService = dashboardQueryService;
        _auditLogService = auditLogService;
        _healthCheckService = healthCheckService;
        _endpointDataSources = endpointDataSources;
    }

    [HttpGet("/Admin/Dashboard")]
    public IActionResult AdminDashboardCanonicalRedirect()
    {
        return LocalRedirect("/Admin");
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _dashboardQueryService.GetAdminSummaryAsync(cancellationToken);
        var recentActivities = await _auditLogService.GetRecentAsync(200, cancellationToken);
        var healthReport = await _healthCheckService.CheckHealthAsync(_ => true, cancellationToken);

        var model = new AdminDashboardPageViewModel
        {
            Summary = summary,
            HealthStatus = healthReport.Status.ToString(),
            ApiEndpointCount = CountApiEndpoints(),
            ActivityLast24Hours = recentActivities.Count(x => x.CreatedAt >= DateTime.UtcNow.AddHours(-24)),
            IsAuditChainValid = IsAuditChainValid(recentActivities),
            RecentActivities = recentActivities.Take(8).ToList()
        };

        return View(model);
    }

    private int CountApiEndpoints()
    {
        return _endpointDataSources
            .SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(x => (x.RoutePattern.RawText ?? string.Empty).TrimStart('/'))
            .Count(x => x.StartsWith("api/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAuditChainValid(IReadOnlyList<AuditLog> items)
    {
        if (items.Count <= 1)
        {
            return true;
        }

        for (var index = 0; index < items.Count - 1; index++)
        {
            var current = items[index];
            var previous = items[index + 1];

            if (!string.Equals(current.PreviousHash, previous.RecordHash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
