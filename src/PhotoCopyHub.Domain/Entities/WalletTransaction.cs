using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class WalletTransaction : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public WalletTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    [MaxLength(100)]
    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [MaxLength(120)]
    public string? IdempotencyKey { get; set; }

    [MaxLength(450)]
    public string? PerformedByAdminId { get; set; }

    public ApplicationUser? User { get; set; }
    public ApplicationUser? PerformedByAdmin { get; set; }
}
