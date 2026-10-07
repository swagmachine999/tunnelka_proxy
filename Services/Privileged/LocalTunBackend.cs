namespace Tunnelka.Services.Privileged;

public sealed class LocalTunBackend : ITunBackend
{
    private readonly ICoreRunner _runner;

    public LocalTunBackend(ICoreRunner runner)
    {
        _runner = runner;
        _runner.Output += line => Output?.Invoke(line);
        _runner.Exited += () => Exited?.Invoke();
    }

    public event Action<string>? Output;
    public event Action? Exited;

    public bool IsRunning => _runner.IsRunning;

    public int? ExitCode => _runner.ExitCode;

    public void Start(TunParameters parameters) =>
        _runner.Start(TunConfigBuilder.Build(parameters.SocksPort, parameters.Routing, parameters.ServerHost, parameters.PhysicalInterface));

    public bool WaitForExit(int milliseconds) => _runner.WaitForExit(milliseconds);

    public void Stop() => _runner.Stop();

    public void Dispose() => _runner.Dispose();
}
