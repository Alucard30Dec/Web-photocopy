using WebPhotocopyHub.Application.DTOs;

namespace WebPhotocopyHub.Application.Contracts;

public interface IBackOfficeDashboardQueryService
{
    Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<ShopDashboardSummaryDto> GetShopSummaryAsync(CancellationToken cancellationToken = default);
}
