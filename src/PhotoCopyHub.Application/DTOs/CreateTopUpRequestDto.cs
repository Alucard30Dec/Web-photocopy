namespace PhotoCopyHub.Application.DTOs;

public class CreateTopUpRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransferContent { get; set; }
    public string? TransactionReferenceCode { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? ProofFileId { get; set; }
}
