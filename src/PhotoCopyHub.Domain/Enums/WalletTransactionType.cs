using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum WalletTransactionType
{
    [Display(Name = "Nạp tiền chờ duyệt")]
    TopUpPending = 1,

    [Display(Name = "Nạp tiền thành công")]
    TopUpApproved = 2,

    [Display(Name = "Nạp tiền bị từ chối")]
    TopUpRejected = 3,

    [Display(Name = "Trừ tiền đơn hàng")]
    DebitForOrder = 4,

    [Display(Name = "Hoàn tiền")]
    Refund = 5,

    [Display(Name = "Điều chỉnh thủ công")]
    ManualAdjustment = 6
}
