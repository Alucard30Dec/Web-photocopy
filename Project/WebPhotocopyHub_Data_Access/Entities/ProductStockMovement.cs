using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class ProductStockMovement : BaseEntity
{
    public Guid ProductId { get; set; }

    [MaxLength(450)]
    public string ActorUserId { get; set; } = string.Empty;

    public StockMovementType MovementType { get; set; } = StockMovementType.ManualAdjustment;

    public int QuantityChanged { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public Product? Product { get; set; }
    public ApplicationUser? ActorUser { get; set; }
}
