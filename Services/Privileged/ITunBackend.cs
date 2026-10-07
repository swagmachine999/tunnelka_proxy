namespace Tunnelka.Services.Privileged;

public interface ITunBackend : IDisposable
{
    event Action<string>? Output;
    event Action? Exited;

    bool IsRunning { get; }

    int? ExitCode { get; }

    void Start(TunParameters parameters);

    bool WaitForExit(int milliseconds);

    void Stop();
}
