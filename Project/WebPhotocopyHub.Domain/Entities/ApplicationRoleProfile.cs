using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebPhotocopyHub.Domain.Entities;

public class ApplicationRoleProfile
{
    [Key, MaxLength(191)]
    public string RoleId { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public IdentityRole? Role { get; set; }
    public ICollection<RoleFunctionPermission> FunctionPermissions { get; set; } = new List<RoleFunctionPermission>();
}
