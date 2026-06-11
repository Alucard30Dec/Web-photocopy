using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class ProductOrderItem : BaseEntity
{
    public Guid ProductOrderId { get; set; }
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public ProductOrder? ProductOrder { get; set; }
    public Product? Product { get; set; }
}
