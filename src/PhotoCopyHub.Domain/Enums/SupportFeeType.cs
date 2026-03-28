using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum SupportFeeType
{
    [Display(Name = "Phí cố định")]
    Fixed = 1,

    [Display(Name = "Tính theo số lượng")]
    PerQuantity = 2
}
