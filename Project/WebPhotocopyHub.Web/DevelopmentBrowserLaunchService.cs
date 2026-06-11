using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

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
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _applicationLifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(OpenBrowserWindowsAsync, CancellationToken.None);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
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
            OpenUrls(urls, FindChromePath());
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

        return "https://localhost:7250";
    }

    private static void OpenUrls(IReadOnlyList<string> urls, string? chromePath)
    {
        if (!string.IsNullOrWhiteSpace(chromePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = chromePath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--new-window");

            foreach (var url in urls)
            {
                startInfo.ArgumentList.Add(url);
            }

            Process.Start(startInfo);
            return;
        }

        foreach (var url in urls)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
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