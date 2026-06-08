using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;

namespace PhotoCopyHub.Domain.Entities;

public class AuditLog : BaseEntity
{
    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? EntityId { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(128)]
    public string? PreviousHash { get; set; }

    [MaxLength(128)]
    public string? RecordHash { get; set; }
}
