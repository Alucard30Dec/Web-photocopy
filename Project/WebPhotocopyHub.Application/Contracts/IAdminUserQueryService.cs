using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IAdminUserQueryService
{
    Task<List<ApplicationUser>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetPrimaryRoleMapAsync(IEnumerable<ApplicationUser> users, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken = default);
}
