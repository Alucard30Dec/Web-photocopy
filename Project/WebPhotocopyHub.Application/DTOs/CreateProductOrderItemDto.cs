namespace WebPhotocopyHub.Application.DTOs;

public class CreateProductOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
