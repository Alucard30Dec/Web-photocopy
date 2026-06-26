using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class BranchRole : BaseEntity
{
    public Guid BranchId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;

    public Branch? Branch { get; set; }
    public ICollection<BranchRolePermission> Permissions { get; set; } = new List<BranchRolePermission>();
    public ICollection<UserBranchMembership> Memberships { get; set; } = new List<UserBranchMembership>();
}
