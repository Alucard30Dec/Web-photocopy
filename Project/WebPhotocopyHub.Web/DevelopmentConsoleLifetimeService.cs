using System.Runtime.InteropServices;

namespace WebPhotocopyHub.Web;

public sealed class DevelopmentConsoleLifetimeService : IHostedService, IDisposable
{
    private const int CtrlCloseEvent = 2;
    private const int CtrlLogoffEvent = 5;
    private const int CtrlShutdownEvent = 6;

    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<DevelopmentConsoleLifetimeService> _logger;
    private readonly ManualResetEventSlim _applicationStopped = new(false);
    private CancellationTokenRegistration _applicationStoppedRegistration;
    private CancellationTokenSource? _consoleWindowWatcherCancellation;
    private ConsoleControlHandler? _consoleControlHandler;
    private Task? _consoleWindowWatcherTask;
    private nint _consoleWindowHandle;
    private int _shutdownRequested;

    public DevelopmentConsoleLifetimeService(
        IConfiguration configuration,
        IHostApplicationLifetime applicationLifetime,
        ILogger<DevelopmentConsoleLifetimeService> logger)
    {
        _configuration = configuration;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows() || !ShouldStopApplicationOnConsoleClose())
        {
            return Task.CompletedTask;
        }

        _applicationStoppedRegistration = _applicationLifetime.ApplicationStopped.Register(() => _applicationStopped.Set());
        _consoleControlHandler = HandleConsoleControl;
        _consoleWindowHandle = GetConsoleWindow();

        // Codex 2026-07-04: Stop the dev host when the Visual Studio debug console is closed, so Kestrel and the launched browser do not remain alive.
        if (!SetConsoleCtrlHandler(_consoleControlHandler, add: true))
        {
            _logger.LogDebug("Could not register the development console close handler. Win32 error: {ErrorCode}.",
                Marshal.GetLastWin32Error());
        }

        StartConsoleWindowWatcher();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        UnregisterConsoleControlHandler();
        await StopConsoleWindowWatcherAsync(cancellationToken);
    }

    public void Dispose()
    {
        UnregisterConsoleControlHandler();
        StopConsoleWindowWatcherAsync(CancellationToken.None).GetAwaiter().GetResult();
        _applicationStoppedRegistration.Dispose();
        _applicationStopped.Dispose();
    }

    private bool HandleConsoleControl(int controlType)
    {
        if (controlType is not (CtrlCloseEvent or CtrlLogoffEvent or CtrlShutdownEvent))
        {
            return false;
        }

        RequestApplicationStop("Development console is closing. Stopping WebPhotocopyHub host.");

        _applicationStopped.Wait(TimeSpan.FromMilliseconds(GetConsoleCloseWaitMs()));
        return false;
    }

    private void StartConsoleWindowWatcher()
    {
        if (_consoleWindowHandle == nint.Zero)
        {
            return;
        }

        _consoleWindowWatcherCancellation = new CancellationTokenSource();
        _consoleWindowWatcherTask = Task.Run(
            () => WatchConsoleWindowAsync(_consoleWindowWatcherCancellation.Token),
            CancellationToken.None);
    }

    private async Task WatchConsoleWindowAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(750, cancellationToken);

                if (_consoleWindowHandle != nint.Zero && !IsWindow(_consoleWindowHandle))
                {
                    RequestApplicationStop("Development console window was closed. Stopping WebPhotocopyHub host.");
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task StopConsoleWindowWatcherAsync(CancellationToken cancellationToken)
    {
        var watcherCancellation = _consoleWindowWatcherCancellation;
        var watcherTask = _consoleWindowWatcherTask;

        if (watcherCancellation is null || watcherTask is null)
        {
            return;
        }

        _consoleWindowWatcherCancellation = null;
        _consoleWindowWatcherTask = null;
        watcherCancellation.Cancel();

        try
        {
            await Task.WhenAny(watcherTask, Task.Delay(500, cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            watcherCancellation.Dispose();
        }
    }

    private void RequestApplicationStop(string message)
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) != 0)
        {
            return;
        }

        _logger.LogInformation("{Message}", message);
        _applicationLifetime.StopApplication();
    }

    private void UnregisterConsoleControlHandler()
    {
        if (!OperatingSystem.IsWindows() || _consoleControlHandler is null)
        {
            return;
        }

        SetConsoleCtrlHandler(_consoleControlHandler, add: false);
        _consoleControlHandler = null;
    }

    private bool ShouldStopApplicationOnConsoleClose()
    {
        return _configuration.GetValue<bool?>("DevelopmentConsole:StopApplicationOnClose") ?? true;
    }

    private int GetConsoleCloseWaitMs()
    {
        return Math.Clamp(
            _configuration.GetValue<int?>("DevelopmentConsole:CloseWaitMs") ?? 4500,
            500,
            4500);
    }

    private delegate bool ConsoleControlHandler(int controlType);

    [DllImport("kernel32.dll")]
    private static extern nint GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleControlHandler handlerRoutine, bool add);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);
}
