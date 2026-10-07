using System.Net;
using System.Net.Sockets;

namespace Tunnelka.Services;

public sealed class LossProbe : IDisposable
{
    public const int PerSecond = 5;
    public const int TimeoutMs = 1000;

    private readonly Func<(string Host, int Port)?> _target;
    private readonly Func<string, int, CancellationToken, Task<bool>> _reach;
    private readonly int _intervalMs;
    private readonly object _gate = new();
    private readonly LossWindow _window = new();
    private CancellationTokenSource? _cancel;
    private string? _key;
    private int _reported;

    public LossProbe(Func<(string Host, int Port)?> target)
        : this(target, TcpReach, 1000 / PerSecond)
    {
    }

    public LossProbe(Func<(string Host, int Port)?> target, Func<string, int, CancellationToken, Task<bool>> reach, int intervalMs)
    {
        _target = target;
        _reach = reach;
        _intervalMs = intervalMs;
    }

    public event Action<int>? Measured;

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
        lock (_gate)
            Reset();
    }

    private async Task Loop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var target = _target();
            if (target != null)
                _ = Probe(target.Value, token);

            try
            {
                await Task.Delay(_intervalMs, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task Probe((string Host, int Port) target, CancellationToken token)
    {
        var key = target.Host + ":" + target.Port;
        bool delivered;
        try
        {
            delivered = await _reach(target.Host, target.Port, token);
        }
        catch (Exception)
        {
            delivered = false;
        }

        if (token.IsCancellationRequested)
            return;

        int percent;
        lock (_gate)
        {
            if (key != _key)
            {
                _window.Clear();
                _key = key;
            }

            _window.Add(delivered);
            percent = _window.Percent;
            if (percent == _reported)
                return;
            _reported = percent;
        }

        Measured?.Invoke(percent);
    }

    private void Reset()
    {
        _window.Clear();
        _key = null;
        _reported = 0;
    }

    private static async Task<bool> TcpReach(string host, int port, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeoutMs);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            return true;
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose() => Stop();
}
