using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Web.Shop.Models;

public class ReviewTopUpViewModel
{
    public Guid TopUpRequestId { get; set; }
    public bool IsApprove { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public class UpdatePrintJobStatusViewModel
{
    public Guid PrintJobId { get; set; }

    [Required]
    public PrintJobStatus Status { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}