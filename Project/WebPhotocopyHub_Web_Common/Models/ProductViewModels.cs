using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Web.Models;

public class ProductOrderItemInputViewModel
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; }
}

public class ProductCatalogViewModel
{
    public List<ProductOrderItemInputViewModel> Items { get; set; } = new();

    [Display(Name = "Hình thức nhận")]
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.PickupAtStore;

    [StringLength(500)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string? DeliveryAddress { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public class ProductOrderHistoryViewModel
{
    public List<ProductOrder> Orders { get; set; } = new();
}
