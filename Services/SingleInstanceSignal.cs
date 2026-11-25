namespace Tunnelka.Services;

public sealed class SingleInstanceSignal : IDisposable
{
    public const string Name = "Local\\Tunnelka.RestoreInstance";

    private readonly EventWaitHandle _signal;
    private readonly Thread _listener;
    private volatile bool _disposed;

    public SingleInstanceSignal(Action restoreRequested, string name = Name)
    {
        _signal = new EventWaitHandle(false, EventResetMode.AutoReset, name);
        _listener = new Thread(() =>
        {
            while (!_disposed)
            {
                if (_signal.WaitOne(1000) && !_disposed)
                    restoreRequested();
            }
        }) { IsBackground = true, Name = "TunnelkaRestore" };
        _listener.Start();
    }

    public static bool TrySend(string name = Name)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var signal = EventWaitHandle.OpenExisting(name);
                return signal.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        return false;
    }

    public void Dispose()
    {
        _disposed = true;
        _signal.Set();
        _listener.Join(1500);
        _signal.Dispose();
    }
}
