using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.DataAccess.Controllers;

public sealed class AdminUserQueryService : IAdminUserQueryService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IBranchContext _branchContext;

    public AdminUserQueryService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IBranchContext branchContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _branchContext = branchContext;
    }

    public async Task<List<ApplicationUser>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var branchId = _branchContext.BranchId ?? BranchDefaults.PrimaryBranchId;
        var balances = await _dbContext.WalletAccounts
            .AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .ToDictionaryAsync(x => x.UserId, x => x.Balance, cancellationToken);

        foreach (var user in users)
        {
            user.BranchWalletBalance = balances.TryGetValue(user.Id, out var balance) ? balance : 0;
        }

        return users;
    }

    public async Task<Dictionary<string, string>> GetPrimaryRoleMapAsync(
        IEnumerable<ApplicationUser> users,
        CancellationToken cancellationToken = default)
    {
        var roleMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roles = await _userManager.GetRolesAsync(user);
            roleMap[user.Id] = roles.FirstOrDefault() ?? string.Empty;
        }

        return roleMap;
    }

    public async Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsActive == true;
    }
}
