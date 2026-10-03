using Tunnelka.Services;

namespace Tunnelka.UI;

public sealed class UpdateWatcher : IDisposable
{
    private const int FirstCheckMs = 15_000;
    private const int RepeatMs = 6 * 60 * 60 * 1000;

    private static Version? _offered;

    private readonly Func<int?> _proxyPort;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = FirstCheckMs };

    public UpdateWatcher(Func<int?> proxyPort)
    {
        _proxyPort = proxyPort;
        _timer.Tick += async (_, _) => await Check();
    }

    public void Start() => _timer.Start();

    private async Task Check()
    {
        _timer.Interval = RepeatMs;
        try
        {
            var update = await UpdateService.CheckAsync(_proxyPort());
            if (update == null || update.Version == _offered)
                return;

            _offered = update.Version;
            UpdateDialog.Show(update, _proxyPort);
        }
        catch (Exception)
        {
        }
    }

    public void Dispose() => _timer.Dispose();
}
