using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class TopUpRequest : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? TransferContent { get; set; }

    [MaxLength(100)]
    public string? TransactionReferenceCode { get; set; }

    [MaxLength(120)]
    public string? CreateIdempotencyKey { get; set; }

    [MaxLength(120)]
    public string? LastReviewIdempotencyKey { get; set; }

    public TopUpChannel Channel { get; set; } = TopUpChannel.BankTransfer;

    public Guid? ProofFileId { get; set; }
    public TopUpStatus Status { get; set; } = TopUpStatus.Pending;
    public bool RequiresAdminApproval { get; set; }

    [MaxLength(450)]
    public string? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewNote { get; set; }

    [MaxLength(450)]
    public string? SecondReviewedByAdminId { get; set; }

    public DateTime? SecondReviewedAt { get; set; }

    [MaxLength(500)]
    public string? SecondReviewNote { get; set; }

    public Guid? ApprovedWalletTransactionId { get; set; }

    public ApplicationUser? User { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
    public ApplicationUser? SecondReviewedByAdmin { get; set; }
    public UploadedFileMetadata? ProofFile { get; set; }
}
