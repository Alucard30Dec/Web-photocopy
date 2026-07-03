using System.ComponentModel.DataAnnotations;

namespace WebPhotocopyHub.Web.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Email/Gmail.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;
}
