using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharpCore.Pdf.IO;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Infrastructure.Data;
using WebPhotocopyHub.Infrastructure.Options;

namespace WebPhotocopyHub.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly FileStorageOptions _options;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _root;

    public FileStorageService(
        ApplicationDbContext dbContext,
        IOptions<FileStorageOptions> options,
        IHostEnvironment hostEnvironment,
        IAmazonS3 s3Client,
        ILogger<FileStorageService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _s3Client = s3Client;
        _logger = logger;

        _root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(hostEnvironment.ContentRootPath, _options.RootPath);

        if (!_options.UseS3)
        {
            Directory.CreateDirectory(_root);
        }
    }

    public async Task<UploadedFileMetadata> SaveAsync(CreateUploadedFileDto request, CancellationToken cancellationToken = default)
    {
        if (!request.Content.CanSeek)
        {
            var bufferedStream = new MemoryStream();
            await request.Content.CopyToAsync(bufferedStream, cancellationToken);
            bufferedStream.Position = 0;
            request.Content = bufferedStream;
            request.Size = bufferedStream.Length;
        }

        ValidateUpload(request);

        var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var subDir = DateTime.UtcNow.ToString("yyyy/MM");
        var relativePath = Path.Combine(subDir, fileName).Replace('\\', '/');

        if (_options.UseS3)
        {
            await SaveToS3Async(request, relativePath, cancellationToken);
        }
        else
        {
            await SaveToLocalDiskAsync(request, fileName, subDir, cancellationToken);
        }

        var metadata = new UploadedFileMetadata
        {
            OwnerUserId = request.OwnerUserId,
            OriginalFileName = request.OriginalFileName,
            StoredFileName = fileName,
            RelativePath = relativePath,
            ContentType = request.ContentType,
            Size = request.Size,
            IsForPrintJob = request.IsForPrintJob
        };

        _dbContext.UploadedFileMetadatas.Add(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Saved uploaded file. UploadedFileId={UploadedFileId} UserId={UserId} Provider={Provider} Size={Size} RelativePath={RelativePath}",
            metadata.Id,
            metadata.OwnerUserId,
            _options.Provider,
            metadata.Size,
            metadata.RelativePath);

        return metadata;
    }

    public Task<UploadedFileMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadedFileMetadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<UploadedFileMetadata>> GetFilesByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadedFileMetadatas
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(id, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy file.");

        if (_options.UseS3)
        {
            return await OpenReadFromS3Async(metadata, cancellationToken);
        }

        var fullPath = Path.Combine(_root, metadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            throw new BusinessException("File vật lý không tồn tại.");
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public int? TryGetPdfPageCount(Stream stream)
    {
        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using var doc = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            return doc.PageCount;
        }
        catch
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            return null;
        }
    }

    private async Task SaveToLocalDiskAsync(
        CreateUploadedFileDto request,
        string fileName,
        string subDir,
        CancellationToken cancellationToken)
    {
        var dirPath = Path.Combine(_root, subDir);
        Directory.CreateDirectory(dirPath);

        var fullPath = Path.Combine(dirPath, fileName);
        await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        await request.Content.CopyToAsync(fs, cancellationToken);
    }

    private async Task SaveToS3Async(
        CreateUploadedFileDto request,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var bucketName = _options.S3.BucketName;
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("Thiếu FileStorage:S3:BucketName khi FileStorage:Provider=S3.");
        }

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        var key = BuildS3Key(relativePath);

        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false
        };

        putRequest.Metadata["original-file-name"] = request.OriginalFileName;
        putRequest.Metadata["owner-user-id"] = request.OwnerUserId;
        putRequest.Metadata["is-for-print-job"] = request.IsForPrintJob.ToString();

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);

        _logger.LogInformation(
            "Uploaded file object to S3-compatible storage. Bucket={BucketName} Key={ObjectKey} Size={Size}",
            bucketName,
            key,
            request.Size);
    }

    private async Task<Stream> OpenReadFromS3Async(
        UploadedFileMetadata metadata,
        CancellationToken cancellationToken)
    {
        var bucketName = _options.S3.BucketName;
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("Thiếu FileStorage:S3:BucketName khi FileStorage:Provider=S3.");
        }

        var key = BuildS3Key(metadata.RelativePath);

        try
        {
            using var response = await _s3Client.GetObjectAsync(bucketName, key, cancellationToken);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new BusinessException("File object không tồn tại trong storage.");
        }
    }

    private string BuildS3Key(string relativePath)
    {
        var normalizedRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
        var prefix = (_options.S3.KeyPrefix ?? string.Empty).Replace('\\', '/').Trim('/');

        return string.IsNullOrWhiteSpace(prefix)
            ? normalizedRelativePath
            : $"{prefix}/{normalizedRelativePath}";
    }

    private void ValidateUpload(CreateUploadedFileDto request)
    {
        if (request.Content is null || request.Size <= 0)
        {
            throw new BusinessException("File upload không hợp lệ.");
        }

        if (request.Size > _options.MaxFileSizeMb * 1024 * 1024)
        {
            throw new BusinessException($"File vượt quá giới hạn {_options.MaxFileSizeMb}MB.");
        }

        var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !_options.AllowedExtensions.Contains(extension))
        {
            throw new BusinessException("Định dạng file không được hỗ trợ.");
        }

        if (!HasValidMagicNumber(request.Content, extension))
        {
            throw new BusinessException("Nội dung file không khớp định dạng khai báo.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new BusinessException("Không xác định được MIME type của file.");
        }

        var contentType = request.ContentType.ToLowerInvariant();
        if (!_options.AllowedMimeTypes.Contains(contentType))
        {
            throw new BusinessException("MIME type không hợp lệ.");
        }

        if (extension is ".exe" or ".bat" or ".cmd" or ".ps1" or ".sh" or ".msi")
        {
            throw new BusinessException("Không cho phép upload file thực thi.");
        }
    }

    private static bool HasValidMagicNumber(Stream stream, string extension)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        var currentPosition = stream.Position;
        try
        {
            stream.Position = 0;
            Span<byte> header = stackalloc byte[16];
            var bytesRead = stream.Read(header);
            if (bytesRead <= 0)
            {
                return false;
            }

            return extension switch
            {
                ".pdf" => StartsWith(header, bytesRead, 0x25, 0x50, 0x44, 0x46, 0x2D),
                ".jpg" or ".jpeg" => StartsWith(header, bytesRead, 0xFF, 0xD8, 0xFF),
                ".png" => StartsWith(header, bytesRead, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
                ".doc" or ".xls" or ".ppt" => StartsWith(header, bytesRead, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1),
                ".docx" or ".xlsx" or ".pptx" => StartsWith(header, bytesRead, 0x50, 0x4B, 0x03, 0x04) ||
                                                 StartsWith(header, bytesRead, 0x50, 0x4B, 0x05, 0x06) ||
                                                 StartsWith(header, bytesRead, 0x50, 0x4B, 0x07, 0x08),
                _ => false
            };
        }
        finally
        {
            stream.Position = currentPosition;
        }
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, int bytesRead, params byte[] signature)
    {
        if (bytesRead < signature.Length)
        {
            return false;
        }

        for (var i = 0; i < signature.Length; i++)
        {
            if (bytes[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}