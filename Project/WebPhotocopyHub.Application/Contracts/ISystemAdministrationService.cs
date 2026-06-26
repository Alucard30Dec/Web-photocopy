using System.Security.Claims;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface ISystemAdministrationService
{
    Task<IReadOnlyList<SystemFunction>> GetFunctionsAsync(CancellationToken cancellationToken = default);
    Task<SystemFunction?> GetFunctionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SystemFunction> SaveFunctionAsync(SystemFunction function, string actorUserId, CancellationToken cancellationToken = default);
    Task SetFunctionActiveAsync(Guid id, bool isActive, string actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemRoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<SystemRoleEditDto?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default);
    Task<string> SaveRoleAsync(SystemRoleEditDto role, string actorUserId, CancellationToken cancellationToken = default);
    Task SetRoleActiveAsync(string roleId, bool isActive, string actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemPermissionMatrixItemDto>> GetPermissionMatrixAsync(string roleId, CancellationToken cancellationToken = default);
    Task SavePermissionMatrixAsync(
        string roleId,
        IEnumerable<SystemPermissionUpdateDto> permissions,
        string actorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string area,
        string controller,
        string permissionAction,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessAdminPortalAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessAdminPortalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemNavigationItemDto>> GetNavigationAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Branch>> GetAccessibleAdminBranchesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<string?> GetFirstAccessibleAdminPathAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
