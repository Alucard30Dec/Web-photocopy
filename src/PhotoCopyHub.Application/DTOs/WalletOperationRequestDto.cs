using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Application.DTOs;

public class WalletOperationRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletTransactionType TransactionType { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? PerformedByAdminId { get; set; }
}
