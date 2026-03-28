namespace PhotoCopyHub.Application.DTOs;

public class CreateUploadedFileDto
{
    public string OwnerUserId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public Stream Content { get; set; } = Stream.Null;
    public bool IsForPrintJob { get; set; }
}
