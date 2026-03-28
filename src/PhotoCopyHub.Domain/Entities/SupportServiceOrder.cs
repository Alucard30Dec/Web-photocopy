using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class SupportServiceOrder : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public Guid SupportServiceId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(120)]
    public string? OrderIdempotencyKey { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Submitted;

    [MaxLength(450)]
    public string? ProcessedByOperatorId { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(500)]
    public string? ProcessNote { get; set; }

    public ApplicationUser? User { get; set; }
    public ApplicationUser? ProcessedByOperator { get; set; }
    public SupportService? SupportService { get; set; }
}
