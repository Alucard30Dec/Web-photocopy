using WebPhotocopyHub.Domain.Common;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Domain.Entities;

public class PricingRule : BaseEntity, IBranchScopedEntity
{
    public Guid BranchId { get; set; }

    public PaperSize PaperSize { get; set; }
    public PrintSide PrintSide { get; set; }
    public ColorMode ColorMode { get; set; }
    public bool IsPhoto { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public Branch? Branch { get; set; }
}
