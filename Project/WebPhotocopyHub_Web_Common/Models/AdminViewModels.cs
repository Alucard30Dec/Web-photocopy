using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Web.Models;

public class ManualAdjustBalanceViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Số tiền điều chỉnh (âm hoặc dương)")]
    [Range(-100000000, 100000000)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Lý do")]
    public string Note { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public class ReviewTopUpViewModel
{
    public Guid TopUpRequestId { get; set; }
    public bool IsApprove { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public class UpdatePrintJobStatusViewModel
{
    public Guid PrintJobId { get; set; }

    [Required]
    public PrintJobStatus Status { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public class EditProductViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Tồn kho")]
    public int StockQuantity { get; set; }

    [StringLength(500)]
    [Display(Name = "Link hình ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Đang kinh doanh")]
    public bool IsActive { get; set; } = true;
}

public class EditSupportServiceViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Tên dịch vụ")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Đơn giá")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Kiểu tính phí")]
    public SupportFeeType FeeType { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class EditPricingRuleViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Khổ giấy")]
    public PaperSize PaperSize { get; set; }

    [Required]
    [Display(Name = "Kiểu in")]
    public PrintSide PrintSide { get; set; }

    [Required]
    [Display(Name = "Màu")]
    public ColorMode ColorMode { get; set; }

    [Display(Name = "In ảnh")]
    public bool IsPhoto { get; set; }

    [Required]
    [Range(0, 1000000)]
    [Display(Name = "Đơn giá")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;
}
