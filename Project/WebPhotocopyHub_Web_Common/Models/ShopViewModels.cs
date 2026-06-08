using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Web.Models;

public class CounterTopUpViewModel
{
    [Required]
    [Display(Name = "Khách hàng")]
    public string TargetUserId { get; set; } = string.Empty;

    [Required]
    [Range(1000, 100000000, ErrorMessage = "Số tiền phải từ 1.000 đến 100.000.000")]
    [Display(Name = "Số tiền nạp")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");

    public List<SelectListItem> CustomerOptions { get; set; } = new();
}

public class ShopInventoryViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<ProductStockMovement> RecentMovements { get; set; } = new();
    public AdjustStockViewModel Form { get; set; } = new();
}

public class AdjustStockViewModel
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(-100000, 100000, ErrorMessage = "Giá trị điều chỉnh không hợp lệ")]
    [Display(Name = "Số lượng điều chỉnh (+/-)")]
    public int QuantityDelta { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Lý do")]
    public string Note { get; set; } = string.Empty;
}

public class UpdateShopOrderStatusViewModel
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public OrderStatus Status { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}
