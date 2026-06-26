using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class UserBranchMembership : BaseEntity
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public Guid BranchId { get; set; }
    public Guid BranchRoleId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(450)]
    public string? AssignedByUserId { get; set; }

    public ApplicationUser? User { get; set; }
    public Branch? Branch { get; set; }
    public BranchRole? BranchRole { get; set; }
}
