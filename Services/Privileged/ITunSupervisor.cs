namespace Tunnelka.Services.Privileged;

public interface ITunSupervisor
{
    bool IsRunning { get; }

    int? ExitCode { get; }

    long Cursor { get; }

    void Start(TunParameters parameters, int ownerProcessId);

    void Stop();

    (long Cursor, List<string> Lines) Read(long cursor);
}
