using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum TopUpChannel
{
    [Display(Name = "Chuyển khoản")]
    BankTransfer = 1,

    [Display(Name = "Tiền mặt tại quầy")]
    CounterCash = 2
}
