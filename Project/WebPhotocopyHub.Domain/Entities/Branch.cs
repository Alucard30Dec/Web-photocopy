using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class Branch : BaseEntity, IHasRowVersion
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? OpenHours { get; set; }

    [MaxLength(1000)]
    public string? ShortDescription { get; set; }

    [MaxLength(1000)]
    public string? CustomerNote { get; set; }

    [MaxLength(2000)]
    public string? PopularServices { get; set; }

    [MaxLength(2000)]
    public string? QuickOptions { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsAcceptingOrders { get; set; } = true;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public ICollection<BranchFeature> Features { get; set; } = new List<BranchFeature>();
    public ICollection<BranchRole> Roles { get; set; } = new List<BranchRole>();
    public ICollection<UserBranchMembership> Memberships { get; set; } = new List<UserBranchMembership>();
}
