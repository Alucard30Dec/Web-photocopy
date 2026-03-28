using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Application.DTOs;

public class CreatePrintJobDto
{
    public string UserId { get; set; } = string.Empty;
    public Guid UploadedFileId { get; set; }
    public PaperSize PaperSize { get; set; }
    public PrintSide PrintSide { get; set; }
    public ColorMode ColorMode { get; set; }
    public bool IsPhoto { get; set; }
    public int Copies { get; set; }
    public int TotalPages { get; set; }
    public string? Notes { get; set; }
    public string? IdempotencyKey { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public string? DeliveryAddress { get; set; }
}
