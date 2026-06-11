using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.DataAccess.Controllers;

public sealed class AdminUserQueryService : IAdminUserQueryService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserQueryService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<List<ApplicationUser>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return _userManager.Users
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetPrimaryRoleMapAsync(
        IEnumerable<ApplicationUser> users,
        CancellationToken cancellationToken = default)
    {
        var v_dicRoleMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var v_objUser in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var v_arrRoles = await _userManager.GetRolesAsync(v_objUser);
            v_dicRoleMap[v_objUser.Id] = v_arrRoles.FirstOrDefault() ?? string.Empty;
        }

        return v_dicRoleMap;
    }

    public async Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var v_objUser = await _userManager.FindByIdAsync(userId);
        return v_objUser?.IsActive == true;
    }
}
