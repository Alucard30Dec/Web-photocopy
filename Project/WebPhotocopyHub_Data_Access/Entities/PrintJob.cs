using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class PrintJob : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public Guid UploadedFileId { get; set; }

    public PaperSize PaperSize { get; set; }
    public PrintSide PrintSide { get; set; }
    public ColorMode ColorMode { get; set; }
    public bool IsPhoto { get; set; }
    public int Copies { get; set; }
    public int TotalPages { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DeliveryMethod DeliveryMethod { get; set; }

    [MaxLength(500)]
    public string? DeliveryAddress { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }

    public PrintJobStatus Status { get; set; } = PrintJobStatus.Submitted;

    [MaxLength(450)]
    public string? ConfirmedByOperatorId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    [MaxLength(450)]
    public string? AssignedOperatorId { get; set; }

    [MaxLength(500)]
    public string? LastStatusNote { get; set; }

    public DateTime? PaidAt { get; set; }
    public Guid? PaidWalletTransactionId { get; set; }

    [MaxLength(120)]
    public string? SubmitIdempotencyKey { get; set; }

    [MaxLength(450)]
    public string? ProcessedByAdminId { get; set; }

    [MaxLength(450)]
    public string? RefundedByUserId { get; set; }

    public DateTime? RefundedAt { get; set; }

    [MaxLength(500)]
    public string? RefundReason { get; set; }

    public ApplicationUser? User { get; set; }
    public ApplicationUser? ConfirmedByOperator { get; set; }
    public ApplicationUser? AssignedOperator { get; set; }
    public ApplicationUser? ProcessedByAdmin { get; set; }
    public ApplicationUser? RefundedByUser { get; set; }
    public UploadedFileMetadata? UploadedFile { get; set; }
}
