using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Tunnelka.Models;
using Tunnelka.UI;

namespace Tunnelka.Next;

public sealed class OverlayState
{
    public bool Connected { get; set; }
    public int? PingMs { get; set; }
    public int Loss { get; set; }
    public long Down { get; set; }
    public long Up { get; set; }
}

public sealed class OverlayWindow : Window
{
    private const int EdgeGap = 16;

    private readonly OverlayView _view = new();

    public OverlayWindow()
    {
        SystemDecorations = SystemDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        CanResize = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        WindowStartupLocation = WindowStartupLocation.Manual;
        Title = "Tunnelka";
        Content = _view;
    }

    public void ShowState(OverlayState state, OverlayOptions options)
    {
        var size = _view.Apply(state, options);
        Width = size.Width;
        Height = size.Height;
        PlaceAt(options.Corner, size);
        if (!IsVisible)
        {
            Show();
            Win32.ClickThrough(this);
            PlaceAt(options.Corner, size);
        }
    }

    private void PlaceAt(OverlayCorner corner, Size size)
    {
        var screen = Screens.Primary;
        var area = screen?.Bounds ?? new PixelRect(0, 0, 1920, 1080);
        var scale = screen?.Scaling ?? 1.0;
        var margin = (int)Math.Round(EdgeGap * scale);
        var width = (int)Math.Ceiling(size.Width * scale);
        var height = (int)Math.Ceiling(size.Height * scale);
        var left = corner is OverlayCorner.TopLeft or OverlayCorner.BottomLeft;
        var top = corner is OverlayCorner.TopLeft or OverlayCorner.TopRight;
        Position = new PixelPoint(
            left ? area.X + margin : area.Right - margin - width,
            top ? area.Y + margin : area.Bottom - margin - height);
    }

    private sealed class OverlayView : Control
    {
        private static readonly Color Back = Color.FromArgb(215, 24, 21, 36);
        private static readonly Color Edge = Color.FromArgb(60, 255, 255, 255);
        private static readonly Color Text = Color.FromArgb(255, 240, 240, 248);
        private static readonly Color Muted = Color.FromArgb(255, 160, 156, 180);
        private static readonly Color Good = Color.FromArgb(255, 96, 214, 140);
        private static readonly Color Mid = Color.FromArgb(255, 246, 198, 80);
        private static readonly Color Bad = Color.FromArgb(255, 242, 98, 112);
        private static readonly Color Divider = Color.FromArgb(50, 255, 255, 255);
        private static readonly FontFamily Family = new("Segoe UI");

        private List<Part> _parts = new();
        private double _k = 1;
        private double _pad;
        private double _gap;
        private double _dot;
        private double _dotWidth;

        public Size Apply(OverlayState state, OverlayOptions options)
        {
            _k = Math.Max(0.5, options.Scale / 100.0);
            var semi = FontWeight.SemiBold;
            var parts = new List<Part>();
            if (!state.Connected)
                parts.Add(Make(L.T("VPN выключен"), Muted, 12 * _k, FontWeight.Normal));
            if (options.ShowPing)
                parts.Add(Make(state.PingMs is { } ms ? L.F("{0} мс", ms) : "— " + L.T("мс"), Text, 14 * _k, semi, PingColor(state.PingMs)));
            if (options.ShowSpeed)
            {
                parts.Add(Make("↓ " + ServerText.Bytes(state.Down) + L.T("/с"), Text, 14 * _k, semi, null, true));
                parts.Add(Make("↑ " + ServerText.Bytes(state.Up) + L.T("/с"), Text, 14 * _k, semi));
            }
            if (options.ShowLoss)
                parts.Add(Make(L.F("потери {0}%", state.Loss), state.Loss == 0 ? Muted : state.Loss < 6 ? Mid : Bad, 14 * _k, semi));

            _parts = parts;
            _pad = 12 * _k;
            _gap = 14 * _k;
            _dot = 8 * _k;
            _dotWidth = _dot + 6 * _k;

            var width = _pad * 2 + _parts.Sum(p => p.Text.Width + (p.Dot != null ? _dotWidth : 0)) + _gap * Math.Max(0, _parts.Count - 1) + 2;
            var height = Math.Ceiling(34 * _k);
            var size = new Size(Math.Ceiling(width), height);
            Width = size.Width;
            Height = size.Height;
            InvalidateVisual();
            return size;
        }

        public override void Render(DrawingContext context)
        {
            var width = Bounds.Width;
            var height = Bounds.Height;
            var rect = new Rect(0.5, 0.5, Math.Max(0, width - 1), Math.Max(0, height - 1));
            context.DrawRectangle(new SolidColorBrush(Back), new Pen(new SolidColorBrush(Edge), 1), rect, rect.Height / 2, rect.Height / 2);

            var x = _pad;
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                var textX = x;
                if (part.Dot is { } dot)
                {
                    var center = new Point(x + _dot / 2, height / 2);
                    context.DrawEllipse(new SolidColorBrush(dot), null, center, _dot / 2, _dot / 2);
                    textX += _dotWidth;
                }

                context.DrawText(part.Text, new Point(textX, (height - part.Text.Height) / 2));
                x += part.Text.Width + (part.Dot != null ? _dotWidth : 0) + _gap;
                if (i < _parts.Count - 1 && !part.Joined)
                {
                    var line = new Pen(new SolidColorBrush(Divider), 1);
                    context.DrawLine(line, new Point(x - _gap / 2, height * 0.3), new Point(x - _gap / 2, height * 0.7));
                }
            }
        }

        private static Part Make(string text, Color color, double size, FontWeight weight, Color? dot = null, bool joined = false)
        {
            var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(Family, FontStyle.Normal, weight), size, new SolidColorBrush(color));
            return new Part(formatted, dot, joined);
        }

        private static Color PingColor(int? ms) => ms switch
        {
            null => Bad,
            < 150 => Good,
            < 400 => Mid,
            _ => Bad
        };

        private sealed record Part(FormattedText Text, Color? Dot, bool Joined);
    }
}
