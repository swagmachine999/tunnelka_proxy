namespace Tunnelka.Services.Privileged;

public sealed class ServiceTunBackend : ITunBackend
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly IServiceChannel _channel;
    private readonly object _gate = new();
    private CancellationTokenSource? _poll;
    private bool _running;
    private int? _exitCode;

    public ServiceTunBackend(IServiceChannel channel)
    {
        _channel = channel;
    }

    public event Action<string>? Output;
    public event Action? Exited;

    public bool IsRunning
    {
        get { lock (_gate) return _running; }
    }

    public int? ExitCode
    {
        get { lock (_gate) return _exitCode; }
    }

    public void Start(TunParameters parameters)
    {
        Stop();

        var request = new ServiceRequest
        {
            Command = ServiceCommand.TunStart,
            SocksPort = parameters.SocksPort,
            ServerHost = parameters.ServerHost,
            PhysicalInterface = parameters.PhysicalInterface,
            Mode = parameters.Routing.EffectiveMode.ToString(),
            Rules = parameters.Routing.Rules.Select(r => r.Value).ToList()
        };

        var reply = _channel.Send(request) ?? throw new InvalidOperationException(L.T("Служба Tunnelka не отвечает"));
        if (!reply.Ok)
            throw new InvalidOperationException(reply.Error ?? "service");

        var cts = new CancellationTokenSource();
        lock (_gate)
        {
            _running = true;
            _exitCode = null;
            _poll = cts;
        }

        _ = Task.Run(() => PollAsync(cts.Token, reply.Cursor));
    }

    public bool WaitForExit(int milliseconds)
    {
        var deadline = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < deadline)
        {
            if (!IsRunning)
                return true;
            Thread.Sleep(50);
        }

        return !IsRunning;
    }

    public void Stop()
    {
        CancellationTokenSource? poll;
        lock (_gate)
        {
            poll = _poll;
            _poll = null;
            _running = false;
        }

        if (poll == null)
            return;

        poll.Cancel();
        _channel.Send(new ServiceRequest { Command = ServiceCommand.TunStop });
        poll.Dispose();
    }

    public void Dispose() => Stop();

    private async Task PollAsync(CancellationToken token, long cursor)
    {
        var failures = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var reply = _channel.Send(new ServiceRequest { Command = ServiceCommand.Status, Cursor = cursor });
            if (token.IsCancellationRequested)
                return;

            if (reply is not { Ok: true })
            {
                if (++failures < 6)
                    continue;

                Finish(token, null);
                return;
            }

            failures = 0;
            cursor = reply.Cursor;
            foreach (var line in reply.Lines)
                Output?.Invoke(line);

            if (!reply.TunRunning)
            {
                Finish(token, reply.ExitCode);
                return;
            }
        }
    }

    private void Finish(CancellationToken token, int? exitCode)
    {
        lock (_gate)
        {
            if (token.IsCancellationRequested || _poll == null)
                return;
            _running = false;
            _exitCode = exitCode;
        }

        Exited?.Invoke();
    }
}
