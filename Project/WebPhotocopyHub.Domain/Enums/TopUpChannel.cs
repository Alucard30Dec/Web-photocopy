using System.ComponentModel.DataAnnotations;

namespace WebPhotocopyHub.Domain.Enums;

public enum TopUpChannel
{
    [Display(Name = "Chuyển khoản")]
    BankTransfer = 1,

    [Display(Name = "Tiền mặt tại quầy")]
    CounterCash = 2
}
