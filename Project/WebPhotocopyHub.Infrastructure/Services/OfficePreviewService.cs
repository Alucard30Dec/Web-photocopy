using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Infrastructure.Options;

namespace WebPhotocopyHub.Infrastructure.Services;

public class OfficePreviewService : IOfficePreviewService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    private readonly OfficePreviewOptions _options;
    private readonly IHostEnvironment _hostEnvironment;

    public OfficePreviewService(
        IOptions<OfficePreviewOptions> options,
        IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
    }

    public bool IsSupportedExtension(string extension)
    {
        return SupportedExtensions.Contains(extension);
    }

    public async Task<OfficePreviewResultDto> ConvertToPdfAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new InvalidDataException("File Office không có dữ liệu.");
        }

        var safeFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

        if (!IsSupportedExtension(extension))
        {
            throw new InvalidDataException("Định dạng Office này chưa được hỗ trợ xem trước.");
        }

        var libreOfficePath = ResolveLibreOfficePath();
        if (string.IsNullOrWhiteSpace(libreOfficePath))
        {
            throw new OfficePreviewUnavailableException(
                "Không tìm thấy LibreOffice trên máy chủ. Hãy cấu hình OfficePreview:LibreOfficePath hoặc đặt LibreOffice portable trong WebPhotocopyHub.Web/LocalTools/LibreOffice.");
        }

        var workRoot = Path.Combine(
            Path.GetTempPath(),
            "WebPhotocopyHub",
            "OfficePreview",
            Guid.NewGuid().ToString("N"));
        var outputDirectory = Path.Combine(workRoot, "output");
        var profileDirectory = Path.Combine(workRoot, "profile");
        var inputPath = Path.Combine(workRoot, "preview" + extension);
        var expectedPdfPath = Path.Combine(outputDirectory, "preview.pdf");

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(profileDirectory);

        try
        {
            await CopyToTempFileAsync(content, inputPath, cancellationToken);
            ValidateOpenXmlContainerWhenRequired(inputPath, extension);

            var startInfo = CreateStartInfo(
                libreOfficePath,
                profileDirectory,
                outputDirectory,
                inputPath,
                ResolvePdfFilter(extension),
                workRoot);

            using var process = new Process { StartInfo = startInfo };

            try
            {
                if (!process.Start())
                {
                    throw new OfficePreviewUnavailableException(
                        "Không thể khởi động LibreOffice để tạo bản xem trước.");
                }
            }
            catch (Win32Exception ex)
            {
                throw new OfficePreviewUnavailableException(
                    "Không thể khởi động LibreOffice để tạo bản xem trước.",
                    ex);
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.ConversionTimeoutSeconds)));

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw new TimeoutException("LibreOffice conversion timed out.");
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            var generatedPdfPath = await WaitForGeneratedPdfAsync(
                outputDirectory,
                expectedPdfPath,
                cancellationToken);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(generatedPdfPath))
            {
                var diagnostic = FirstNonEmptyLine(standardError, standardOutput);
                var engineName = Path.GetFileName(libreOfficePath);

                throw new InvalidDataException(
                    string.IsNullOrWhiteSpace(diagnostic)
                        ? $"LibreOffice ({engineName}) không tạo được PDF. Hãy đóng các cửa sổ LibreOffice đang mở rồi thử lại."
                        : $"Không thể chuyển file Office sang PDF: {diagnostic}");
            }

            var pdfInfo = new FileInfo(generatedPdfPath);
            if (pdfInfo.Length <= 4 || pdfInfo.Length > _options.MaxPreviewPdfSizeBytes)
            {
                throw new InvalidDataException("Bản PDF xem trước không hợp lệ hoặc vượt quá giới hạn cho phép.");
            }

            var pdfBytes = await File.ReadAllBytesAsync(generatedPdfPath, cancellationToken);
            if (pdfBytes.Length < 4 ||
                pdfBytes[0] != (byte)'%' ||
                pdfBytes[1] != (byte)'P' ||
                pdfBytes[2] != (byte)'D' ||
                pdfBytes[3] != (byte)'F')
            {
                throw new InvalidDataException("LibreOffice không tạo được một file PDF hợp lệ.");
            }

            return new OfficePreviewResultDto(pdfBytes);
        }
        finally
        {
            TryDeleteDirectory(workRoot);
        }
    }

    private static async Task CopyToTempFileAsync(
        Stream content,
        string inputPath,
        CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using var destination = new FileStream(
            inputPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await content.CopyToAsync(destination, cancellationToken);
    }

    private static ProcessStartInfo CreateStartInfo(
        string libreOfficePath,
        string profileDirectory,
        string outputDirectory,
        string inputPath,
        string pdfFilter,
        string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = libreOfficePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        startInfo.Environment.Remove("PYTHONHOME");
        startInfo.Environment.Remove("PYTHONPATH");
        startInfo.Environment.Remove("PYTHONSTARTUP");

        var profileUri = new Uri(
            profileDirectory.TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar).AbsoluteUri;

        startInfo.ArgumentList.Add($"-env:UserInstallation={profileUri}");
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--nodefault");
        startInfo.ArgumentList.Add("--nolockcheck");
        startInfo.ArgumentList.Add("--norestore");
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add(pdfFilter);
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(inputPath);

        return startInfo;
    }

    private static string ResolvePdfFilter(string extension)
    {
        return extension switch
        {
            ".doc" or ".docx" => "pdf:writer_pdf_Export",
            ".xls" or ".xlsx" => "pdf:calc_pdf_Export",
            ".ppt" or ".pptx" => "pdf:impress_pdf_Export",
            _ => "pdf"
        };
    }

    private static void ValidateOpenXmlContainerWhenRequired(string inputPath, string extension)
    {
        if (extension is not (".docx" or ".xlsx" or ".pptx"))
        {
            return;
        }

        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> signature = stackalloc byte[4];
        var bytesRead = stream.Read(signature);

        if (bytesRead < 4 ||
            signature[0] != (byte)'P' ||
            signature[1] != (byte)'K')
        {
            throw new InvalidDataException(
                "File Office Open XML không hợp lệ. Hãy mở file bằng Office và lưu lại trước khi tải lên.");
        }
    }

    private async Task<string?> WaitForGeneratedPdfAsync(
        string outputDirectory,
        string expectedPdfPath,
        CancellationToken cancellationToken)
    {
        var timeoutSeconds = Math.Max(1, _options.OutputDiscoveryTimeoutSeconds);
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = File.Exists(expectedPdfPath)
                ? expectedPdfPath
                : Directory
                    .EnumerateFiles(outputDirectory, "*.pdf", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                try
                {
                    var info = new FileInfo(candidate);
                    info.Refresh();
                    if (info.Exists && info.Length > 4)
                    {
                        return candidate;
                    }
                }
                catch (IOException)
                {
                    // LibreOffice can still be flushing the generated file.
                }
            }

            await Task.Delay(150, cancellationToken);
        }

        return null;
    }

    private string? ResolveLibreOfficePath()
    {
        var configuredCandidates = new[]
        {
            _options.LibreOfficePath,
            Environment.GetEnvironmentVariable("WEBPHOTOCOPYHUB_LIBREOFFICE_PATH"),
            Environment.GetEnvironmentVariable("LIBREOFFICE_PATH")
        };

        foreach (var candidate in configuredCandidates
                     .SelectMany(BuildConfiguredCandidates)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        foreach (var candidate in BuildCandidates()
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private IEnumerable<string> BuildConfiguredCandidates(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            yield break;
        }

        var normalizedPath = configuredPath.Trim().Trim('"');

        if (File.Exists(normalizedPath))
        {
            yield return normalizedPath;

            var configuredDirectory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrWhiteSpace(configuredDirectory) && OperatingSystem.IsWindows())
            {
                yield return Path.Combine(configuredDirectory, "soffice.exe");
                yield return Path.Combine(configuredDirectory, "soffice.com");
            }
        }

        if (Directory.Exists(normalizedPath))
        {
            foreach (var candidate in BuildDirectoryCandidates(normalizedPath))
            {
                yield return candidate;
            }

            foreach (var candidate in BuildDirectoryCandidates(Path.Combine(normalizedPath, "program")))
            {
                yield return candidate;
            }
        }
    }

    private IEnumerable<string> BuildCandidates()
    {
        var localToolsPath = ResolveLocalToolsPath();
        foreach (var candidate in BuildConfiguredCandidates(localToolsPath))
        {
            yield return candidate;
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            foreach (var candidate in BuildDirectoryCandidates(
                         Path.Combine(programFiles, "LibreOffice", "program")))
            {
                yield return candidate;
            }
        }

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            foreach (var candidate in BuildDirectoryCandidates(
                         Path.Combine(programFilesX86, "LibreOffice", "program")))
            {
                yield return candidate;
            }
        }

        if (!OperatingSystem.IsWindows())
        {
            yield return "/usr/bin/libreoffice";
            yield return "/usr/bin/soffice";
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var pathPart in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedPart = pathPart.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(normalizedPart))
            {
                continue;
            }

            foreach (var candidate in BuildDirectoryCandidates(normalizedPart))
            {
                yield return candidate;
            }
        }
    }

    private string ResolveLocalToolsPath()
    {
        var localToolsDirectory = string.IsNullOrWhiteSpace(_options.LocalToolsDirectory)
            ? "LocalTools/LibreOffice"
            : _options.LocalToolsDirectory;

        return Path.IsPathRooted(localToolsDirectory)
            ? localToolsDirectory
            : Path.Combine(_hostEnvironment.ContentRootPath, localToolsDirectory);
    }

    private static IEnumerable<string> BuildDirectoryCandidates(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            yield break;
        }

        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(directory, "soffice.exe");
            yield return Path.Combine(directory, "soffice.com");
            yield return Path.Combine(directory, "libreoffice.exe");
            yield break;
        }

        yield return Path.Combine(directory, "soffice");
        yield return Path.Combine(directory, "libreoffice");
    }

    private static string FirstNonEmptyLine(params string[] values)
    {
        foreach (var value in values)
        {
            var line = value
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            if (!string.IsNullOrWhiteSpace(line))
            {
                return line.Length <= 240 ? line : line[..240];
            }
        }

        return string.Empty;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup after timeout.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Temporary preview files are cleaned up best-effort.
        }
    }
}
