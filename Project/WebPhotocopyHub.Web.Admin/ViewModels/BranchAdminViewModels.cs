using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Web.Admin.Models;

public sealed class BranchEditViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Mã cơ sở")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [RegularExpression("^[a-z0-9][a-z0-9-]{2,79}$", ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Tên cơ sở")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "Điện thoại")]
    public string? PhoneNumber { get; set; }

    [StringLength(200), EmailAddress]
    public string? Email { get; set; }

    [StringLength(200)]
    [Display(Name = "Giờ hoạt động")]
    public string? OpenHours { get; set; }

    [StringLength(1000)]
    [Display(Name = "Mô tả ngắn")]
    public string? ShortDescription { get; set; }

    [StringLength(1000)]
    [Display(Name = "Ghi chú cho khách hàng")]
    public string? CustomerNote { get; set; }

    [StringLength(2000)]
    [Display(Name = "Dịch vụ nổi bật, mỗi dòng một mục")]
    public string? PopularServices { get; set; }

    [StringLength(2000)]
    [Display(Name = "Tùy chọn nhanh, mỗi dòng một mục")]
    public string? QuickOptions { get; set; }

    [Display(Name = "Cơ sở đang hoạt động")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Đang nhận đơn")]
    public bool IsAcceptingOrders { get; set; } = true;
}

public sealed class BranchFeaturesViewModel
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<BranchFeatureOptionViewModel> Features { get; set; } = new();
}

public sealed class BranchFeatureOptionViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public sealed class BranchUsersViewModel
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public IReadOnlyList<ApplicationUser> Users { get; set; } = Array.Empty<ApplicationUser>();
    public IReadOnlyList<BranchRole> Roles { get; set; } = Array.Empty<BranchRole>();
    public IReadOnlyList<UserBranchMembership> Memberships { get; set; } = Array.Empty<UserBranchMembership>();
}

public sealed class AssignBranchUserViewModel
{
    [Required]
    public Guid BranchId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public Guid BranchRoleId { get; set; }

    public bool IsPrimary { get; set; }
}

public sealed class BranchRolePermissionsViewModel
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid BranchRoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }
    public List<BranchPermissionOptionViewModel> Permissions { get; set; } = new();
}

public sealed class BranchPermissionOptionViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
