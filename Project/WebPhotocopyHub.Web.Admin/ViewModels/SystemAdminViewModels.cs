using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Web.Admin.Models;

public sealed class AdminDashboardPageViewModel
{
    public AdminDashboardSummaryDto Summary { get; set; } = new();
    public string HealthStatus { get; set; } = "Unknown";
    public int ApiEndpointCount { get; set; }
    public int ActivityLast24Hours { get; set; }
    public bool IsAuditChainValid { get; set; }
    public List<AuditLog> RecentActivities { get; set; } = new();
}

public sealed class SystemMonitoringViewModel
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public TimeSpan Uptime { get; set; }
    public double WorkingSetMb { get; set; }
    public double ManagedMemoryMb { get; set; }
    public double GcHeapMb { get; set; }
    public int ThreadCount { get; set; }
    public int AvailableWorkerThreads { get; set; }
    public int MaxWorkerThreads { get; set; }
    public int ProcessorCount { get; set; }
    public string TotalProcessorTime { get; set; } = string.Empty;
    public string OverallHealthStatus { get; set; } = "Unknown";
    public bool SwaggerEnabled { get; set; }
    public bool IsAuditChainValid { get; set; }
    public int ActivityLast24Hours { get; set; }
    public List<HealthCheckMonitorItemViewModel> HealthChecks { get; set; } = new();
    public List<ApiEndpointMonitorViewModel> ApiEndpoints { get; set; } = new();
    public List<AuditLog> RecentActivities { get; set; } = new();
}

public sealed class HealthCheckMonitorItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double DurationMilliseconds { get; set; }
    public string? Description { get; set; }
}

public sealed class ApiEndpointMonitorViewModel
{
    public string Route { get; set; } = string.Empty;
    public string Methods { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string AccessMode { get; set; } = string.Empty;
    public string AuthorizationDetails { get; set; } = string.Empty;
    public string RateLimitMode { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
}

public sealed class AuditLogIndexViewModel
{
    public List<AuditLog> Items { get; set; } = new();
    public string? Query { get; set; }
    public string? ActionName { get; set; }
    public string? EntityName { get; set; }
    public string? ActorUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string> AvailableActions { get; set; } = new();
    public List<string> AvailableEntities { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages { get; set; } = 1;
}

public sealed class UpdateSystemOrderStatusViewModel
{
    public Guid OrderId { get; set; }

    [Required]
    [Display(Name = "Trạng thái")]
    public OrderStatus Status { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú xử lý")]
    public string? Note { get; set; }
}

public sealed class AdjustAdminStockViewModel
{
    public Guid ProductId { get; set; }

    [Range(-1000000, 1000000)]
    [Display(Name = "Số lượng thay đổi")]
    public int QuantityDelta { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Lý do điều chỉnh")]
    public string Note { get; set; } = string.Empty;
}

public sealed class AdminInventoryViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<ProductStockMovement> RecentMovements { get; set; } = new();
}
