using System.ComponentModel.DataAnnotations;

namespace WebPhotocopyHub.Domain.Entities;

public class BranchRolePermission
{
    public Guid BranchRoleId { get; set; }

    [Required, MaxLength(120)]
    public string PermissionCode { get; set; } = string.Empty;

    public BranchRole? BranchRole { get; set; }
}
