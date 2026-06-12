namespace WebPhotocopyHub.Infrastructure.Options;

public class OfficePreviewOptions
{
    public const string SectionName = "OfficePreview";

    public string LibreOfficePath { get; set; } = string.Empty;
    public string LocalToolsDirectory { get; set; } = "LocalTools/LibreOffice";
    public int ConversionTimeoutSeconds { get; set; } = 75;
    public int OutputDiscoveryTimeoutSeconds { get; set; } = 12;
    public long MaxPreviewPdfSizeMb { get; set; } = 60;

    public long MaxPreviewPdfSizeBytes => Math.Max(1, MaxPreviewPdfSizeMb) * 1024L * 1024L;
}
