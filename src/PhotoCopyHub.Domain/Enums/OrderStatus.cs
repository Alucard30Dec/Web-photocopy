using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum OrderStatus
{
    [Display(Name = "Đã gửi")]
    Submitted = 1,

    [Display(Name = "Đang xử lý")]
    Processing = 2,

    [Display(Name = "Hoàn tất")]
    Completed = 3,

    [Display(Name = "Đã hủy")]
    Cancelled = 4,

    [Display(Name = "Đã hoàn tiền")]
    Refunded = 5
}
