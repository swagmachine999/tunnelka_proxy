namespace Tunnelka.Services;

public interface ICoreRunner : IDisposable
{
    event Action<string>? Output;
    event Action? Exited;

    bool IsRunning { get; }

    int? ExitCode { get; }

    bool WaitForExit(int milliseconds);

    void Start(string configJson);

    void Stop();
}
