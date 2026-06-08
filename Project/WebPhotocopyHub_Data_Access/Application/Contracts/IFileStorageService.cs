using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Application.Contracts;

public interface IFileStorageService
{
    Task<UploadedFileMetadata> SaveAsync(CreateUploadedFileDto request, CancellationToken cancellationToken = default);
    Task<UploadedFileMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UploadedFileMetadata>> GetFilesByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(Guid id, CancellationToken cancellationToken = default);
    int? TryGetPdfPageCount(Stream stream);
}
