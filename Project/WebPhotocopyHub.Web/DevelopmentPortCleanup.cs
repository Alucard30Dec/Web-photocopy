using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WebPhotocopyHub.Web;

internal static class DevelopmentPortCleanup
{
    private static readonly Regex NetstatWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static void StopStaleConfiguredPortOwners(WebApplication app)
    {
        if (!app.Configuration.GetValue("DevelopmentPortCleanup:Enabled", true))
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var ports = ResolveConfiguredPorts(app.Configuration);
        if (ports.Count == 0)
        {
            return;
        }

        var netstatOutput = ReadNetstatOutput(app.Logger);
        if (string.IsNullOrWhiteSpace(netstatOutput))
        {
            return;
        }

        var currentProcessId = Environment.ProcessId;
        var stopTimeoutMs = app.Configuration.GetValue("DevelopmentPortCleanup:StopTimeoutMs", 2500);
        var owners = FindListeningOwners(netstatOutput, ports);

        foreach (var owner in owners)
        {
            if (owner.ProcessId == currentProcessId)
            {
                continue;
            }

            StopDevelopmentHost(owner.ProcessId, owner.Port, stopTimeoutMs, app.Logger);
        }
    }

    private static HashSet<int> ResolveConfiguredPorts(IConfiguration configuration)
    {
        var ports = new HashSet<int>();

        AddPorts(configuration["urls"], ports);
        AddPorts(configuration["ASPNETCORE_URLS"], ports);
        AddPorts(configuration["DOTNET_URLS"], ports);
        AddPorts(configuration["BrowserLaunch:BaseUrl"], ports);

        foreach (var endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
        {
            AddPorts(endpoint["Url"], ports);
        }

        return ports;
    }

    private static void AddPorts(string? urlList, ISet<int> ports)
    {
        if (string.IsNullOrWhiteSpace(urlList))
        {
            return;
        }

        foreach (var url in urlList.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                ports.Add(uri.Port);
            }
        }
    }

    private static string ReadNetstatOutput(ILogger logger)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "netstat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.ArgumentList.Add("-ano");
            process.StartInfo.ArgumentList.Add("-p");
            process.StartInfo.ArgumentList.Add("tcp");

            if (!process.Start())
            {
                return string.Empty;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(3000))
            {
                process.Kill(entireProcessTree: true);
                return string.Empty;
            }

            return outputTask.GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            logger.LogDebug(ex, "Unable to inspect TCP ports before starting the development server.");
            return string.Empty;
        }
    }

    private static HashSet<PortOwner> FindListeningOwners(string netstatOutput, ISet<int> configuredPorts)
    {
        var owners = new HashSet<PortOwner>();

        foreach (var line in netstatOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = NetstatWhitespaceRegex.Split(trimmedLine);
            if (parts.Length < 5)
            {
                continue;
            }

            var localAddress = parts[1];
            var state = parts[3];
            var processIdText = parts[^1];

            if (!state.Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var port = TryExtractPort(localAddress);
            if (port is null || !configuredPorts.Contains(port.Value))
            {
                continue;
            }

            if (int.TryParse(processIdText, out var processId))
            {
                owners.Add(new PortOwner(port.Value, processId));
            }
        }

        return owners;
    }

    private static int? TryExtractPort(string localAddress)
    {
        var lastColonIndex = localAddress.LastIndexOf(':');
        if (lastColonIndex < 0 || lastColonIndex == localAddress.Length - 1)
        {
            return null;
        }

        return int.TryParse(localAddress[(lastColonIndex + 1)..], out var port)
            ? port
            : null;
    }

    private static void StopDevelopmentHost(int processId, int port, int stopTimeoutMs, ILogger logger)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!IsDevelopmentHost(process))
            {
                logger.LogInformation(
                    "Port {Port} is already in use by process {ProcessName} ({ProcessId}); cleanup skipped.",
                    port,
                    process.ProcessName,
                    processId);
                return;
            }

            logger.LogInformation(
                "Stopping stale development host {ProcessName} ({ProcessId}) that is holding port {Port}.",
                process.ProcessName,
                processId,
                port);

            if (TryCloseMainWindow(process, stopTimeoutMs))
            {
                return;
            }

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(stopTimeoutMs);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            logger.LogDebug(ex, "Unable to stop stale development host {ProcessId} for port {Port}.", processId, port);
        }
    }

    private static bool IsDevelopmentHost(Process process)
    {
        var processName = process.ProcessName;
        return processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("WebPhotocopyHub", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCloseMainWindow(Process process, int stopTimeoutMs)
    {
        try
        {
            if (process.CloseMainWindow())
            {
                return process.WaitForExit(Math.Min(stopTimeoutMs, 1000));
            }
        }
        catch (InvalidOperationException)
        {
        }

        return false;
    }

    private readonly record struct PortOwner(int Port, int ProcessId);
}
