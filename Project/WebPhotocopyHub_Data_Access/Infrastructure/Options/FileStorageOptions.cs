namespace PhotoCopyHub.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "App_Data/uploads";
    public long MaxFileSizeMb { get; set; } = 20;

    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx"
    };

    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    public S3StorageOptions S3 { get; set; } = new();

    public bool UseS3 =>
        Provider.Equals("S3", StringComparison.OrdinalIgnoreCase) ||
        Provider.Equals("R2", StringComparison.OrdinalIgnoreCase) ||
        Provider.Equals("ObjectStorage", StringComparison.OrdinalIgnoreCase);
}

public class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-southeast-1";
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "uploads";
    public bool ForcePathStyle { get; set; } = true;
    public bool UseHttp { get; set; } = false;
}