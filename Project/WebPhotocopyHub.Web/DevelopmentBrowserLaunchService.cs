using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Win32.SafeHandles;

namespace WebPhotocopyHub.Web;

public sealed class DevelopmentBrowserLaunchService : IHostedService
{
    private static readonly string[] DefaultRelativeUrls =
    {
        "/swagger",
        "/Home",
        "/Shops"
    };

    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IServer _server;
    private readonly ILogger<DevelopmentBrowserLaunchService> _logger;
    private readonly object _browserProcessLock = new();
    private readonly List<Process> _browserProcesses = new();
    private SafeFileHandle? _browserJobHandle;
    private string? _browserUserDataDirectory;

    public DevelopmentBrowserLaunchService(
        IConfiguration configuration,
        IHostApplicationLifetime applicationLifetime,
        IServer server,
        ILogger<DevelopmentBrowserLaunchService> logger)
    {
        _configuration = configuration;
        _applicationLifetime = applicationLifetime;
        _server = server;
        _logger = logger;

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CloseLaunchedBrowsers();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _applicationLifetime.ApplicationStopping.Register(CloseLaunchedBrowsers);
        _applicationLifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(OpenBrowserWindowsAsync, CancellationToken.None);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        CloseLaunchedBrowsers();
        return Task.CompletedTask;
    }

    private async Task OpenBrowserWindowsAsync()
    {
        var delayMs = _configuration.GetValue<int?>("BrowserLaunch:DelayMs") ?? 1200;
        await Task.Delay(delayMs);

        var baseUrl = ResolveBaseUrl();
        var urls = ResolveUrls(baseUrl);

        if (urls.Count == 0)
        {
            _logger.LogWarning("BrowserLaunch is enabled but no URLs were configured.");
            return;
        }

        try
        {
            var launchedProcesses = OpenUrls(urls, FindChromePath());
            TrackLaunchedBrowsers(launchedProcesses);
            _logger.LogInformation("Opened development URLs: {Urls}", string.Join(", ", urls));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open development browser URLs: {Urls}", string.Join(", ", urls));
        }
    }

    private IReadOnlyList<string> ResolveUrls(string baseUrl)
    {
        var configuredUrls = _configuration.GetSection("BrowserLaunch:Urls").Get<string[]>();
        var urls = configuredUrls is { Length: > 0 }
            ? configuredUrls
            : DefaultRelativeUrls;

        var normalizedBaseUrl = baseUrl.TrimEnd('/');

        return urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Select(url =>
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    return url;
                }

                return normalizedBaseUrl + "/" + url.TrimStart('/');
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string ResolveBaseUrl()
    {
        var configuredBaseUrl = _configuration["BrowserLaunch:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.TrimEnd('/');
        }

        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses ?? Array.Empty<string>();

        var httpsLocalhost = addresses.FirstOrDefault(address =>
            address.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(httpsLocalhost))
        {
            return httpsLocalhost.TrimEnd('/');
        }

        var httpsAddress = addresses.FirstOrDefault(address =>
            address.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(httpsAddress))
        {
            return httpsAddress.TrimEnd('/');
        }

        var firstAddress = addresses.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstAddress))
        {
            return firstAddress.TrimEnd('/');
        }

        // Codex 2026-07-04: Keep the browser fallback aligned with the single HTTPS debug launch port.
        return "https://localhost:7260";
    }

    private IReadOnlyList<Process> OpenUrls(IReadOnlyList<string> urls, string? chromePath)
    {
        var launchedProcesses = new List<Process>();

        if (!string.IsNullOrWhiteSpace(chromePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = chromePath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Codex 2026-07-04: Launch a dedicated debug browser profile so the app can close the whole project window when hosting stops.
            if (ShouldCloseBrowserOnStop())
            {
                startInfo.ArgumentList.Add("--user-data-dir=" + GetOrCreateBrowserUserDataDirectory());
                startInfo.ArgumentList.Add("--no-first-run");
                startInfo.ArgumentList.Add("--no-default-browser-check");
                startInfo.ArgumentList.Add("--disable-background-mode");
            }

            startInfo.ArgumentList.Add("--new-window");

            foreach (var url in urls)
            {
                startInfo.ArgumentList.Add(url);
            }

            var process = Process.Start(startInfo);
            if (process is not null)
            {
                launchedProcesses.Add(process);
            }

            return launchedProcesses;
        }

        foreach (var url in urls)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        _logger.LogWarning("Chrome was not found. Development URLs were opened with the default browser and cannot be closed automatically by the app.");
        return launchedProcesses;
    }

    private void TrackLaunchedBrowsers(IReadOnlyList<Process> processes)
    {
        if (!ShouldCloseBrowserOnStop() || processes.Count == 0)
        {
            return;
        }

        foreach (var process in processes)
        {
            AssignToBrowserJob(process);
        }

        lock (_browserProcessLock)
        {
            _browserProcesses.AddRange(processes);
        }
    }

    private void AssignToBrowserJob(Process process)
    {
        if (!OperatingSystem.IsWindows() || !ShouldCloseBrowserOnStop())
        {
            return;
        }

        try
        {
            var jobHandle = GetOrCreateBrowserJob();
            if (!AssignProcessToJobObject(jobHandle, process.Handle))
            {
                _logger.LogDebug("Could not assign development browser process {ProcessId} to the cleanup job. Win32 error: {ErrorCode}.",
                    process.Id,
                    Marshal.GetLastWin32Error());
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not assign development browser process {ProcessId} to the cleanup job.", process.Id);
        }
    }

    private SafeFileHandle GetOrCreateBrowserJob()
    {
        lock (_browserProcessLock)
        {
            if (_browserJobHandle is { IsInvalid: false, IsClosed: false })
            {
                return _browserJobHandle;
            }

            _browserJobHandle = CreateJobObject(nint.Zero, null);
            if (_browserJobHandle.IsInvalid)
            {
                throw new InvalidOperationException("Could not create development browser cleanup job.");
            }

            var extendedLimit = new JobObjectExtendedLimitInformation
            {
                BasicLimitInformation = new JobObjectBasicLimitInformation
                {
                    LimitFlags = JobObjectLimitKillOnJobClose
                }
            };

            var length = (uint)Marshal.SizeOf<JobObjectExtendedLimitInformation>();
            if (!SetInformationJobObject(_browserJobHandle, JobObjectExtendedLimitInformationClass, ref extendedLimit, length))
            {
                throw new InvalidOperationException("Could not configure development browser cleanup job.");
            }

            return _browserJobHandle;
        }
    }

    private bool ShouldCloseBrowserOnStop()
    {
        return _configuration.GetValue<bool?>("BrowserLaunch:CloseOnStop") ?? true;
    }

    private string GetOrCreateBrowserUserDataDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_browserUserDataDirectory))
        {
            return _browserUserDataDirectory;
        }

        var directoryName = "WebPhotocopyHub-dev-browser-" + Environment.ProcessId;
        _browserUserDataDirectory = Path.Combine(Path.GetTempPath(), directoryName);
        Directory.CreateDirectory(_browserUserDataDirectory);

        return _browserUserDataDirectory;
    }

    private void CloseLaunchedBrowsers()
    {
        List<Process> processes;
        SafeFileHandle? browserJobHandle;
        string? browserUserDataDirectory;

        lock (_browserProcessLock)
        {
            processes = _browserProcesses.ToList();
            _browserProcesses.Clear();
            browserJobHandle = _browserJobHandle;
            _browserJobHandle = null;
            browserUserDataDirectory = _browserUserDataDirectory;
            _browserUserDataDirectory = null;
        }

        foreach (var process in processes)
        {
            CloseBrowserProcess(process);
        }

        browserJobHandle?.Dispose();
        DeleteBrowserUserDataDirectory(browserUserDataDirectory);
    }

    private void CloseBrowserProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow();

                if (!process.WaitForExit(1500))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not close the development browser process {ProcessId}.", process.Id);
        }
        finally
        {
            process.Dispose();
        }
    }

    private void DeleteBrowserUserDataDirectory(string? browserUserDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(browserUserDataDirectory) || !Directory.Exists(browserUserDataDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(browserUserDataDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not delete development browser profile directory {Directory}.", browserUserDataDirectory);
        }
    }

    private const int JobObjectExtendedLimitInformationClass = 9;
    private const int JobObjectLimitKillOnJobClose = 0x00002000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateJobObject(nint lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        SafeFileHandle hJob,
        int jobObjectInformationClass,
        ref JobObjectExtendedLimitInformation lpJobObjectInformation,
        uint cbJobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle hJob, nint hProcess);

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public int LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public int ActiveProcessLimit;
        public nuint Affinity;
        public int PriorityClass;
        public int SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    private static string? FindChromePath()
    {
        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
