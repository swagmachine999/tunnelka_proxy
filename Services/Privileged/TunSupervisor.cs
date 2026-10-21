namespace Tunnelka.Services.Privileged;

public sealed class TunSupervisor : ITunSupervisor, IDisposable
{
    private const int StartupCheckMs = 800;

    private readonly ICoreRunner _runner;
    private readonly IOwnerMonitor _owner;
    private readonly Action<string> _log;
    private readonly Func<IReadOnlyList<string>>? _closeForeign;
    private readonly LineBuffer _lines = new();
    private readonly object _gate = new();
    private IDisposable? _watch;
    private int _generation;

    public TunSupervisor(ICoreRunner runner, IOwnerMonitor owner, Action<string> log, Func<IReadOnlyList<string>>? closeForeign = null)
    {
        _runner = runner;
        _owner = owner;
        _log = log;
        _closeForeign = closeForeign;
        _runner.Output += _lines.Add;
    }

    public bool IsRunning => _runner.IsRunning;

    public int? ExitCode => _runner.ExitCode;

    public long Cursor => _lines.Cursor;

    public void Start(TunParameters parameters, int ownerProcessId)
    {
        lock (_gate)
        {
            StopLocked();
            var generation = ++_generation;
            CloseForeign();

            _runner.Start(TunConfigBuilder.Build(parameters.SocksPort, parameters.Routing, parameters.ServerHost, parameters.PhysicalInterface, parameters.SocksUser, parameters.SocksPassword));
            if (_runner.WaitForExit(StartupCheckMs))
            {
                var code = _runner.ExitCode ?? -1;
                _runner.Stop();
                throw new InvalidOperationException($"sing-box exited with code {code}");
            }

            _log($"TUN started for process {ownerProcessId}");
            _watch = _owner.Watch(ownerProcessId, () => OwnerGone(generation));
        }
    }

    public void Stop()
    {
        lock (_gate)
            StopLocked();
    }

    public (long Cursor, List<string> Lines) Read(long cursor) => _lines.ReadFrom(cursor);

    public void Dispose() => Stop();

    private void CloseForeign()
    {
        if (_closeForeign == null)
            return;

        IReadOnlyList<string> closed;
        try
        {
            closed = _closeForeign();
        }
        catch (Exception ex)
        {
            _log($"Closing other VPN cores failed: {ex.Message}");
            return;
        }

        if (closed.Count == 0)
            return;

        _lines.Add("Закрыты ядра другого VPN: " + string.Join(", ", closed));
        Thread.Sleep(1000);
    }

    private void OwnerGone(int generation)
    {
        lock (_gate)
        {
            if (generation != _generation)
                return;

            _log("App process exited, stopping TUN");
            StopLocked();
        }
    }

    private void StopLocked()
    {
        _generation++;
        _watch?.Dispose();
        _watch = null;
        _runner.Stop();
    }
}
