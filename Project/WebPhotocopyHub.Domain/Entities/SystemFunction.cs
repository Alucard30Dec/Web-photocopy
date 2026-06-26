using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class SystemFunction : BaseEntity
{
    [Required, MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    [Required, MaxLength(50)]
    public string Area { get; set; } = "Admin";

    [MaxLength(100)]
    public string? Controller { get; set; }

    [MaxLength(100)]
    public string? Action { get; set; } = "Index";

    [MaxLength(50)]
    public string IconKey { get; set; } = "grid";

    [MaxLength(100)]
    public string? RequiredBranchFeatureCode { get; set; }

    public int SortOrder { get; set; }
    public bool RequiresBranchSelection { get; set; }
    public bool IsMenuItem { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsSystemFunction { get; set; }

    public bool SupportsView { get; set; } = true;
    public bool SupportsCreate { get; set; }
    public bool SupportsEdit { get; set; }
    public bool SupportsDelete { get; set; }
    public bool SupportsExport { get; set; }

    public SystemFunction? Parent { get; set; }
    public ICollection<SystemFunction> Children { get; set; } = new List<SystemFunction>();
    public ICollection<RoleFunctionPermission> RolePermissions { get; set; } = new List<RoleFunctionPermission>();
}
