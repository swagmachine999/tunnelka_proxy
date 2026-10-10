using Avalonia.Threading;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class UpdateWatcher : IDisposable
{
    private static readonly TimeSpan FirstCheck = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Repeat = TimeSpan.FromHours(6);

    private static Version? _offered;

    private readonly Func<int?> _proxyPort;
    private readonly Action<UpdateInfo> _offer;
    private readonly DispatcherTimer _timer = new() { Interval = FirstCheck };

    public UpdateWatcher(Func<int?> proxyPort, Action<UpdateInfo> offer)
    {
        _proxyPort = proxyPort;
        _offer = offer;
        _timer.Tick += async (_, _) => await Check();
    }

    public void Start() => _timer.Start();

    public void Dispose() => _timer.Stop();

    private async Task Check()
    {
        _timer.Interval = Repeat;
        try
        {
            var update = await UpdateService.CheckAsync(_proxyPort());
            if (update == null || update.Version == _offered)
                return;

            _offered = update.Version;
            _offer(update);
        }
        catch (Exception)
        {
        }
    }
}
