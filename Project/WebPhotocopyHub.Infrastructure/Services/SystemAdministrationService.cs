using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Application.Security;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Infrastructure.Services;

public sealed class SystemAdministrationService : ISystemAdministrationService
{
    private static readonly Regex CodePattern = new(
        "^[A-Za-z][A-Za-z0-9._-]{1,99}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RouteTokenPattern = new(
        "^[A-Za-z][A-Za-z0-9]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> AllowedIconKeys = new(
        new[]
        {
            "grid", "dashboard", "data", "import", "export", "report",
            "admin", "settings", "user", "users", "key", "log", "api", "globe"
        },
        StringComparer.OrdinalIgnoreCase);

    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly IBranchContext _branchContext;

    public SystemAdministrationService(
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService,
        IBranchContext branchContext)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _branchContext = branchContext;
    }

    public async Task<IReadOnlyList<SystemFunction>> GetFunctionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemFunctions
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<SystemFunction?> GetFunctionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SystemFunctions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<SystemFunction> SaveFunctionAsync(
        SystemFunction function,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        NormalizeFunction(function);
        await ValidateFunctionAsync(function, cancellationToken);

        var isNew = function.Id == Guid.Empty
            || !await _dbContext.SystemFunctions.AnyAsync(
                x => x.Id == function.Id,
                cancellationToken);

        SystemFunction target;
        if (isNew)
        {
            target = function;
            target.Id = target.Id == Guid.Empty ? Guid.NewGuid() : target.Id;
            target.CreatedAt = DateTime.UtcNow;
            _dbContext.SystemFunctions.Add(target);
        }
        else
        {
            target = await _dbContext.SystemFunctions
                .FirstAsync(x => x.Id == function.Id, cancellationToken);

            if (target.IsSystemFunction)
            {
                function.Code = target.Code;
                function.Area = target.Area;
                function.Controller = target.Controller;
                function.Action = target.Action;
                function.IsSystemFunction = true;
            }

            target.Code = function.Code;
            target.Name = function.Name;
            target.Description = function.Description;
            target.ParentId = function.ParentId;
            target.Area = function.Area;
            target.Controller = function.Controller;
            target.Action = function.Action;
            target.IconKey = function.IconKey;
            target.RequiredBranchFeatureCode = function.RequiredBranchFeatureCode;
            target.SortOrder = function.SortOrder;
            target.RequiresBranchSelection = function.RequiresBranchSelection;
            target.IsMenuItem = function.IsMenuItem;
            target.IsActive = function.IsActive;
            target.SupportsView = function.SupportsView;
            target.SupportsCreate = function.SupportsCreate;
            target.SupportsEdit = function.SupportsEdit;
            target.SupportsDelete = function.SupportsDelete;
            target.SupportsExport = function.SupportsExport;
            target.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            actorUserId,
            isNew ? "CreateSystemFunction" : "UpdateSystemFunction",
            nameof(SystemFunction),
            target.Id.ToString(),
            $"Code={target.Code}; Controller={target.Controller}; Active={target.IsActive}",
            cancellationToken);

        return target;
    }

    public async Task SetFunctionActiveAsync(
        Guid id,
        bool isActive,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var function = await _dbContext.SystemFunctions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy chức năng hệ thống.");

        if (function.IsSystemFunction && !isActive)
        {
            throw new BusinessException("Không thể vô hiệu hóa chức năng hệ thống mặc định.");
        }

        if (!isActive)
        {
            var hasActiveChildren = await _dbContext.SystemFunctions
                .AnyAsync(x => x.ParentId == id && x.IsActive, cancellationToken);
            if (hasActiveChildren)
            {
                throw new BusinessException("Hãy vô hiệu hóa các chức năng con trước.");
            }
        }

        function.IsActive = isActive;
        function.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(
            actorUserId,
            "SetSystemFunctionActive",
            nameof(SystemFunction),
            function.Id.ToString(),
            $"Code={function.Code}; IsActive={function.IsActive}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<SystemRoleListItemDto>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var profiles = await _dbContext.ApplicationRoleProfiles
            .AsNoTracking()
            .ToDictionaryAsync(x => x.RoleId, cancellationToken);

        var userCounts = await _dbContext.UserRoles
            .AsNoTracking()
            .GroupBy(x => x.RoleId)
            .Select(x => new { RoleId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var permissionCounts = await _dbContext.RoleFunctionPermissions
            .AsNoTracking()
            .Where(x => x.CanView)
            .GroupBy(x => x.RoleId)
            .Select(x => new { RoleId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles.Select(role =>
        {
            profiles.TryGetValue(role.Id, out var profile);
            userCounts.TryGetValue(role.Id, out var userCount);
            permissionCounts.TryGetValue(role.Id, out var permissionCount);

            return new SystemRoleListItemDto(
                role.Id,
                role.Name ?? string.Empty,
                profile?.DisplayName ?? role.Name ?? string.Empty,
                profile?.Description,
                profile?.IsSystemRole ?? IsReservedRole(role.Name),
                profile?.IsActive ?? true,
                userCount,
                permissionCount);
        }).ToList();
    }

    public async Task<SystemRoleEditDto?> GetRoleAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var profile = await _dbContext.ApplicationRoleProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);

        return new SystemRoleEditDto
        {
            RoleId = role.Id,
            RoleName = role.Name ?? string.Empty,
            DisplayName = profile?.DisplayName ?? role.Name ?? string.Empty,
            Description = profile?.Description,
            IsActive = profile?.IsActive ?? true
        };
    }

    public async Task<string> SaveRoleAsync(
        SystemRoleEditDto roleInput,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        roleInput.RoleName = roleInput.RoleName.Trim();
        roleInput.DisplayName = roleInput.DisplayName.Trim();
        roleInput.Description = NormalizeNullable(roleInput.Description);

        if (!CodePattern.IsMatch(roleInput.RoleName))
        {
            throw new BusinessException(
                "Tên role chỉ được dùng chữ, số, dấu chấm, gạch ngang hoặc gạch dưới.");
        }

        if (string.IsNullOrWhiteSpace(roleInput.DisplayName))
        {
            throw new BusinessException("Tên hiển thị của vai trò là bắt buộc.");
        }

        IdentityRole role;
        var isNew = string.IsNullOrWhiteSpace(roleInput.RoleId);
        if (isNew)
        {
            if (await _roleManager.RoleExistsAsync(roleInput.RoleName))
            {
                throw new BusinessException("Tên role đã tồn tại.");
            }

            role = new IdentityRole(roleInput.RoleName);
            var createResult = await _roleManager.CreateAsync(role);
            EnsureIdentityResult(createResult, "Không thể tạo vai trò.");
        }
        else
        {
            role = await _roleManager.FindByIdAsync(roleInput.RoleId!)
                ?? throw new BusinessException("Không tìm thấy vai trò.");

            var profile = await _dbContext.ApplicationRoleProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.RoleId == role.Id, cancellationToken);
            var isSystemRole = profile?.IsSystemRole ?? IsReservedRole(role.Name);

            if (isSystemRole
                && !string.Equals(role.Name, roleInput.RoleName, StringComparison.Ordinal))
            {
                throw new BusinessException("Không thể đổi tên role hệ thống.");
            }

            if (!isSystemRole
                && !string.Equals(role.Name, roleInput.RoleName, StringComparison.Ordinal))
            {
                var duplicate = await _roleManager.FindByNameAsync(roleInput.RoleName);
                if (duplicate is not null && !string.Equals(duplicate.Id, role.Id, StringComparison.Ordinal))
                {
                    throw new BusinessException("Tên role đã tồn tại.");
                }

                role.Name = roleInput.RoleName;
                var updateRoleResult = await _roleManager.UpdateAsync(role);
                EnsureIdentityResult(updateRoleResult, "Không thể đổi tên vai trò.");
            }
        }

        var roleProfile = await _dbContext.ApplicationRoleProfiles
            .FirstOrDefaultAsync(x => x.RoleId == role.Id, cancellationToken);

        if (!isNew
            && roleProfile?.IsActive == true
            && !roleInput.IsActive
            && await _dbContext.UserRoles.AsNoTracking().AnyAsync(
                x => x.UserId == actorUserId && x.RoleId == role.Id,
                cancellationToken))
        {
            throw new BusinessException(
                "Không thể tự vô hiệu hóa vai trò đang được gán cho tài khoản của bạn.");
        }

        if (roleProfile is null)
        {
            roleProfile = new ApplicationRoleProfile
            {
                RoleId = role.Id,
                DisplayName = roleInput.DisplayName,
                Description = roleInput.Description,
                IsSystemRole = IsReservedRole(role.Name),
                IsActive = roleInput.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ApplicationRoleProfiles.Add(roleProfile);
        }
        else
        {
            roleProfile.DisplayName = roleInput.DisplayName;
            roleProfile.Description = roleInput.Description;
            if (!roleProfile.IsSystemRole)
            {
                roleProfile.IsActive = roleInput.IsActive;
            }
            roleProfile.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            actorUserId,
            isNew ? "CreateSystemRole" : "UpdateSystemRole",
            nameof(IdentityRole),
            role.Id,
            $"RoleName={role.Name}; DisplayName={roleProfile.DisplayName}; Active={roleProfile.IsActive}",
            cancellationToken);

        return role.Id;
    }

    public async Task SetRoleActiveAsync(
        string roleId,
        bool isActive,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy vai trò.");

        var profile = await _dbContext.ApplicationRoleProfiles
            .FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);

        if (profile?.IsSystemRole == true || IsReservedRole(role.Name))
        {
            throw new BusinessException("Không thể vô hiệu hóa role hệ thống.");
        }

        if (!isActive
            && await _dbContext.UserRoles.AsNoTracking().AnyAsync(
                x => x.UserId == actorUserId && x.RoleId == roleId,
                cancellationToken))
        {
            throw new BusinessException(
                "Không thể tự vô hiệu hóa vai trò đang được gán cho tài khoản của bạn.");
        }

        if (profile is null)
        {
            profile = new ApplicationRoleProfile
            {
                RoleId = role.Id,
                DisplayName = role.Name ?? string.Empty,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ApplicationRoleProfiles.Add(profile);
        }
        else
        {
            profile.IsActive = isActive;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            actorUserId,
            "SetSystemRoleActive",
            nameof(IdentityRole),
            role.Id,
            $"RoleName={role.Name}; Active={isActive}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<SystemPermissionMatrixItemDto>> GetPermissionMatrixAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(x => x.Id == roleId, cancellationToken);
        if (!roleExists)
        {
            throw new BusinessException("Không tìm thấy vai trò.");
        }

        var functions = await _dbContext.SystemFunctions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var grants = await _dbContext.RoleFunctionPermissions
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .ToDictionaryAsync(x => x.SystemFunctionId, cancellationToken);

        var result = new List<SystemPermissionMatrixItemDto>();
        foreach (var item in FlattenFunctions(functions))
        {
            grants.TryGetValue(item.Function.Id, out var grant);
            result.Add(new SystemPermissionMatrixItemDto
            {
                FunctionId = item.Function.Id,
                ParentId = item.Function.ParentId,
                FunctionCode = item.Function.Code,
                FunctionName = item.Function.Name,
                SortOrder = item.Function.SortOrder,
                Level = item.Level,
                SupportsView = item.Function.SupportsView,
                SupportsCreate = item.Function.SupportsCreate,
                SupportsEdit = item.Function.SupportsEdit,
                SupportsDelete = item.Function.SupportsDelete,
                SupportsExport = item.Function.SupportsExport,
                CanView = grant?.CanView ?? false,
                CanCreate = grant?.CanCreate ?? false,
                CanEdit = grant?.CanEdit ?? false,
                CanDelete = grant?.CanDelete ?? false,
                CanExport = grant?.CanExport ?? false
            });
        }

        return result;
    }

    public async Task SavePermissionMatrixAsync(
        string roleId,
        IEnumerable<SystemPermissionUpdateDto> permissions,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy vai trò.");

        var profile = await _dbContext.ApplicationRoleProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);

        if (profile?.IsSystemRole == true
            && string.Equals(role.Name, RoleConstants.Admin, StringComparison.Ordinal))
        {
            throw new BusinessException("Role Admin có toàn quyền mặc định và không cần cấu hình ma trận quyền.");
        }

        var functionMap = await _dbContext.SystemFunctions
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var requested = permissions
            .GroupBy(x => x.FunctionId)
            .Select(x => x.Last())
            .Where(x => functionMap.ContainsKey(x.FunctionId))
            .ToList();

        var existing = await _dbContext.RoleFunctionPermissions
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.RoleFunctionPermissions.RemoveRange(existing);

        foreach (var item in requested)
        {
            var function = functionMap[item.FunctionId];
            var canCreate = function.SupportsCreate && item.CanCreate;
            var canEdit = function.SupportsEdit && item.CanEdit;
            var canDelete = function.SupportsDelete && item.CanDelete;
            var canExport = function.SupportsExport && item.CanExport;
            var canView = function.SupportsView
                && (item.CanView || canCreate || canEdit || canDelete || canExport);

            if (!canView && !canCreate && !canEdit && !canDelete && !canExport)
            {
                continue;
            }

            _dbContext.RoleFunctionPermissions.Add(new RoleFunctionPermission
            {
                RoleId = roleId,
                SystemFunctionId = function.Id,
                CanView = canView,
                CanCreate = canCreate,
                CanEdit = canEdit,
                CanDelete = canDelete,
                CanExport = canExport
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            actorUserId,
            "SetSystemRolePermissions",
            nameof(RoleFunctionPermission),
            roleId,
            $"RoleName={role.Name}; PermissionRows={requested.Count}",
            cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string area,
        string controller,
        string permissionAction,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (principal.IsInRole(RoleConstants.Admin))
        {
            return true;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var userIsActive = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (!userIsActive)
        {
            return false;
        }

        var function = await _dbContext.SystemFunctions
            .AsNoTracking()
            .Where(x => x.IsActive
                        && x.Area == area
                        && x.Controller != null
                        && x.Controller.ToLower() == controller.ToLower())
            .OrderBy(x => x.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
        if (function is null)
        {
            return false;
        }

        var roleIds = await GetActiveRoleIdsAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
        {
            return false;
        }

        var grants = await _dbContext.RoleFunctionPermissions
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId)
                        && x.SystemFunctionId == function.Id)
            .ToListAsync(cancellationToken);

        return permissionAction switch
        {
            SystemPermissionActions.Create =>
                function.SupportsCreate && grants.Any(x => x.CanView && x.CanCreate),
            SystemPermissionActions.Edit =>
                function.SupportsEdit && grants.Any(x => x.CanView && x.CanEdit),
            SystemPermissionActions.Delete =>
                function.SupportsDelete && grants.Any(x => x.CanView && x.CanDelete),
            SystemPermissionActions.Export =>
                function.SupportsExport && grants.Any(x => x.CanView && x.CanExport),
            _ =>
                function.SupportsView && grants.Any(x => x.CanView)
        };
    }

    public async Task<bool> CanAccessAdminPortalAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsActive)
        {
            return false;
        }

        if (await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
        {
            return true;
        }

        var roleIds = await GetActiveRoleIdsAsync(user.Id, cancellationToken);
        if (roleIds.Count == 0)
        {
            return false;
        }

        return await _dbContext.RoleFunctionPermissions
            .AsNoTracking()
            .AnyAsync(
                x => roleIds.Contains(x.RoleId)
                     && x.CanView
                     && x.SystemFunction != null
                     && x.SystemFunction.IsActive,
                cancellationToken);
    }

    public async Task<bool> CanAccessAdminPortalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (principal.IsInRole(RoleConstants.Admin))
        {
            return true;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var userIsActive = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (!userIsActive)
        {
            return false;
        }

        var roleIds = await GetActiveRoleIdsAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
        {
            return false;
        }

        return await _dbContext.RoleFunctionPermissions
            .AsNoTracking()
            .AnyAsync(
                x => roleIds.Contains(x.RoleId)
                     && x.CanView
                     && x.SystemFunction != null
                     && x.SystemFunction.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SystemNavigationItemDto>> GetNavigationAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Array.Empty<SystemNavigationItemDto>();
        }

        var isAdmin = principal.IsInRole(RoleConstants.Admin);
        HashSet<Guid>? grantedFunctionIds = null;

        if (!isAdmin)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Array.Empty<SystemNavigationItemDto>();
            }

            var roleIds = await GetActiveRoleIdsAsync(userId, cancellationToken);
            if (roleIds.Count == 0)
            {
                return Array.Empty<SystemNavigationItemDto>();
            }

            grantedFunctionIds = (await _dbContext.RoleFunctionPermissions
                    .AsNoTracking()
                    .Where(x => roleIds.Contains(x.RoleId) && x.CanView)
                    .Select(x => x.SystemFunctionId)
                    .Distinct()
                    .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        var enabledBranchFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_branchContext.BranchId.HasValue)
        {
            enabledBranchFeatures = (await _dbContext.BranchFeatures
                    .AsNoTracking()
                    .Where(x => x.BranchId == _branchContext.BranchId.Value && x.IsEnabled)
                    .Select(x => x.FeatureCode)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var allFunctions = await _dbContext.SystemFunctions
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsMenuItem && x.Area == "Admin")
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        bool PassesBranchScope(SystemFunction function)
        {
            if (function.RequiresBranchSelection && !_branchContext.BranchId.HasValue)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(function.RequiredBranchFeatureCode)
                && !enabledBranchFeatures.Contains(function.RequiredBranchFeatureCode))
            {
                return false;
            }

            return true;
        }

        var directlyVisibleIds = allFunctions
            .Where(PassesBranchScope)
            .Where(x => isAdmin || grantedFunctionIds!.Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSet();

        var functionById = allFunctions.ToDictionary(x => x.Id);
        var visibleIds = new HashSet<Guid>(directlyVisibleIds);
        foreach (var visibleId in directlyVisibleIds.ToList())
        {
            var current = functionById.GetValueOrDefault(visibleId);
            while (current?.ParentId is Guid parentId
                   && functionById.TryGetValue(parentId, out var parent))
            {
                visibleIds.Add(parent.Id);
                current = parent;
            }
        }

        var visibleFunctions = allFunctions
            .Where(x => visibleIds.Contains(x.Id))
            .ToList();

        var itemMap = visibleFunctions.ToDictionary(
            x => x.Id,
            x => new SystemNavigationItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Code = x.Code,
                Name = x.Name,
                Area = x.Area,
                Controller = x.Controller,
                Action = x.Action,
                IconKey = NormalizeIcon(x.IconKey),
                SortOrder = x.SortOrder
            });

        foreach (var item in itemMap.Values)
        {
            if (item.ParentId.HasValue
                && itemMap.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children.Add(item);
            }
        }

        foreach (var item in itemMap.Values)
        {
            item.Children = item.Children
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();
        }

        return itemMap.Values
            .Where(x => !x.ParentId.HasValue || !itemMap.ContainsKey(x.ParentId.Value))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<Branch>> GetAccessibleAdminBranchesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Array.Empty<Branch>();
        }

        if (principal.IsInRole(RoleConstants.Admin))
        {
            return await _dbContext.Branches
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<Branch>();
        }

        return await (
                from membership in _dbContext.UserBranchMemberships.AsNoTracking()
                join branch in _dbContext.Branches.AsNoTracking()
                    on membership.BranchId equals branch.Id
                join branchRole in _dbContext.BranchRoles.AsNoTracking()
                    on membership.BranchRoleId equals branchRole.Id
                where membership.UserId == userId
                      && membership.IsActive
                      && branch.IsActive
                      && branchRole.IsActive
                orderby membership.IsPrimary descending, branch.Name
                select branch)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetFirstAccessibleAdminPathAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var navigation = await GetNavigationAsync(principal, cancellationToken);

        static SystemNavigationItemDto? FindFirstLink(
            IEnumerable<SystemNavigationItemDto> items)
        {
            foreach (var item in items
                         .OrderBy(x => x.SortOrder)
                         .ThenBy(x => x.Name))
            {
                if (!string.IsNullOrWhiteSpace(item.Controller))
                {
                    return item;
                }

                var child = FindFirstLink(item.Children);
                if (child is not null)
                {
                    return child;
                }
            }

            return null;
        }

        var firstLink = FindFirstLink(navigation);
        if (firstLink is null)
        {
            return null;
        }

        return $"/Admin/{firstLink.Controller}/{firstLink.Action ?? "Index"}";
    }

    private async Task<HashSet<string>> GetActiveRoleIdsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return (await (
                from userRole in _dbContext.UserRoles.AsNoTracking()
                join profile in _dbContext.ApplicationRoleProfiles.AsNoTracking()
                    on userRole.RoleId equals profile.RoleId
                where userRole.UserId == userId && profile.IsActive
                select userRole.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task ValidateFunctionAsync(
        SystemFunction function,
        CancellationToken cancellationToken)
    {
        if (!CodePattern.IsMatch(function.Code))
        {
            throw new BusinessException(
                "Mã chức năng chỉ được dùng chữ, số, dấu chấm, gạch ngang hoặc gạch dưới.");
        }

        if (string.IsNullOrWhiteSpace(function.Name))
        {
            throw new BusinessException("Tên chức năng là bắt buộc.");
        }

        if (!string.Equals(function.Area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Phiên bản hiện tại chỉ quản lý chức năng trong khu vực Admin.");
        }

        if (!string.IsNullOrWhiteSpace(function.Controller)
            && !RouteTokenPattern.IsMatch(function.Controller))
        {
            throw new BusinessException("Controller không đúng định dạng tên C#.");
        }

        if (!string.IsNullOrWhiteSpace(function.Action)
            && !RouteTokenPattern.IsMatch(function.Action))
        {
            throw new BusinessException("Action không đúng định dạng tên C#.");
        }

        var duplicateCode = await _dbContext.SystemFunctions
            .AnyAsync(
                x => x.Id != function.Id
                     && x.Code.ToLower() == function.Code.ToLower(),
                cancellationToken);
        if (duplicateCode)
        {
            throw new BusinessException("Mã chức năng đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(function.Controller))
        {
            var duplicateRoute = await _dbContext.SystemFunctions
                .AnyAsync(
                    x => x.Id != function.Id
                         && x.Controller != null
                         && x.Area.ToLower() == function.Area.ToLower()
                         && x.Controller.ToLower() == function.Controller.ToLower(),
                    cancellationToken);
            if (duplicateRoute)
            {
                throw new BusinessException(
                    "Controller này đã được đăng ký cho một chức năng khác.");
            }
        }

        if (!function.ParentId.HasValue)
        {
            return;
        }

        if (function.ParentId.Value == function.Id)
        {
            throw new BusinessException("Chức năng không thể là cha của chính nó.");
        }

        var parentExists = await _dbContext.SystemFunctions
            .AnyAsync(x => x.Id == function.ParentId.Value, cancellationToken);
        if (!parentExists)
        {
            throw new BusinessException("Chức năng cha không tồn tại.");
        }

        var cursor = function.ParentId;
        while (cursor.HasValue)
        {
            if (cursor.Value == function.Id)
            {
                throw new BusinessException("Cấu trúc chức năng tạo thành vòng lặp.");
            }

            cursor = await _dbContext.SystemFunctions
                .AsNoTracking()
                .Where(x => x.Id == cursor.Value)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private static IEnumerable<(SystemFunction Function, int Level)> FlattenFunctions(
        IReadOnlyCollection<SystemFunction> functions)
    {
        var childrenByParent = functions.ToLookup(x => x.ParentId);

        IEnumerable<(SystemFunction Function, int Level)> Walk(Guid? parentId, int level)
        {
            var children = childrenByParent[parentId]
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

            foreach (var child in children)
            {
                yield return (child, level);
                foreach (var nested in Walk(child.Id, level + 1))
                {
                    yield return nested;
                }
            }
        }

        return Walk(null, 0).ToList();
    }

    private static void NormalizeFunction(SystemFunction function)
    {
        function.Code = function.Code.Trim();
        function.Name = function.Name.Trim();
        function.Description = NormalizeNullable(function.Description);
        function.Area = string.IsNullOrWhiteSpace(function.Area)
            ? "Admin"
            : function.Area.Trim();
        function.Controller = NormalizeNullable(function.Controller);
        function.Action = string.IsNullOrWhiteSpace(function.Controller)
            ? null
            : NormalizeNullable(function.Action) ?? "Index";
        function.IconKey = NormalizeIcon(function.IconKey);
        function.RequiredBranchFeatureCode = NormalizeNullable(function.RequiredBranchFeatureCode);

        if (string.IsNullOrWhiteSpace(function.Controller))
        {
            function.SupportsView = false;
            function.SupportsCreate = false;
            function.SupportsEdit = false;
            function.SupportsDelete = false;
            function.SupportsExport = false;
        }
        else
        {
            function.SupportsView = true;
        }
    }

    private static string NormalizeIcon(string? iconKey)
    {
        var normalized = string.IsNullOrWhiteSpace(iconKey)
            ? "grid"
            : iconKey.Trim().ToLowerInvariant();

        return AllowedIconKeys.Contains(normalized) ? normalized : "grid";
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsReservedRole(string? roleName)
    {
        return string.Equals(roleName, RoleConstants.Admin, StringComparison.Ordinal)
            || string.Equals(roleName, RoleConstants.Customer, StringComparison.Ordinal)
            || string.Equals(roleName, RoleConstants.ShopOperator, StringComparison.Ordinal);
    }

    private static void EnsureIdentityResult(
        IdentityResult result,
        string message)
    {
        if (!result.Succeeded)
        {
            throw new BusinessException(
                message + " " + string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    private Task WriteAuditAsync(
        string actorUserId,
        string action,
        string entityName,
        string entityId,
        string details,
        CancellationToken cancellationToken)
    {
        return _auditLogService.WriteAsync(new AuditLogEntryDto
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details
        }, cancellationToken);
    }
}
