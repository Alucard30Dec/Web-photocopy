using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Domain.Entities;

public class SupportService : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }
    public SupportFeeType FeeType { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SupportServiceOrder> Orders { get; set; } = new List<SupportServiceOrder>();
}
