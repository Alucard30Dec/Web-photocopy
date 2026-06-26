using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IBranchManagementService
{
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Branch?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Branch> SaveAsync(Branch branch, string actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetEnabledFeaturesAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task SetFeaturesAsync(Guid branchId, IEnumerable<string> enabledFeatureCodes, string actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchRole>> GetRolesAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<BranchRole?> GetRoleAsync(Guid branchRoleId, CancellationToken cancellationToken = default);
    Task SetRolePermissionsAsync(Guid branchRoleId, IEnumerable<string> permissionCodes, string actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserBranchMembership>> GetMembershipsAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task AssignUserAsync(Guid branchId, string userId, Guid branchRoleId, bool isPrimary, string actorUserId, CancellationToken cancellationToken = default);
    Task RemoveUserAsync(Guid membershipId, string actorUserId, CancellationToken cancellationToken = default);
    Task<bool> IsFeatureEnabledAsync(Guid branchId, string featureCode, CancellationToken cancellationToken = default);
    Task<bool> UserHasPermissionAsync(string userId, Guid branchId, string permissionCode, CancellationToken cancellationToken = default);
    Task EnsureDefaultsForBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task SyncStaticCatalogAsync(CancellationToken cancellationToken = default);
}
