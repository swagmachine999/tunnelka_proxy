using System.Diagnostics;

namespace Tunnelka.Services.Privileged;

public interface IOwnerMonitor
{
    IDisposable Watch(int processId, Action onExit);
}

public sealed class ProcessOwnerMonitor : IOwnerMonitor
{
    public IDisposable Watch(int processId, Action onExit)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(processId);
            process.EnableRaisingEvents = true;
        }
        catch (Exception)
        {
            onExit();
            return new Subscription(null);
        }

        process.Exited += (_, _) => onExit();
        if (process.HasExited)
            onExit();
        return new Subscription(process);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Process? _process;

        public Subscription(Process? process)
        {
            _process = process;
        }

        public void Dispose() => _process?.Dispose();
    }
}
