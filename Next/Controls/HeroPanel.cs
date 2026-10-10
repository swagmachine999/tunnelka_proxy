using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Tunnelka.Models;

namespace Tunnelka.Next.Controls;

public sealed class HeroPanel : UserControl
{
    public const double RequiredHeight =
        HeroGeometry.HeaderHeight + HeroGeometry.BottomMargin + HeroGeometry.FooterHeight + HeroGeometry.GroupHeight * HeroGeometry.GroupScale;

    private const double HintHoldSeconds = 8;
    private const int FrameDefaultMs = 33;
    private const int HiddenFrameMs = 250;

    private readonly Session _session;
    private readonly HeroStage _stage = new();
    private readonly HeroHeader _header = new();
    private readonly HeroFooter _footer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render);

    private bool _busy;
    private DateTime _holdUntil = DateTime.MinValue;
    private ProxyServer? _holdServer;

    public HeroPanel(Session session)
    {
        _session = session;
        ActualThemeVariantChanged += (_, _) => ApplyColors();

        Content = new HeroLayout(_stage, _header, _footer);

        _timer.Interval = TimeSpan.FromMilliseconds(FrameDefaultMs);
        _timer.Tick += OnTick;

        _stage.PowerClicked += () => _session.Toggle();
        _header.ModeSelected += tun => _session.SetMode(tun);
        _footer.RefreshClicked += () => RunSafe(_session.RefreshCurrentSubscription);
        _footer.PingClicked += () => RunSafe(_session.PingCurrent);
        PointerMoved += (_, e) => _stage.PointerAt(e.GetPosition(_stage));
        PointerExited += (_, _) => _stage.LookAway();
        PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                _stage.Click();
        };

        _session.StateChanged += Refresh;
        _session.ModeChanged += UpdateMode;
        _session.Hint += OnHint;
        _session.Speed += _header.SetSpeed;
        _session.ClockTick += text => _stage.Elapsed = text;
        _session.Failed += () => _stage.Fail();

        ApplyColors();
        Localize();
        Refresh();
    }

    public void Localize()
    {
        _header.SetProxyText(L.T("Прокси"));
        _footer.SetButtonTexts(L.T("Обновить подписку"), L.T("Проверка пинга"));
        _stage.SetTexts(L.T("Подключение"), L.T("Подключено"), L.T("Отключено"));
        _footer.ResetServer();
        UpdateServer();
        UpdateHint();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyColors();
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_holdUntil != DateTime.MinValue && DateTime.Now >= _holdUntil)
        {
            _holdUntil = DateTime.MinValue;
            UpdateHint();
        }

        if (!IsEffectivelyVisible)
        {
            SetInterval(HiddenFrameMs);
            return;
        }

        var time = (float)_clock.Elapsed.TotalSeconds;
        var interval = _stage.Step(time, _busy);
        if (_busy)
            _footer.TickDots(time);

        SetInterval(interval);
    }

    private void SetInterval(int milliseconds)
    {
        var span = TimeSpan.FromMilliseconds(milliseconds);
        if (_timer.Interval != span)
            _timer.Interval = span;
    }

    private async void RunSafe(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _session.Log.Write(ex.Message);
        }
    }

    private void ApplyColors()
    {
        _header.ApplyColors();
        _footer.ApplyColors();
    }

    private void UpdateMode() => _header.SetMode(_session.Data.Tun);

    private void OnHint(string text, Tone tone)
    {
        _holdUntil = DateTime.Now.AddSeconds(HintHoldSeconds);
        _holdServer = _session.HeroServer;
        _footer.ShowHint(text, tone);
    }

    private void Refresh()
    {
        _stage.Connected = _session.Active != null;
        _stage.Connecting = _session.Connecting;
        if (_stage.Connected)
            _stage.Elapsed = _session.Elapsed;

        var busy = _session.HeroBusy;
        if (busy && !_busy)
            _holdUntil = DateTime.MinValue;
        if (_holdUntil != DateTime.MinValue && _session.HeroServer != _holdServer)
            _holdUntil = DateTime.MinValue;

        if (busy != _busy)
        {
            _busy = busy;
            _footer.SetBusy(busy);
        }

        UpdateMode();
        UpdateServer();
        UpdateHint();
    }

    private void UpdateServer()
    {
        var empty = _session.Data.Servers.Count == 0 ? L.T("Сначала добавь ключ") : L.T("Выбери сервер");
        _footer.ShowServer(_session.HeroServer, empty);
    }

    private void UpdateHint()
    {
        if (_holdUntil != DateTime.MinValue)
            return;

        var server = _session.HeroServer;
        if (server == null)
            _footer.ShowHint(_session.Data.Servers.Count == 0 ? L.T("Нажми на кнопку или на +, чтобы добавить ключ") : "", Tone.Muted);
        else
            _footer.ShowHint(Session.PingText(server), Session.PingTone(server.PingMs));
    }
}
