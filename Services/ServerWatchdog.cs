using System.Net.Sockets;
using Tunnelka.Models;

namespace Tunnelka.Services;

public sealed class ServerWatchdog : IDisposable
{
    private const int ProbeTimeoutMs = 4000;

    private readonly Func<string, int, CancellationToken, Task<bool>> _probe;
    private readonly TimeSpan _interval;
    private readonly int _limit;
    private CancellationTokenSource? _cts;

    public ServerWatchdog(Func<string, int, CancellationToken, Task<bool>> probe, TimeSpan interval, int limit)
    {
        _probe = probe;
        _interval = interval;
        _limit = limit;
    }

    public event Action<ProxyServer>? Lost;

    public static bool CanWatch(ProxyServer server) => server.Protocol is not ("hysteria2" or "tuic");

    public void Start(ProxyServer server)
    {
        Stop();
        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = Task.Run(() => Watch(server, cts.Token));
    }

    public void Stop()
    {
        var cts = Interlocked.Exchange(ref _cts, null);
        cts?.Cancel();
    }

    public void Dispose() => Stop();

    public static async Task<bool> TcpProbe(string host, int port, CancellationToken token)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
        limit.CancelAfter(ProbeTimeoutMs);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, limit.Token);
            return true;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private async Task Watch(ProxyServer server, CancellationToken token)
    {
        var failures = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(_interval, token);
                failures = await _probe(server.Address, server.Port, token) ? 0 : failures + 1;
                if (failures < _limit)
                    continue;

                if (!token.IsCancellationRequested)
                    Lost?.Invoke(server);
                return;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
