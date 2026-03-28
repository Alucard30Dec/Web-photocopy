using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class PricingRule : BaseEntity
{
    public PaperSize PaperSize { get; set; }
    public PrintSide PrintSide { get; set; }
    public ColorMode ColorMode { get; set; }
    public bool IsPhoto { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
