using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Admin)]
public sealed class SystemMonitoringController : Controller
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;
    private readonly IAuditLogService _auditLogService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;

    public SystemMonitoringController(
        HealthCheckService healthCheckService,
        IEnumerable<EndpointDataSource> endpointDataSources,
        IAuditLogService auditLogService,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _healthCheckService = healthCheckService;
        _endpointDataSources = endpointDataSources;
        _auditLogService = auditLogService;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var healthReport = await _healthCheckService.CheckHealthAsync(_ => true, cancellationToken);
        var recentActivities = await _auditLogService.GetRecentAsync(500, cancellationToken);

        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);

        using var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();

        var model = new SystemMonitoringViewModel
        {
            EnvironmentName = _hostEnvironment.EnvironmentName,
            RuntimeVersion = Environment.Version.ToString(),
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime(),
            WorkingSetMb = BytesToMegabytes(process.WorkingSet64),
            ManagedMemoryMb = BytesToMegabytes(GC.GetTotalMemory(false)),
            GcHeapMb = BytesToMegabytes(gcInfo.HeapSizeBytes),
            ThreadCount = process.Threads.Count,
            AvailableWorkerThreads = availableWorkerThreads,
            MaxWorkerThreads = maxWorkerThreads,
            ProcessorCount = Environment.ProcessorCount,
            TotalProcessorTime = process.TotalProcessorTime.ToString("g"),
            OverallHealthStatus = healthReport.Status.ToString(),
            SwaggerEnabled = _hostEnvironment.IsDevelopment() || _configuration.GetValue<bool>("Swagger:Enabled"),
            IsAuditChainValid = IsAuditChainValid(recentActivities),
            ActivityLast24Hours = recentActivities.Count(x => x.CreatedAt >= DateTime.UtcNow.AddHours(-24)),
            HealthChecks = healthReport.Entries
                .OrderBy(x => x.Key)
                .Select(x => new HealthCheckMonitorItemViewModel
                {
                    Name = x.Key,
                    Status = x.Value.Status.ToString(),
                    DurationMilliseconds = x.Value.Duration.TotalMilliseconds,
                    Description = x.Value.Description
                })
                .ToList(),
            ApiEndpoints = BuildEndpointInventory(),
            RecentActivities = recentActivities.Take(12).ToList()
        };

        return View(model);
    }

    private List<ApiEndpointMonitorViewModel> BuildEndpointInventory()
    {
        var items = new List<ApiEndpointMonitorViewModel>();

        foreach (var endpoint in _endpointDataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>())
        {
            var rawRoute = endpoint.RoutePattern.RawText ?? string.Empty;
            var normalizedRoute = rawRoute.TrimStart('/');

            if (!normalizedRoute.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
                && !normalizedRoute.StartsWith("healthz/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            var rateLimited = endpoint.Metadata.Any(x =>
                x.GetType().Name.Contains("RateLimit", StringComparison.OrdinalIgnoreCase));

            var policies = authorizeData
                .Where(x => !string.IsNullOrWhiteSpace(x.Policy))
                .Select(x => $"Policy: {x.Policy}")
                .ToList();

            policies.AddRange(authorizeData
                .Where(x => !string.IsNullOrWhiteSpace(x.Roles))
                .Select(x => $"Roles: {x.Roles}"));

            var accessMode = allowAnonymous
                ? "Công khai"
                : authorizeData.Count > 0
                    ? "Có authorization"
                    : "Đăng nhập mặc định";

            items.Add(new ApiEndpointMonitorViewModel
            {
                Route = "/" + normalizedRoute,
                Methods = httpMethods is null || httpMethods.Count == 0
                    ? "ANY"
                    : string.Join(", ", httpMethods),
                Controller = actionDescriptor?.ControllerName ?? "System endpoint",
                Action = actionDescriptor?.ActionName ?? endpoint.DisplayName ?? "-",
                AccessMode = accessMode,
                AuthorizationDetails = policies.Count == 0
                    ? allowAnonymous ? "AllowAnonymous" : "Global authenticated-user filter"
                    : string.Join(" · ", policies),
                RateLimitMode = rateLimited ? "Endpoint policy" : "Không có metadata riêng",
                IsAnonymous = allowAnonymous
            });
        }

        return items
            .OrderBy(x => x.Route)
            .ThenBy(x => x.Methods)
            .ToList();
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

    private static double BytesToMegabytes(long bytes)
    {
        return Math.Round(bytes / 1024d / 1024d, 2);
    }
}
