using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Branching;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Infrastructure.Data;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Infrastructure.Services;

public sealed class BranchManagementService : IBranchManagementService
{
    private readonly ApplicationDbContext _dbContext;

    public BranchManagementService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Branches
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Branch?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSlug(slug);
        return _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == normalized && x.IsActive, cancellationToken);
    }

    public async Task<Branch> SaveAsync(Branch branch, string actorUserId, CancellationToken cancellationToken = default)
    {
        branch.Code = branch.Code.Trim().ToUpperInvariant();
        branch.Slug = NormalizeSlug(branch.Slug);
        branch.Name = branch.Name.Trim();

        if (string.IsNullOrWhiteSpace(branch.Code) || string.IsNullOrWhiteSpace(branch.Slug) || string.IsNullOrWhiteSpace(branch.Name))
        {
            throw new BusinessException("Mã, slug và tên cơ sở là bắt buộc.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(branch.Slug, "^[a-z0-9][a-z0-9-]{2,79}$"))
        {
            throw new BusinessException("Slug cơ sở chỉ gồm chữ thường, số và dấu gạch ngang.");
        }

        var duplicate = await _dbContext.Branches.AnyAsync(
            x => x.Id != branch.Id && (x.Code == branch.Code || x.Slug == branch.Slug),
            cancellationToken);
        if (duplicate)
        {
            throw new BusinessException("Mã hoặc slug cơ sở đã tồn tại.");
        }

        var isNew = branch.Id == Guid.Empty || !await _dbContext.Branches.AnyAsync(x => x.Id == branch.Id, cancellationToken);
        if (isNew)
        {
            branch.Id = branch.Id == Guid.Empty ? Guid.NewGuid() : branch.Id;
            _dbContext.Branches.Add(branch);
        }
        else
        {
            var current = await _dbContext.Branches.FirstAsync(x => x.Id == branch.Id, cancellationToken);
            current.Code = branch.Code;
            current.Slug = branch.Slug;
            current.Name = branch.Name;
            current.Address = branch.Address?.Trim();
            current.PhoneNumber = branch.PhoneNumber?.Trim();
            current.Email = branch.Email?.Trim();
            current.OpenHours = branch.OpenHours?.Trim();
            current.ShortDescription = branch.ShortDescription?.Trim();
            current.CustomerNote = branch.CustomerNote?.Trim();
            current.PopularServices = branch.PopularServices?.Trim();
            current.QuickOptions = branch.QuickOptions?.Trim();
            current.IsActive = branch.IsActive;
            current.IsAcceptingOrders = branch.IsAcceptingOrders;
            branch = current;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (isNew)
        {
            await EnsureDefaultsForBranchAsync(branch.Id, cancellationToken);
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = isNew ? "CreateBranch" : "UpdateBranch",
            EntityName = nameof(Branch),
            EntityId = branch.Id.ToString(),
            Details = $"Code={branch.Code}; Slug={branch.Slug}; Active={branch.IsActive}; AcceptingOrders={branch.IsAcceptingOrders}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncStaticCatalogAsync(cancellationToken);
        return branch;
    }

    public async Task<IReadOnlySet<string>> GetEnabledFeaturesAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.BranchFeatures
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsEnabled)
            .Select(x => x.FeatureCode)
            .ToListAsync(cancellationToken);
        return items.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task SetFeaturesAsync(Guid branchId, IEnumerable<string> enabledFeatureCodes, string actorUserId, CancellationToken cancellationToken = default)
    {
        var allowed = BranchFeatureCodes.All.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var enabled = enabledFeatureCodes.Where(allowed.Contains).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existing = await _dbContext.BranchFeatures.Where(x => x.BranchId == branchId).ToListAsync(cancellationToken);

        foreach (var feature in BranchFeatureCodes.All)
        {
            var item = existing.FirstOrDefault(x => x.FeatureCode == feature.Code);
            if (item is null)
            {
                item = new BranchFeature { BranchId = branchId, FeatureCode = feature.Code };
                _dbContext.BranchFeatures.Add(item);
            }

            item.IsEnabled = enabled.Contains(feature.Code);
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedByUserId = actorUserId;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "SetBranchFeatures",
            EntityName = nameof(Branch),
            EntityId = branchId.ToString(),
            Details = string.Join(',', enabled.OrderBy(x => x))
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BranchRole>> GetRolesAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BranchRoles
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<BranchRole?> GetRoleAsync(Guid branchRoleId, CancellationToken cancellationToken = default)
    {
        return _dbContext.BranchRoles
            .AsNoTracking()
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == branchRoleId && x.IsActive, cancellationToken);
    }

    public async Task SetRolePermissionsAsync(
        Guid branchRoleId,
        IEnumerable<string> permissionCodes,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.BranchRoles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == branchRoleId && x.IsActive, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy vai trò cơ sở.");

        var allowed = BranchPermissionCodes.All.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = permissionCodes
            .Where(allowed.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removed = role.Permissions
            .Where(x => !selected.Contains(x.PermissionCode))
            .ToList();
        if (removed.Count > 0)
        {
            _dbContext.BranchRolePermissions.RemoveRange(removed);
        }

        var existing = role.Permissions
            .Select(x => x.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var code in selected)
        {
            if (!existing.Contains(code))
            {
                role.Permissions.Add(new BranchRolePermission { PermissionCode = code });
            }
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "SetBranchRolePermissions",
            EntityName = nameof(BranchRole),
            EntityId = role.Id.ToString(),
            Details = $"BranchId={role.BranchId}; Permissions={string.Join(',', selected.OrderBy(x => x))}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserBranchMembership>> GetMembershipsAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBranchMemberships
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.BranchRole)
            .Where(x => x.BranchId == branchId)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.User!.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AssignUserAsync(Guid branchId, string userId, Guid branchRoleId, bool isPrimary, string actorUserId, CancellationToken cancellationToken = default)
    {
        var roleValid = await _dbContext.BranchRoles.AnyAsync(x => x.Id == branchRoleId && x.BranchId == branchId && x.IsActive, cancellationToken);
        if (!roleValid)
        {
            throw new BusinessException("Vai trò không thuộc cơ sở đã chọn.");
        }

        var membership = await _dbContext.UserBranchMemberships.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.UserId == userId,
            cancellationToken);

        if (membership is null)
        {
            membership = new UserBranchMembership
            {
                BranchId = branchId,
                UserId = userId,
                BranchRoleId = branchRoleId,
                IsPrimary = isPrimary,
                IsActive = true,
                AssignedByUserId = actorUserId
            };
            _dbContext.UserBranchMemberships.Add(membership);
        }
        else
        {
            membership.BranchRoleId = branchRoleId;
            membership.IsPrimary = isPrimary;
            membership.IsActive = true;
            membership.AssignedByUserId = actorUserId;
        }

        if (isPrimary)
        {
            var otherPrimary = await _dbContext.UserBranchMemberships
                .Where(x => x.UserId == userId && x.Id != membership.Id && x.IsPrimary)
                .ToListAsync(cancellationToken);
            foreach (var item in otherPrimary)
            {
                item.IsPrimary = false;
            }
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "AssignUserToBranch",
            EntityName = nameof(UserBranchMembership),
            EntityId = membership.Id.ToString(),
            Details = $"BranchId={branchId}; UserId={userId}; BranchRoleId={branchRoleId}; Primary={isPrimary}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveUserAsync(Guid membershipId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.UserBranchMemberships.FirstOrDefaultAsync(x => x.Id == membershipId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy phân công cơ sở.");
        membership.IsActive = false;
        membership.IsPrimary = false;
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "DisableUserBranchMembership",
            EntityName = nameof(UserBranchMembership),
            EntityId = membership.Id.ToString(),
            Details = $"BranchId={membership.BranchId}; UserId={membership.UserId}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsFeatureEnabledAsync(Guid branchId, string featureCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.BranchFeatures.AsNoTracking().AnyAsync(
            x => x.BranchId == branchId && x.FeatureCode == featureCode && x.IsEnabled,
            cancellationToken);
    }

    public Task<bool> UserHasPermissionAsync(string userId, Guid branchId, string permissionCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserBranchMemberships
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BranchId == branchId && x.IsActive && x.BranchRole!.IsActive)
            .AnyAsync(x => x.BranchRole!.Permissions.Any(p => p.PermissionCode == permissionCode), cancellationToken);
    }

    public async Task EnsureDefaultsForBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        foreach (var feature in BranchFeatureCodes.All)
        {
            if (!await _dbContext.BranchFeatures.AnyAsync(x => x.BranchId == branchId && x.FeatureCode == feature.Code, cancellationToken))
            {
                _dbContext.BranchFeatures.Add(new BranchFeature
                {
                    BranchId = branchId,
                    FeatureCode = feature.Code,
                    IsEnabled = true
                });
            }
        }

        var roleTemplates = GetRoleTemplates();
        foreach (var template in roleTemplates)
        {
            var role = await _dbContext.BranchRoles
                .Include(x => x.Permissions)
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.Name == template.Name, cancellationToken);
            if (role is null)
            {
                role = new BranchRole
                {
                    BranchId = branchId,
                    Name = template.Name,
                    Description = template.Description,
                    IsSystemRole = true,
                    IsActive = true
                };
                foreach (var permission in template.Permissions)
                {
                    role.Permissions.Add(new BranchRolePermission { PermissionCode = permission });
                }

                _dbContext.BranchRoles.Add(role);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (branchId != BranchDefaults.PrimaryBranchId)
        {
            await CloneCatalogDataAsync(branchId, cancellationToken);
        }
    }

    public async Task SyncStaticCatalogAsync(CancellationToken cancellationToken = default)
    {
        var branches = await _dbContext.Branches.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ShopBranchCatalog.ReplaceFromEntities(branches);
    }

    private async Task CloneCatalogDataAsync(Guid branchId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Products.IgnoreQueryFilters().AnyAsync(x => x.BranchId == branchId, cancellationToken))
        {
            var products = await _dbContext.Products.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == BranchDefaults.PrimaryBranchId)
                .ToListAsync(cancellationToken);
            foreach (var item in products)
            {
                _dbContext.Products.Add(new Product
                {
                    BranchId = branchId,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    StockQuantity = 0,
                    ImageUrl = item.ImageUrl,
                    IsActive = item.IsActive
                });
            }
        }

        if (!await _dbContext.SupportServices.IgnoreQueryFilters().AnyAsync(x => x.BranchId == branchId, cancellationToken))
        {
            var services = await _dbContext.SupportServices.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == BranchDefaults.PrimaryBranchId)
                .ToListAsync(cancellationToken);
            foreach (var item in services)
            {
                _dbContext.SupportServices.Add(new SupportService
                {
                    BranchId = branchId,
                    Name = item.Name,
                    Description = item.Description,
                    UnitPrice = item.UnitPrice,
                    FeeType = item.FeeType,
                    IsActive = item.IsActive
                });
            }
        }

        if (!await _dbContext.PricingRules.IgnoreQueryFilters().AnyAsync(x => x.BranchId == branchId, cancellationToken))
        {
            var rules = await _dbContext.PricingRules.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == BranchDefaults.PrimaryBranchId)
                .ToListAsync(cancellationToken);
            foreach (var item in rules)
            {
                _dbContext.PricingRules.Add(new PricingRule
                {
                    BranchId = branchId,
                    PaperSize = item.PaperSize,
                    PrintSide = item.PrintSide,
                    ColorMode = item.ColorMode,
                    IsPhoto = item.IsPhoto,
                    UnitPrice = item.UnitPrice,
                    IsActive = item.IsActive
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeSlug(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<RoleTemplate> GetRoleTemplates()
    {
        return new[]
        {
            new RoleTemplate("Quản lý cơ sở", "Toàn quyền vận hành trong cơ sở.", BranchPermissionCodes.All),
            new RoleTemplate("Nhân viên in ấn", "Xem và xử lý đơn in, xem/tải file.", new[]
            {
                BranchPermissionCodes.DashboardView,
                BranchPermissionCodes.PrintJobsView,
                BranchPermissionCodes.PrintJobsManage,
                BranchPermissionCodes.PrintJobsFiles
            }),
            new RoleTemplate("Thu ngân", "Nạp tiền, duyệt giao dịch và theo dõi tài chính tại quầy.", new[]
            {
                BranchPermissionCodes.DashboardView,
                BranchPermissionCodes.TopUpsView,
                BranchPermissionCodes.TopUpsReview,
                BranchPermissionCodes.CounterTopUp
            }),
            new RoleTemplate("Nhân viên kho", "Theo dõi và điều chỉnh tồn kho.", new[]
            {
                BranchPermissionCodes.DashboardView,
                BranchPermissionCodes.ProductOrdersView,
                BranchPermissionCodes.ProductOrdersManage,
                BranchPermissionCodes.InventoryView,
                BranchPermissionCodes.InventoryAdjust
            }),
            new RoleTemplate("Chỉ xem báo cáo", "Chỉ xem dashboard và báo cáo.", new[]
            {
                BranchPermissionCodes.DashboardView,
                BranchPermissionCodes.ReportsView
            })
        };
    }

    private sealed record RoleTemplate(string Name, string Description, IReadOnlyList<string> Permissions);
}
