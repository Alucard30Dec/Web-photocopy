using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Application.DTOs;

public class CreateProductOrderDto
{
    public string UserId { get; set; } = string.Empty;
    public List<CreateProductOrderItemDto> Items { get; set; } = new();
    public string? IdempotencyKey { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
}
