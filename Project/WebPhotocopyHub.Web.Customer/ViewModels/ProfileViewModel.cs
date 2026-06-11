using System.ComponentModel.DataAnnotations;

namespace WebPhotocopyHub.Web.Customer.Models;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 100 ký tự")]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Tên đăng nhập chỉ dùng chữ, số, dấu chấm, gạch dưới hoặc gạch ngang")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Gmail/Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Gmail")]
    public string Gmail => Email;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(200)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Ngày tạo tài khoản")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; }
}