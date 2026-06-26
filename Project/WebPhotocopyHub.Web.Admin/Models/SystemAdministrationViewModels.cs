using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Web.Admin.Models;

public sealed class SystemFunctionListViewModel
{
    public IReadOnlyList<SystemFunctionRowViewModel> Functions { get; set; }
        = Array.Empty<SystemFunctionRowViewModel>();
}

public sealed class SystemFunctionRowViewModel
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string PermissionSummary { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSystemFunction { get; set; }
    public int SortOrder { get; set; }
}

public sealed class SystemFunctionEditViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Mã chức năng")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Tên chức năng")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Chức năng cha")]
    public Guid? ParentId { get; set; }

    [Required, StringLength(50)]
    public string Area { get; set; } = "Admin";

    [StringLength(100)]
    public string? Controller { get; set; }

    [StringLength(100)]
    public string? Action { get; set; } = "Index";

    [StringLength(50)]
    [Display(Name = "Biểu tượng")]
    public string IconKey { get; set; } = "grid";

    [StringLength(100)]
    [Display(Name = "Mã chức năng cơ sở bắt buộc")]
    public string? RequiredBranchFeatureCode { get; set; }

    [Display(Name = "Thứ tự")]
    public int SortOrder { get; set; }

    [Display(Name = "Bắt buộc chọn cơ sở")]
    public bool RequiresBranchSelection { get; set; }

    [Display(Name = "Hiển thị trên menu")]
    public bool IsMenuItem { get; set; } = true;

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public bool IsSystemFunction { get; set; }

    [Display(Name = "Xem")]
    public bool SupportsView { get; set; } = true;

    [Display(Name = "Thêm")]
    public bool SupportsCreate { get; set; }

    [Display(Name = "Sửa")]
    public bool SupportsEdit { get; set; }

    [Display(Name = "Xóa")]
    public bool SupportsDelete { get; set; }

    [Display(Name = "Xuất")]
    public bool SupportsExport { get; set; }

    public IReadOnlyList<SystemFunction> ParentOptions { get; set; }
        = Array.Empty<SystemFunction>();
}

public sealed class SystemRoleIndexViewModel
{
    public IReadOnlyList<SystemRoleListItemDto> Roles { get; set; }
        = Array.Empty<SystemRoleListItemDto>();
}

public sealed class SystemRoleEditViewModel
{
    public string? RoleId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Tên role kỹ thuật")]
    public string RoleName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Tên hiển thị")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public bool IsSystemRole { get; set; }
}

public sealed class SystemPermissionMatrixViewModel
{
    [Required]
    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public bool IsAdminRole { get; set; }
    public IReadOnlyList<SystemRoleListItemDto> AvailableRoles { get; set; }
        = Array.Empty<SystemRoleListItemDto>();
    public List<SystemPermissionMatrixItemDto> Permissions { get; set; } = new();
}

public sealed class CreateSystemUserViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(50)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "Mật khẩu ban đầu")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<string> SelectedRoleIds { get; set; } = new();
    public IReadOnlyList<SystemRoleListItemDto> AvailableRoles { get; set; }
        = Array.Empty<SystemRoleListItemDto>();
}

public sealed class EditSystemUserViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(50)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    public bool IsActive { get; set; }
    public List<string> SelectedRoleIds { get; set; } = new();
    public IReadOnlyList<SystemRoleListItemDto> AvailableRoles { get; set; }
        = Array.Empty<SystemRoleListItemDto>();
}

public sealed class ResetSystemUserPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string UserDisplay { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class AdminNavigationViewModel
{
    public string CurrentController { get; set; } = string.Empty;
    public IReadOnlyList<SystemNavigationItemDto> Items { get; set; }
        = Array.Empty<SystemNavigationItemDto>();
}
