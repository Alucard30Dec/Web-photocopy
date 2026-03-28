using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Web.Models;

public class CreateSupportOrderViewModel
{
    [Required]
    [Display(Name = "Dịch vụ hỗ trợ")]
    public Guid SupportServiceId { get; set; }

    [Range(1, 10000, ErrorMessage = "Số lượng phải lớn hơn 0")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");

    public List<SupportService> AvailableServices { get; set; } = new();
}
