using System.Diagnostics;
using System.Net;

namespace Tunnelka.Services;

public sealed class LatencyProbe : IDisposable
{
    private const int Window = 30;
    private const int TimeoutMs = 2000;

    private readonly Func<int?> _proxyPort;
    private readonly Func<string> _url;
    private readonly Queue<bool> _results = new();
    private CancellationTokenSource? _cancel;
    private HttpClient? _client;
    private int? _clientPort;

    public LatencyProbe(Func<int?> proxyPort, Func<string> url)
    {
        _proxyPort = proxyPort;
        _url = url;
    }

    public event Action<int?, int>? Measured;

    public bool IsRunning => _cancel != null;

    public void Start()
    {
        if (_cancel != null)
            return;

        _cancel = new CancellationTokenSource();
        _ = Loop(_cancel.Token);
    }

    public void Stop()
    {
        _cancel?.Cancel();
        _cancel = null;
        _results.Clear();
    }

    private async Task Loop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var timer = Stopwatch.StartNew();
            var ms = await Measure();
            if (token.IsCancellationRequested)
                return;

            _results.Enqueue(ms != null);
            while (_results.Count > Window)
                _results.Dequeue();

            var loss = (int)Math.Round(100.0 * _results.Count(ok => !ok) / _results.Count);
            Measured?.Invoke(ms, loss);

            var rest = 1000 - (int)timer.ElapsedMilliseconds;
            try
            {
                if (rest > 0)
                    await Task.Delay(rest, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task<int?> Measure()
    {
        try
        {
            var client = Client();
            var timer = Stopwatch.StartNew();
            using var response = await client.GetAsync(_url(), HttpCompletionOption.ResponseHeadersRead);
            return (int)response.StatusCode < 500 ? (int)timer.ElapsedMilliseconds : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private HttpClient Client()
    {
        var port = _proxyPort();
        if (_client != null && port == _clientPort)
            return _client;

        _client?.Dispose();
        var handler = new HttpClientHandler { UseProxy = port != null };
        if (port != null)
            handler.Proxy = new WebProxy($"http://127.0.0.1:{port}");

        _client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(TimeoutMs) };
        _clientPort = port;
        return _client;
    }

    public void Dispose()
    {
        Stop();
        _client?.Dispose();
    }
}
