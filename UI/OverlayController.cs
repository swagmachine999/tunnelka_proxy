using Tunnelka.Models;
using Tunnelka.Services;

namespace Tunnelka.UI;

public sealed class OverlayController : IDisposable
{
    private readonly OverlayOptions _options;
    private readonly LatencyProbe _probe;
    private readonly LossProbe _lossProbe;
    private readonly OverlayState _state = new();
    private OverlayWindow? _window;
    private GlobalHotkey? _hotkey;

    public OverlayController(OverlayOptions options, Func<int?> proxyPort, Func<string> pingUrl, Func<(string Host, int Port)?> lossTarget)
    {
        _options = options;
        _probe = new LatencyProbe(proxyPort, pingUrl);
        _lossProbe = new LossProbe(lossTarget);
        _probe.Measured += ms =>
        {
            _state.PingMs = ms;
            Redraw();
        };
        _lossProbe.Measured += loss =>
        {
            _state.Loss = loss;
            Redraw();
        };
    }

    public event EventHandler? VisibilityChanged;

    public bool IsShown => _window is { Visible: true };

    public bool HotkeyFailed { get; private set; }

    public void ApplyHotkey()
    {
        _hotkey ??= CreateHotkey();
        HotkeyFailed = false;
        if (_options.HotkeyEnabled)
            HotkeyFailed = !_hotkey.Set((Keys)_options.Hotkey);
        else
            _hotkey.Clear();
    }

    private GlobalHotkey CreateHotkey()
    {
        var hotkey = new GlobalHotkey();
        hotkey.Pressed += (_, _) => Toggle();
        return hotkey;
    }

    public void Toggle()
    {
        if (IsShown)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        _window ??= new OverlayWindow();
        _probe.Start();
        _window.ShowState(_state, _options);
        SyncLossProbe();
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Hide()
    {
        _probe.Stop();
        _lossProbe.Stop();
        _window?.Hide();
        _state.PingMs = null;
        _state.Loss = 0;
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetConnected(bool connected)
    {
        _state.Connected = connected;
        if (!connected)
            _state.Down = _state.Up = 0;
        SyncLossProbe();
        Redraw();
    }

    public void ApplyOptions()
    {
        SyncLossProbe();
        Redraw();
    }

    private void SyncLossProbe()
    {
        if (IsShown && _state.Connected && _options.ShowLoss)
        {
            _lossProbe.Start();
            return;
        }

        _lossProbe.Stop();
        _state.Loss = 0;
    }

    public void SetSpeed(long down, long up)
    {
        _state.Down = down;
        _state.Up = up;
        Redraw();
    }

    public void Redraw()
    {
        if (IsShown)
            _window!.ShowState(_state, _options);
    }

    public void Dispose()
    {
        _probe.Dispose();
        _lossProbe.Dispose();
        _hotkey?.Dispose();
        _window?.Dispose();
    }
}
