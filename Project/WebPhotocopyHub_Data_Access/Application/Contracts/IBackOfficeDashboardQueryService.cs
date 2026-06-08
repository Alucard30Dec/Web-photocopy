using PhotoCopyHub.Application.DTOs;

namespace PhotoCopyHub.Application.Contracts;

public interface IBackOfficeDashboardQueryService
{
    Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<ShopDashboardSummaryDto> GetShopSummaryAsync(CancellationToken cancellationToken = default);
}
