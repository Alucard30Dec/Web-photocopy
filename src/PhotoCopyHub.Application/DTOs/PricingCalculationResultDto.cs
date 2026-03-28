namespace PhotoCopyHub.Application.DTOs;

public class PricingCalculationResultDto
{
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
}
