using WebPhotocopyHub.Application.DTOs;

namespace WebPhotocopyHub.Application.Contracts;

public interface IOfficePreviewService
{
    bool IsSupportedExtension(string extension);

    Task<OfficePreviewResultDto> ConvertToPdfAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
