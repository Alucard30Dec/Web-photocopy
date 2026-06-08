using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum PrintJobStatus
{
    [Display(Name = "Nháp")]
    Draft = 1,

    [Display(Name = "Đã gửi")]
    Submitted = 2,

    [Display(Name = "Đã thanh toán")]
    Paid = 3,

    [Display(Name = "Đang in")]
    Processing = 4,

    [Display(Name = "Sẵn sàng nhận")]
    ReadyForPickup = 5,

    [Display(Name = "Đang giao")]
    Shipping = 6,

    [Display(Name = "Hoàn tất")]
    Completed = 7,

    [Display(Name = "Đã hủy")]
    Cancelled = 8,

    [Display(Name = "Đã hoàn tiền")]
    Refunded = 9,

    [Display(Name = "Đã xác nhận file")]
    ConfirmedByShop = 10
}
