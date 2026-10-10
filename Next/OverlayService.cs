namespace Tunnelka.Next;

public sealed class OverlayService : IDisposable
{
    public event EventHandler? VisibilityChanged;

    public bool IsShown => false;

    public bool HotkeyFailed => false;

    public void ApplyHotkey()
    {
    }

    public void ApplyOptions()
    {
    }

    public void Toggle()
    {
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetConnected(bool connected)
    {
    }

    public void SetSpeed(long down, long up)
    {
    }

    public void Dispose()
    {
    }
}
