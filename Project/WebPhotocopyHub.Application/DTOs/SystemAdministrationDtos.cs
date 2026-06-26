namespace WebPhotocopyHub.Application.DTOs;

public sealed record SystemRoleListItemDto(
    string RoleId,
    string RoleName,
    string DisplayName,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    int UserCount,
    int GrantedFunctionCount);

public sealed class SystemRoleEditDto
{
    public string? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SystemPermissionMatrixItemDto
{
    public Guid FunctionId { get; set; }
    public Guid? ParentId { get; set; }
    public string FunctionCode { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Level { get; set; }

    public bool SupportsView { get; set; }
    public bool SupportsCreate { get; set; }
    public bool SupportsEdit { get; set; }
    public bool SupportsDelete { get; set; }
    public bool SupportsExport { get; set; }

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
}

public sealed class SystemPermissionUpdateDto
{
    public Guid FunctionId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
}

public sealed class SystemNavigationItemDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Area { get; set; } = "Admin";
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public string IconKey { get; set; } = "grid";
    public int SortOrder { get; set; }
    public List<SystemNavigationItemDto> Children { get; set; } = new();
}
