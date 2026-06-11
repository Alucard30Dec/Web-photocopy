using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IFileStorageService
{
    Task<UploadedFileMetadata> SaveAsync(CreateUploadedFileDto request, CancellationToken cancellationToken = default);
    Task<UploadedFileMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UploadedFileMetadata>> GetFilesByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(Guid id, CancellationToken cancellationToken = default);
    int? TryGetPdfPageCount(Stream stream);
}
