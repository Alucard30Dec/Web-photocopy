namespace PhotoCopyHub.Application.DTOs;

public class AdjustProductStockDto
{
    public Guid ProductId { get; set; }
    public int QuantityDelta { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string? Note { get; set; }
}
