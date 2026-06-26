using System.ComponentModel.DataAnnotations;

namespace WebPhotocopyHub.Domain.Entities;

public class BranchFeature
{
    public Guid BranchId { get; set; }

    [Required, MaxLength(100)]
    public string FeatureCode { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? UpdatedByUserId { get; set; }

    public Branch? Branch { get; set; }
}
