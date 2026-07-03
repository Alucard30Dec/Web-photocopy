using System.ComponentModel.DataAnnotations;

namespace WebPhotocopyHub.Web.Models;

public class ExternalLoginConfirmationViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email / Gmail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên hiển thị tối đa 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số, gạch ngang và gạch dưới.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    public string? ProviderDisplayName { get; set; }
    
    public string? ReturnUrl { get; set; }
}
