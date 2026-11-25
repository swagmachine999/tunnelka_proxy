using Avalonia.Threading;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class UpdateWatcher : IDisposable
{
    private static readonly TimeSpan FirstCheck = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Repeat = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan Retry = TimeSpan.FromMinutes(1);

    private static Version? _offered;

    private readonly Func<int?> _proxyPort;
    private readonly Action<UpdateInfo> _offer;
    private readonly DispatcherTimer _timer = new() { Interval = FirstCheck };
    private bool _disposed;
    public UpdateWatcher(Func<int?> proxyPort, Action<UpdateInfo> offer)
    {
        _proxyPort = proxyPort;
        _offer = offer;
        _timer.Tick += async (_, _) => await Check();
    }

    public void Start()
    {
        if (!_disposed)
            _timer.Start();
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
    }

    private async Task Check()
    {
        _timer.Stop();
        try
        {
            var update = await UpdateService.CheckAsync(_proxyPort());
            if (update == null || update.Version == _offered)
                return;

            var remaining = UpdateService.DelayUntilOffer(update.PublishedAt, DateTimeOffset.UtcNow);
            if (remaining > TimeSpan.Zero)
            {
                _timer.Interval = remaining < Repeat ? remaining : Repeat;
                return;
            }

            if (update.DownloadUrl.Length == 0)
            {
                _timer.Interval = Retry;
                return;
            }

            _offered = update.Version;
            _offer(update);
        }
        catch (Exception)
        {
            _timer.Interval = Retry;
        }
        finally
        {
            if (!_disposed)
                _timer.Start();
        }
    }
}
