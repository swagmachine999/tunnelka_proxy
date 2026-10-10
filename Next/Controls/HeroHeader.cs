using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using static Tunnelka.Next.Controls.HeroStyle;

namespace Tunnelka.Next.Controls;

internal sealed class HeroHeader : Grid
{
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

    private bool _hoverToggle;
    private bool _tun;

    public HeroHeader()
    {
        Height = HeroGeometry.HeaderHeight;
        Children.Add(BuildSpeedChip());
        Children.Add(BuildToggle());
    }

    public event Action<bool>? ModeSelected;

    public void SetProxyText(string text) => _proxyLabel.Text = text;

    public void SetSpeed(string? down, string? up)
    {
        _speedChip.IsVisible = down != null && up != null;
        _downText.Text = down ?? "";
        _upText.Text = up ?? "";
    }

    public void SetMode(bool tun)
    {
        _tun = tun;
        ApplySegment(_proxySegment, _proxyLabel, !tun);
        ApplySegment(_tunSegment, _tunLabel, tun);
    }

    public void ApplyColors()
    {
        var border = Ui.Brush(this, "BorderBrush2");
        var text = Ui.Brush(this, "TextBrush");
        _speedChip.Background = Solid(Ui.Color(this, "CardColor"), 225);
        _speedChip.BorderBrush = border;
        _toggle.BorderBrush = border;
        _downArrow.Stroke = Ui.Brush(this, "AccentStrongBrush");
        _upArrow.Stroke = Ui.Brush(this, "PingBadBrush");
        _downText.Foreground = text;
        _upText.Foreground = text;
        ApplyToggleBackground();
        SetMode(_tun);
    }

    private Control BuildSpeedChip()
    {
        ConfigureArrow(_downArrow, "M5,0 L5,12 M1,7.5 L5,12 L9,7.5");
        ConfigureArrow(_upArrow, "M5,0 L5,12 M1,4.5 L5,0 L9,4.5");
        StyleText(_downText, BodyFont).Margin = new Thickness(0, 0, 10, 0);
        StyleText(_upText, BodyFont);
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
        return _speedChip;
    }

    private static void ConfigureArrow(Avalonia.Controls.Shapes.Path arrow, string data)
    {
        arrow.Data = Geometry.Parse(data);
        arrow.Width = 10;
        arrow.Height = 12;
        arrow.Stretch = Stretch.None;
        arrow.StrokeThickness = 1.8;
        arrow.StrokeLineCap = PenLineCap.Round;
        arrow.StrokeJoin = PenLineJoin.Round;
        arrow.VerticalAlignment = VerticalAlignment.Center;
        arrow.Margin = new Thickness(0, 0, 5, 0);
    }

    private Control BuildToggle()
    {
        StyleSegment(_proxySegment, _proxyLabel, false);
        StyleSegment(_tunSegment, _tunLabel, true);
        _tunLabel.Text = "TUN";
        var segments = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(3) };
        SetColumn(_tunSegment, 1);
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
        _toggle.PointerEntered += (_, _) => SetToggleHover(true);
        _toggle.PointerExited += (_, _) => SetToggleHover(false);
        return _toggle;
    }

    private void StyleSegment(Border segment, TextBlock label, bool tun)
    {
        StyleText(label, BodyFont);
        segment.CornerRadius = new CornerRadius(16);
        segment.Background = Brushes.Transparent;
        segment.Cursor = new Cursor(StandardCursorType.Hand);
        segment.Child = label;
        segment.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
                ModeSelected?.Invoke(tun);
        };
    }

    private void SetToggleHover(bool hover)
    {
        _hoverToggle = hover;
        ApplyToggleBackground();
    }

    private void ApplyToggleBackground() =>
        _toggle.Background = Solid(Ui.Color(this, "CardColor"), _hoverToggle ? 250 : 215);

    private void ApplySegment(Border segment, TextBlock label, bool active)
    {
        segment.Background = active ? Ui.Brush(this, "AccentBrush") : Brushes.Transparent;
        label.Foreground = active ? Brushes.White : Ui.Brush(this, "TextMutedBrush");
    }
}
