using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum StockMovementType
{
    [Display(Name = "Điều chỉnh thủ công")]
    ManualAdjustment = 1,

    [Display(Name = "Trừ theo đơn hàng")]
    OrderDeduction = 2,

    [Display(Name = "Nhập thêm")]
    Restock = 3
}
