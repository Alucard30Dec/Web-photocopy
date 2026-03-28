using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;

namespace PhotoCopyHub.Domain.Entities;

public class Product : BaseEntity, IHasRowVersion
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public ICollection<ProductOrderItem> ProductOrderItems { get; set; } = new List<ProductOrderItem>();
    public ICollection<ProductStockMovement> StockMovements { get; set; } = new List<ProductStockMovement>();
}
