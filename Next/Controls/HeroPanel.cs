using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Tunnelka.Models;
using Tunnelka.UI;

namespace Tunnelka.Next.Controls;

public sealed class HeroPanel : UserControl
{
    public const double RequiredHeight =
        HeroGeometry.HeaderHeight + HeroGeometry.BottomMargin + HeroGeometry.FooterHeight + HeroGeometry.GroupHeight * HeroGeometry.GroupScale;

    private const double HintHoldSeconds = 8;
    private const double BodyFont = 15.4;
    private const double CaptionFont = 14.3;
    private const double NameFont = 18.7;

    private readonly Session _session;
    private readonly HeroStage _stage = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render);

    private readonly Border _speedChip = new();
    private readonly TextBlock _downText = new();
    private readonly TextBlock _upText = new();
    private readonly Avalonia.Controls.Shapes.Path _downArrow = new();
    private readonly Avalonia.Controls.Shapes.Path _upArrow = new();

    private readonly Border _toggle = new();
    private readonly Border _proxySegment = new();
    private readonly Border _tunSegment = new();
    private readonly TextBlock _proxyLabel = new();
    private readonly TextBlock _tunLabel = new();

    private readonly NameView _name = new();
    private readonly TextBlock _empty = new();
    private readonly Border _refreshButton = new();
    private readonly Border _pingButton = new();
    private readonly TextBlock _refreshLabel = new();
    private readonly TextBlock _pingLabel = new();
    private readonly TextBlock _hintText = new();
    private readonly HeroBusyDots _dots = new();

    private bool _hoverToggle;
    private bool _hoverRefresh;
    private bool _hoverPing;
    private bool _busy;
    private Tone _hintTone = Tone.Muted;
    private DateTime _holdUntil = DateTime.MinValue;
    private ProxyServer? _holdServer;
    private ProxyServer? _shownServer;
    private string _shownName = "";

    public HeroPanel(Session session)
    {
        _session = session;

        Content = new HeroLayout(_stage, BuildHeader(), BuildFooter());

        _timer.Interval = TimeSpan.FromMilliseconds(FrameDefaultMs);
        _timer.Tick += OnTick;

        _stage.PowerClicked += () => _session.Toggle();
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
        _session.Speed += OnSpeed;
        _session.ClockTick += text => _stage.Elapsed = text;
        _session.Failed += () => _stage.Fail();

        ApplyColors();
        Localize();
        Refresh();
    }

    private const int FrameDefaultMs = 33;

    public void Localize()
    {
        _proxyLabel.Text = L.T("Прокси");
        _refreshLabel.Text = L.T("Обновить подписку");
        _pingLabel.Text = L.T("Проверка пинга");
        _stage.SetTexts(L.T("Подключение"), L.T("Подключено"), L.T("Отключено"));
        _shownServer = null;
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualThemeVariantProperty)
            ApplyColors();
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
            SetInterval(250);
            return;
        }

        var time = (float)_clock.Elapsed.TotalSeconds;
        var interval = _stage.Step(time, _busy);
        if (_busy)
            _dots.Tick(time);

        SetInterval(interval);
    }

    private void SetInterval(int milliseconds)
    {
        var span = TimeSpan.FromMilliseconds(milliseconds);
        if (_timer.Interval != span)
            _timer.Interval = span;
    }

    private static IBrush Solid(Color color, byte alpha) =>
        new ImmutableSolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));

    private static Color Lighten(Color color, double amount) => Color.FromArgb(
        color.A,
        (byte)(color.R + (255 - color.R) * amount),
        (byte)(color.G + (255 - color.G) * amount),
        (byte)(color.B + (255 - color.B) * amount));

    private static TextBlock StyleLabel(TextBlock block, double size)
    {
        block.FontFamily = new FontFamily("Segoe UI");
        block.FontSize = size;
        block.FontWeight = FontWeight.SemiBold;
        block.HorizontalAlignment = HorizontalAlignment.Center;
        block.VerticalAlignment = VerticalAlignment.Center;
        return block;
    }

    private Control BuildHeader()
    {
        var grid = new Grid { Height = HeroGeometry.HeaderHeight };

        _downArrow.Data = Geometry.Parse("M5,0 L5,12 M1,7.5 L5,12 L9,7.5");
        _upArrow.Data = Geometry.Parse("M5,0 L5,12 M1,4.5 L5,0 L9,4.5");
        foreach (var arrow in new[] { _downArrow, _upArrow })
        {
            arrow.Width = 10;
            arrow.Height = 12;
            arrow.Stretch = Stretch.None;
            arrow.StrokeThickness = 1.8;
            arrow.StrokeLineCap = PenLineCap.Round;
            arrow.StrokeJoin = PenLineJoin.Round;
            arrow.VerticalAlignment = VerticalAlignment.Center;
        }

        _downArrow.Margin = new Thickness(0, 0, 5, 0);
        _upArrow.Margin = new Thickness(0, 0, 5, 0);
        StyleLabel(_downText, BodyFont).Margin = new Thickness(0, 0, 10, 0);
        StyleLabel(_upText, BodyFont);
        _downText.HorizontalAlignment = HorizontalAlignment.Left;
        _upText.HorizontalAlignment = HorizontalAlignment.Left;

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15, 0, 17, 0)
        };
        row.Children.Add(_downArrow);
        row.Children.Add(_downText);
        row.Children.Add(_upArrow);
        row.Children.Add(_upText);

        _speedChip.Height = 40;
        _speedChip.CornerRadius = new CornerRadius(20);
        _speedChip.BorderThickness = new Thickness(1);
        _speedChip.HorizontalAlignment = HorizontalAlignment.Left;
        _speedChip.VerticalAlignment = VerticalAlignment.Top;
        _speedChip.Margin = new Thickness(24, 24, 0, 0);
        _speedChip.IsVisible = false;
        _speedChip.Child = row;
        grid.Children.Add(_speedChip);

        StyleSegment(_proxySegment, _proxyLabel, false);
        StyleSegment(_tunSegment, _tunLabel, true);
        _tunLabel.Text = "TUN";
        var segments = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(3) };
        Grid.SetColumn(_tunSegment, 1);
        segments.Children.Add(_proxySegment);
        segments.Children.Add(_tunSegment);

        _toggle.Width = 190;
        _toggle.Height = 40;
        _toggle.CornerRadius = new CornerRadius(20);
        _toggle.BorderThickness = new Thickness(1);
        _toggle.HorizontalAlignment = HorizontalAlignment.Right;
        _toggle.VerticalAlignment = VerticalAlignment.Top;
        _toggle.Margin = new Thickness(0, 24, 24, 0);
        _toggle.Child = segments;
        _toggle.PointerEntered += (_, _) =>
        {
            _hoverToggle = true;
            ApplyToggle();
        };
        _toggle.PointerExited += (_, _) =>
        {
            _hoverToggle = false;
            ApplyToggle();
        };
        grid.Children.Add(_toggle);
        return grid;
    }

    private void StyleSegment(Border segment, TextBlock label, bool tun)
    {
        StyleLabel(label, BodyFont);
        segment.CornerRadius = new CornerRadius(16);
        segment.Background = Brushes.Transparent;
        segment.Cursor = new Cursor(StandardCursorType.Hand);
        segment.Child = label;
        segment.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                _session.SetMode(tun);
        };
    }

    private Control BuildFooter()
    {
        _name.FontSize = NameFont;
        _name.FontWeight = FontWeight.SemiBold;
        _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.VerticalAlignment = VerticalAlignment.Center;
        StyleLabel(_empty, NameFont);
        _empty.IsVisible = false;

        var nameHost = new Grid { Height = 34, Margin = new Thickness(16, 12, 16, 0) };
        nameHost.Children.Add(_name);
        nameHost.Children.Add(_empty);

        StyleButton(_refreshButton, _refreshLabel, () => RunSafe(_session.RefreshCurrentSubscription), true);
        StyleButton(_pingButton, _pingLabel, () => RunSafe(_session.PingCurrent), false);
        Grid.SetColumn(_pingButton, 2);
        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,12,*"),
            Height = 44,
            MaxWidth = 372,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(24, 18, 24, 0)
        };
        buttons.Children.Add(_refreshButton);
        buttons.Children.Add(_pingButton);

        StyleLabel(_hintText, CaptionFont);
        _hintText.TextTrimming = TextTrimming.CharacterEllipsis;
        _dots.HorizontalAlignment = HorizontalAlignment.Center;
        _dots.VerticalAlignment = VerticalAlignment.Center;
        _dots.IsVisible = false;
        var hintHost = new Grid { Height = 22, Margin = new Thickness(16, 16, 16, 0) };
        hintHost.Children.Add(_hintText);
        hintHost.Children.Add(_dots);

        var footer = new StackPanel();
        footer.Children.Add(nameHost);
        footer.Children.Add(buttons);
        footer.Children.Add(hintHost);
        return footer;
    }

    private void StyleButton(Border button, TextBlock label, Action click, bool outlined)
    {
        StyleLabel(label, BodyFont);
        button.Height = 44;
        button.CornerRadius = new CornerRadius(22);
        button.BorderThickness = outlined ? new Thickness(1.4) : new Thickness(0);
        button.Cursor = new Cursor(StandardCursorType.Hand);
        button.Child = label;
        button.PointerEntered += (_, _) =>
        {
            if (outlined)
                _hoverRefresh = true;
            else
                _hoverPing = true;
            ApplyButtons();
        };
        button.PointerExited += (_, _) =>
        {
            if (outlined)
                _hoverRefresh = false;
            else
                _hoverPing = false;
            ApplyButtons();
        };
        button.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                click();
        };
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
        var card = Ui.Color(this, "CardColor");
        var border = Ui.Brush(this, "BorderBrush2");
        _speedChip.Background = Solid(card, 225);
        _speedChip.BorderBrush = border;
        _toggle.BorderBrush = border;
        _downArrow.Stroke = Ui.Brush(this, "AccentStrongBrush");
        _upArrow.Stroke = Ui.Brush(this, "PingBadBrush");
        var text = Ui.Brush(this, "TextBrush");
        _downText.Foreground = text;
        _upText.Foreground = text;
        _name.Foreground = text;
        _empty.Foreground = Ui.Brush(this, "TextMutedBrush");
        _pingLabel.Foreground = Brushes.White;
        _dots.SetColors(Ui.Color(this, "AccentColor"), Ui.Color(this, "PinkColor"));
        ApplyToggle();
        ApplyButtons();
        UpdateMode();
        ApplyHintBrush();
    }

    private void ApplyToggle() =>
        _toggle.Background = Solid(Ui.Color(this, "CardColor"), (byte)(_hoverToggle ? 250 : 215));

    private void ApplyButtons()
    {
        _refreshButton.Background = Solid(Ui.Color(this, "CardColor"), (byte)(_hoverRefresh ? 255 : 215));
        _refreshButton.BorderBrush = Ui.Brush(this, _hoverRefresh ? "AccentBrush" : "BorderBrush2");
        _refreshLabel.Foreground = Ui.Brush(this, "AccentStrongBrush");

        var accent = Ui.Color(this, "AccentColor");
        var pink = Ui.Color(this, "PinkColor");
        if (_hoverPing)
        {
            accent = Lighten(accent, 0.15);
            pink = Lighten(pink, 0.15);
        }

        _pingButton.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(accent, 0), new GradientStop(pink, 1) }
        };

        var opacity = _busy ? 0.7 : 1.0;
        _refreshButton.Opacity = opacity;
        _pingButton.Opacity = opacity;
    }

    private void UpdateMode()
    {
        var tun = _session.Data.Tun;
        ApplySegment(_proxySegment, _proxyLabel, !tun);
        ApplySegment(_tunSegment, _tunLabel, tun);
    }

    private void ApplySegment(Border segment, TextBlock label, bool active)
    {
        segment.Background = active ? Ui.Brush(this, "AccentBrush") : Brushes.Transparent;
        label.Foreground = active ? Brushes.White : Ui.Brush(this, "TextMutedBrush");
    }

    private void OnSpeed(string? down, string? up)
    {
        _speedChip.IsVisible = down != null && up != null;
        _downText.Text = down ?? "";
        _upText.Text = up ?? "";
    }

    private void OnHint(string text, Tone tone)
    {
        _holdUntil = DateTime.Now.AddSeconds(HintHoldSeconds);
        _holdServer = _session.HeroServer;
        ShowHint(text, tone);
    }

    private void ShowHint(string text, Tone tone)
    {
        _hintText.Text = text;
        _hintTone = tone;
        ApplyHintBrush();
    }

    private void ApplyHintBrush() =>
        _hintText.Foreground = Ui.Brush(this, Ui.ToneBrush(_hintTone));

    private void Refresh()
    {
        var connected = _session.Active != null;
        _stage.Connected = connected;
        _stage.Connecting = _session.Connecting;
        if (connected)
            _stage.Elapsed = _session.Elapsed;

        var busy = _session.HeroBusy;
        if (busy && !_busy)
            _holdUntil = DateTime.MinValue;
        if (_holdUntil != DateTime.MinValue && _session.HeroServer != _holdServer)
            _holdUntil = DateTime.MinValue;

        var busyChanged = busy != _busy;
        _busy = busy;
        if (busyChanged)
            ApplyButtons();

        UpdateMode();
        UpdateServer();
        UpdateHint();
    }

    private void UpdateServer()
    {
        var server = _session.HeroServer;
        if (server == null)
        {
            _shownServer = null;
            _name.IsVisible = false;
            _empty.IsVisible = true;
            _empty.Text = _session.Data.Servers.Count == 0 ? L.T("Сначала добавь ключ") : L.T("Выбери сервер");
            return;
        }

        _empty.IsVisible = false;
        _name.IsVisible = true;
        if (ReferenceEquals(server, _shownServer) && server.Name == _shownName)
            return;

        _shownServer = server;
        _shownName = server.Name;
        _name.SetParts(ServerText.Parts(server), ServerText.CountryCode(server.Name));
    }

    private void UpdateHint()
    {
        _dots.IsVisible = _busy;
        _hintText.IsVisible = !_busy;
        if (_holdUntil != DateTime.MinValue)
            return;

        var server = _session.HeroServer;
        if (server == null)
            ShowHint(_session.Data.Servers.Count == 0 ? L.T("Нажми на кнопку или на +, чтобы добавить ключ") : "", Tone.Muted);
        else
            ShowHint(Session.PingText(server), Session.PingTone(server.PingMs));
    }
}
