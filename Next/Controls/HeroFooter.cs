using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Tunnelka.Models;
using Tunnelka.UI;
using static Tunnelka.Next.Controls.HeroStyle;

namespace Tunnelka.Next.Controls;

internal sealed class HeroFooter : StackPanel
{
    private const double ButtonHeight = 44;
    private const double ButtonPadding = 28;
    private const double ButtonGap = 12;

    private readonly NameView _name = new();
    private readonly TextBlock _empty = new();
    private readonly Border _refreshButton = new();
    private readonly Border _pingButton = new();
    private readonly TextBlock _refreshLabel = new();
    private readonly TextBlock _pingLabel = new();
    private readonly TextBlock _hintText = new();
    private readonly HeroBusyDots _dots = new();

    private bool _hoverRefresh;
    private bool _hoverPing;
    private bool _busy;
    private Tone _hintTone = Tone.Muted;
    private ProxyServer? _shownServer;
    private string _shownName = "";

    public HeroFooter()
    {
        Children.Add(BuildNameRow());
        Children.Add(BuildButtonRow());
        Children.Add(BuildHintRow());
    }

    public event Action? RefreshClicked;

    public event Action? PingClicked;

    public void SetButtonTexts(string refresh, string ping)
    {
        _refreshLabel.Text = refresh;
        _pingLabel.Text = ping;
    }

    public void ResetServer() => _shownServer = null;

    public void TickDots(float time) => _dots.Tick(time);

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _dots.IsVisible = busy;
        _hintText.IsVisible = !busy;
        ApplyButtons();
    }

    public void ShowHint(string text, Tone tone)
    {
        _hintText.Text = text;
        _hintTone = tone;
        ApplyHintBrush();
    }

    public void ShowServer(ProxyServer? server, string emptyText)
    {
        if (server == null)
        {
            _shownServer = null;
            _name.IsVisible = false;
            _empty.IsVisible = true;
            _empty.Text = emptyText;
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

    public void ApplyColors()
    {
        _name.Foreground = Ui.Brush(this, "TextBrush");
        _empty.Foreground = Ui.Brush(this, "TextMutedBrush");
        _pingLabel.Foreground = Brushes.White;
        _dots.SetColors(Ui.Color(this, "AccentColor"), Ui.Color(this, "PinkColor"));
        ApplyButtons();
        ApplyHintBrush();
    }

    private Control BuildNameRow()
    {
        _name.FontSize = NameFont;
        _name.FontWeight = FontWeight.SemiBold;
        _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.VerticalAlignment = VerticalAlignment.Center;
        StyleText(_empty, NameFont);
        _empty.IsVisible = false;

        var host = new Grid { Height = 34, Margin = new Thickness(16, 12, 16, 0) };
        host.Children.Add(_name);
        host.Children.Add(_empty);
        return host;
    }

    private Control BuildButtonRow()
    {
        StyleButton(_refreshButton, _refreshLabel, () => RefreshClicked?.Invoke(), hover => _hoverRefresh = hover, true);
        StyleButton(_pingButton, _pingLabel, () => PingClicked?.Invoke(), hover => _hoverPing = hover, false);
        _pingButton.Margin = new Thickness(ButtonGap, 0, 0, 0);

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(24, 18, 24, 0)
        };
        row.Children.Add(_refreshButton);
        row.Children.Add(_pingButton);
        return row;
    }

    private Control BuildHintRow()
    {
        StyleText(_hintText, CaptionFont);
        _hintText.TextTrimming = TextTrimming.CharacterEllipsis;
        _dots.HorizontalAlignment = HorizontalAlignment.Center;
        _dots.VerticalAlignment = VerticalAlignment.Center;
        _dots.IsVisible = false;

        var host = new Grid { Height = 22, Margin = new Thickness(16, 16, 16, 0) };
        host.Children.Add(_hintText);
        host.Children.Add(_dots);
        return host;
    }

    private void StyleButton(Border button, TextBlock label, Action click, Action<bool> setHover, bool outlined)
    {
        StyleText(label, BodyFont);
        label.TextWrapping = TextWrapping.NoWrap;
        button.Height = ButtonHeight;
        button.Padding = new Thickness(ButtonPadding, 0);
        button.CornerRadius = new CornerRadius(ButtonHeight / 2);
        button.BorderThickness = outlined ? new Thickness(1.4) : new Thickness(0);
        button.Cursor = new Cursor(StandardCursorType.Hand);
        button.Child = label;
        button.PointerEntered += (_, _) =>
        {
            setHover(true);
            ApplyButtons();
        };
        button.PointerExited += (_, _) =>
        {
            setHover(false);
            ApplyButtons();
        };
        button.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                click();
        };
    }

    private void ApplyButtons()
    {
        _refreshButton.Background = Solid(Ui.Color(this, "CardColor"), _hoverRefresh ? 255 : 215);
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

    private void ApplyHintBrush() =>
        _hintText.Foreground = Ui.Brush(this, Ui.ToneBrush(_hintTone));
}
