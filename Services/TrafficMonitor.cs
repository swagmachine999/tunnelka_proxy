using System.Text.Json;

namespace VpnClient.Services;

public sealed class TrafficMonitor : IDisposable
{
    private static readonly HttpClient Http = new(new HttpClientHandler { UseProxy = false })
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private bool _busy;

    public event Action<TrafficCounters>? Updated;

    public TrafficMonitor()
    {
        _timer.Tick += async (_, _) => await Poll();
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private async Task Poll()
    {
        if (_busy)
            return;

        _busy = true;
        try
        {
            var json = await Http.GetStringAsync($"http://127.0.0.1:{XrayConfigBuilder.MetricsPort}/debug/vars");
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("stats", out var stats) ||
                !stats.TryGetProperty("outbound", out var outbound))
                return;

            Updated?.Invoke(new TrafficCounters
            {
                ProxyDown = Read(outbound, "proxy", "downlink"),
                ProxyUp = Read(outbound, "proxy", "uplink"),
                DirectDown = Read(outbound, "direct", "downlink"),
                DirectUp = Read(outbound, "direct", "uplink")
            });
        }
        catch (Exception)
        {
        }
        finally
        {
            _busy = false;
        }
    }

    private static long Read(JsonElement outbound, string tag, string direction) =>
        outbound.TryGetProperty(tag, out var node) && node.TryGetProperty(direction, out var value) && value.TryGetInt64(out var number)
            ? number
            : 0;

    public void Dispose() => _timer.Dispose();
}
