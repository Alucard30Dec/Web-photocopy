using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Web.Models;

public class CreatePrintJobViewModel
{
    [Display(Name = "Chọn file đã upload")]
    public Guid? ExistingFileId { get; set; }

    [Display(Name = "Hoặc upload file mới")]
    public IFormFile? UploadFile { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khổ giấy")]
    [Display(Name = "Khổ giấy")]
    public PaperSize PaperSize { get; set; } = PaperSize.A4;

    [Required(ErrorMessage = "Vui lòng chọn kiểu in")]
    [Display(Name = "In 1 mặt / 2 mặt")]
    public PrintSide PrintSide { get; set; } = PrintSide.OneSide;

    [Required(ErrorMessage = "Vui lòng chọn chế độ màu")]
    [Display(Name = "Màu sắc")]
    public ColorMode ColorMode { get; set; } = ColorMode.BlackWhite;

    [Display(Name = "In ảnh")]
    public bool IsPhoto { get; set; }

    [Range(1, 1000, ErrorMessage = "Số bản in phải từ 1 đến 1000")]
    [Display(Name = "Số bản in")]
    public int Copies { get; set; } = 1;

    [Range(1, 10000, ErrorMessage = "Số trang phải từ 1 đến 10000")]
    [Display(Name = "Số trang")]
    public int? TotalPages { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [Required]
    [Display(Name = "Hình thức nhận")]
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.PickupAtStore;

    [StringLength(500)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string? DeliveryAddress { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");

    public List<UploadedFileMetadata> ExistingFiles { get; set; } = new();
}
