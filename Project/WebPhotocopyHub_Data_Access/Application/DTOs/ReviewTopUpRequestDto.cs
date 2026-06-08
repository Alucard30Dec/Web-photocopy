namespace PhotoCopyHub.Application.DTOs;

public class ReviewTopUpRequestDto
{
    public Guid TopUpRequestId { get; set; }
    public string ReviewerUserId { get; set; } = string.Empty;
    public bool IsAdminReviewer { get; set; }
    public bool IsApprove { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Note { get; set; }
}
