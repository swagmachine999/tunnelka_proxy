using Avalonia.Threading;
using Tunnelka.Models;
using Tunnelka.Services;

namespace Tunnelka.Desktop;

public sealed class OverlayService : IDisposable
{
    private readonly OverlayState _state = new();
    private Session? _session;
    private LatencyProbe? _probe;
    private LossProbe? _lossProbe;
    private OverlayWindow? _window;
    private GlobalHotkey? _hotkey;
    private readonly DispatcherTimer _fullscreenTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _requestedShown;

    public event EventHandler? VisibilityChanged;

    public bool IsShown => _requestedShown;

    public bool HotkeyFailed { get; private set; }

    private OverlayOptions? Options => _session?.Data.Overlay;

    public void Attach(Session session)
    {
        if (_session != null)
            return;

        _session = session;
        _fullscreenTimer.Tick += (_, _) => UpdateVisibility();
        _probe = new LatencyProbe(() => session.ProxyPort, () => PingTarget.Url);
        _lossProbe = new LossProbe(() => session.LossTarget());
        _probe.Measured += ms => Dispatcher.UIThread.Post(() =>
        {
            _state.PingMs = ms;
            Redraw();
        });
        _lossProbe.Measured += loss => Dispatcher.UIThread.Post(() =>
        {
            _state.Loss = loss;
            Redraw();
        });
    }

    public void ApplyHotkey()
    {
        var options = Options;
        if (options == null)
            return;

        if (_hotkey == null)
        {
            _hotkey = new GlobalHotkey();
            _hotkey.Pressed += (_, _) => Toggle();
        }

        HotkeyFailed = false;
        if (options.HotkeyEnabled)
            HotkeyFailed = !_hotkey.Set(options.Hotkey);
        else
            _hotkey.Clear();
    }

    public void ApplyOptions()
    {
        SyncLossProbe();
        Redraw();
    }

    public void Toggle()
    {
        if (IsShown)
            Hide();
        else
            Show();
    }

    public void SetConnected(bool connected)
    {
        _state.Connected = connected;
        if (!connected)
            _state.Down = _state.Up = 0;
        SyncLossProbe();
        Redraw();
    }

    public void SetSpeed(long down, long up)
    {
        _state.Down = down;
        _state.Up = up;
        Redraw();
    }

    public void Dispose()
    {
        _probe?.Dispose();
        _lossProbe?.Dispose();
        _hotkey?.Dispose();
        _hotkey = null;
        _window?.Close();
        _window = null;
        _fullscreenTimer.Stop();
    }

    private void Show()
    {
        var options = Options;
        if (options == null)
            return;

        _window ??= new OverlayWindow();
        _requestedShown = true;
        _fullscreenTimer.Start();
        UpdateVisibility();
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Hide()
    {
        _requestedShown = false;
        _fullscreenTimer.Stop();
        _probe?.Stop();
        _lossProbe?.Stop();
        _window?.Hide();
        _state.PingMs = null;
        _state.Loss = 0;
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncLossProbe()
    {
        if (_window is { IsVisible: true } && _state.Connected && Options is { ShowLoss: true })
        {
            _lossProbe?.Start();
            return;
        }

        _lossProbe?.Stop();
        _state.Loss = 0;
    }

    private void Redraw()
    {
        var options = Options;
        if (_requestedShown && options != null)
            UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (!_requestedShown || _window == null || Options is not { } options)
            return;

        if (Win32.FullscreenAppActive())
        {
            _window.Hide();
            _probe?.Stop();
            _lossProbe?.Stop();
            return;
        }

        if (!_window.IsVisible)
            _probe?.Start();
        _window.ShowState(_state, options);
        SyncLossProbe();
    }
}
