using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Application.DTOs;

public class PricingCalculationRequestDto
{
    public PaperSize PaperSize { get; set; }
    public PrintSide PrintSide { get; set; }
    public ColorMode ColorMode { get; set; }
    public bool IsPhoto { get; set; }
    public int Copies { get; set; }
    public int TotalPages { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public decimal ShippingFee { get; set; }
}
