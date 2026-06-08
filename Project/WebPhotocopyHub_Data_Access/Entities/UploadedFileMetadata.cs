using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Common;

namespace PhotoCopyHub.Domain.Entities;

public class UploadedFileMetadata : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string RelativePath { get; set; } = string.Empty;

    public long Size { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public bool IsForPrintJob { get; set; }

    public ApplicationUser? OwnerUser { get; set; }
    public ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();
}
