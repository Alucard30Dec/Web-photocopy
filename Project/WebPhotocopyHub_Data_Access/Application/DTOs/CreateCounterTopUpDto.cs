namespace PhotoCopyHub.Application.DTOs;

public class CreateCounterTopUpDto
{
    public string TargetUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OperatorUserId { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }
}
