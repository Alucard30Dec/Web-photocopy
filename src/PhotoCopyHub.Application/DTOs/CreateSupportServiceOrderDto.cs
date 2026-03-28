namespace PhotoCopyHub.Application.DTOs;

public class CreateSupportServiceOrderDto
{
    public string UserId { get; set; } = string.Empty;
    public Guid SupportServiceId { get; set; }
    public string? IdempotencyKey { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
