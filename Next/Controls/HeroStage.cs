using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Tunnelka.Next.Kitten;

namespace Tunnelka.Next.Controls;

internal sealed class HeroStage : Control
{
    private const int RippleCount = 5;
    private const double RippleDelay = 0.3;
    private const double RippleLife = 2.6;
    private const double RippleReach = 132;
    private const double HoverSettled = 0.02;
    private const double ShakeLength = 0.55;
    private const double StatusSize = 15.4;
    private const double TimerSize = 20.9;

    private static readonly Typeface Face = new(new FontFamily("Segoe UI"), FontStyle.Normal, FontWeight.SemiBold);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
    private static double _digitWidth;
    private static double _colonWidth;

    private readonly FramePacer _pacer = new();
    private readonly KittenBrain _brain = new();
    private readonly Dictionary<(string, double, IBrush), FormattedText> _texts = new();
    private KittenPose _pose = new();
    private float _time;
    private float _rippleStart = -100;
    private float _shakeStart = -100;
    private double _hoverAmount;
    private bool _hoverPower;
    private bool _connected;
    private bool _connecting;
    private string _elapsed = "00:00:00";
    private string _connectedText = "";
    private string _connectingText = "";
    private string _disconnectedText = "";

    private ThemeVariant? _variant;
    private bool _dark;
    private IBrush _heroBrush = Brushes.Transparent;
    private Color _accent;
    private Color _accentStrong;
    private Color _pink;
    private Color _powerOff;
    private IBrush _mutedBrush = Brushes.Gray;
    private IBrush _accentStrongBrush = Brushes.Gray;
    private IBrush _borderBrush = Brushes.Gray;
    private IBrush _rimBrush = Brushes.Gray;
    private IBrush _iconOnBrush = Brushes.Gray;
    private IBrush _iconOffBrush = Brushes.Gray;
    private IBrush _glowPink = Brushes.Transparent;
    private IBrush _glowAccent = Brushes.Transparent;
    private IBrush _bodyBrush = Brushes.Gray;
    private Color _spinnerFromColor;
    private Color _spinnerToColor;

    private (double, double, double) _iconKey;
    private StreamGeometry? _icon;

    public HeroStage()
    {
        ClipToBounds = true;
    }

    public event Action? PowerClicked;

    public bool Connected
    {
        get => _connected;
        set => _connected = value;
    }

    public bool Connecting
    {
        get => _connecting;
        set => _connecting = value;
    }

    public string Elapsed
    {
        get => _elapsed;
        set => _elapsed = value;
    }

    public float Time => _time;

    private bool Rippling => _time - _rippleStart < RippleDelay * (RippleCount - 1) + RippleLife;

    private bool Shaking => _time - _shakeStart < ShakeLength;

    public void SetTexts(string connecting, string connected, string disconnected)
    {
        _connectingText = connecting;
        _connectedText = connected;
        _disconnectedText = disconnected;
    }

    public void Fail()
    {
        _brain.Fail(_time);
        _shakeStart = _time;
    }

    public void Click() => _brain.Click(_time);

    public void LookAway()
    {
        _brain.LookAway();
        SetHover(false);
    }

    public void PointerAt(Point point)
    {
        var geometry = HeroGeometry.Compute(Bounds.Width, Bounds.Height);
        _brain.Look((float)((point.X - geometry.Kitten.Center.X) / 160.0), (float)((point.Y - geometry.Kitten.Center.Y) / 120.0));
        SetHover(InCircle(geometry.Power, point));
    }

    private void SetHover(bool hover)
    {
        if (hover == _hoverPower)
            return;

        _hoverPower = hover;
        Cursor = hover ? HandCursor : Cursor.Default;
    }

    public int Step(float time, bool busy)
    {
        _time = time;
        _hoverAmount += ((_hoverPower ? 1.0 : 0.0) - _hoverAmount) * 0.15;
        var active = _connected || _connecting || _hoverPower || _hoverAmount > HoverSettled || busy;
        var interval = _pacer.IntervalMs(Rippling || Shaking, active);
        _pose = _brain.Evaluate(time, _connected, _connecting);
        InvalidateVisual();
        return interval;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        var geometry = HeroGeometry.Compute(Bounds.Width, Bounds.Height);
        if (!InCircle(geometry.Power, e.GetPosition(this)))
            return;

        _rippleStart = _time;
        _brain.Press(_time);
        PowerClicked?.Invoke();
    }

    private static bool InCircle(Rect rect, Point point)
    {
        var dx = point.X - rect.Center.X;
        var dy = point.Y - rect.Center.Y;
        return dx * dx + dy * dy <= rect.Width * rect.Width / 4;
    }

    private static Rect Inflate(Rect rect, double by) =>
        new(rect.X - by, rect.Y - by, rect.Width + by * 2, rect.Height + by * 2);

    private static byte Alpha(double value) => (byte)Math.Max(0, Math.Min(255, (int)value));

    private static Color WithAlpha(Color color, double alpha) => Color.FromArgb(Alpha(alpha), color.R, color.G, color.B);

    private static IBrush Solid(Color color, double alpha) => new ImmutableSolidColorBrush(WithAlpha(color, alpha));

    private static Color Lighten(Color color, double amount) => Color.FromArgb(
        color.A,
        (byte)(color.R + (255 - color.R) * amount),
        (byte)(color.G + (255 - color.G) * amount),
        (byte)(color.B + (255 - color.B) * amount));

    private static IBrush Vertical(Color top, Color bottom) => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = { new GradientStop(top, 0), new GradientStop(bottom, 1) }
    };

    private static IBrush Glow(Color center, double alpha) => new RadialGradientBrush
    {
        GradientStops = { new GradientStop(WithAlpha(center, alpha), 0), new GradientStop(WithAlpha(center, 0), 1) }
    };

    private static void Oval(DrawingContext context, IBrush? brush, IPen? pen, Rect rect) =>
        context.DrawEllipse(brush, pen, rect.Center, rect.Width / 2, rect.Height / 2);

    private void EnsurePalette()
    {
        if (_variant != null && Equals(_variant, ActualThemeVariant))
            return;

        _variant = ActualThemeVariant;
        _dark = Equals(ActualThemeVariant, ThemeVariant.Dark);
        _heroBrush = Ui.Brush(this, "HeroBrush");
        _accent = Ui.Color(this, "AccentColor");
        _accentStrong = Ui.Color(this, "AccentStrongColor");
        _pink = Ui.Color(this, "PinkColor");
        _powerOff = Ui.Color(this, "PowerOffColor");
        _mutedBrush = new ImmutableSolidColorBrush(Ui.Color(this, "TextMutedColor"));
        _accentStrongBrush = new ImmutableSolidColorBrush(_accentStrong);
        _borderBrush = new ImmutableSolidColorBrush(Ui.Color(this, "BorderColor"));
        _rimBrush = Vertical(WithAlpha(_pink, 220), WithAlpha(_accent, 220));
        _iconOnBrush = Vertical(_pink, _accent);
        _iconOffBrush = Solid(_accent, 190);
        _glowPink = Glow(_pink, _dark ? 45 : 90);
        _glowAccent = Glow(_accent, _dark ? 45 : 90);
        _bodyBrush = _dark
            ? Vertical(Lighten(_powerOff, 0.07), _powerOff)
            : Vertical(Colors.White, Color.FromRgb(248, 244, 253));
        _spinnerFromColor = _pink;
        _spinnerToColor = _accent;
        _texts.Clear();
        _icon = null;
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width < 10 || height < 10)
            return;

        EnsurePalette();
        var geometry = HeroGeometry.Compute(width, height);
        var area = new Rect(0, 0, width, height);

        context.DrawRectangle(_heroBrush, null, area);
        Oval(context, _glowPink, null, new Rect(width * 0.55, -height * 0.25, width * 0.8, width * 0.8));
        Oval(context, _glowAccent, null, new Rect(-width * 0.35, height * 0.45, width * 0.9, width * 0.9));

        DrawRipples(context, geometry);
        DrawPower(context, geometry);
        KittenPainter.Draw(context, geometry.Kitten, _pose, _time, _accent, _accentStrong, _pink);
    }

    private void DrawRipples(DrawingContext context, HeroGeometry geometry)
    {
        if (!Rippling)
            return;

        var color = _connected ? _pink : _accent;
        var elapsed = _time - _rippleStart;
        for (var i = 0; i < RippleCount; i++)
        {
            var t = (elapsed - i * RippleDelay) / RippleLife;
            if (t <= 0 || t >= 1)
                continue;

            var eased = 1 - (1 - t) * (1 - t);
            var alpha = 110 * Math.Min(1.0, t / 0.12) * Math.Pow(1 - t, 1.6);
            var ring = Inflate(geometry.Power, 4 + eased * RippleReach * geometry.Scale);
            Oval(context, null, new Pen(Solid(color, alpha * 0.25), 9), ring);
            Oval(context, null, new Pen(Solid(color, alpha), 1.2 + 1.4 * (1 - t)), ring);
        }
    }

    private double ShakeOffset()
    {
        var t = _time - _shakeStart;
        if (t < 0 || t > ShakeLength)
            return 0;

        return Math.Sin(t * 60) * 9 * (1 - t / ShakeLength);
    }

    private void DrawPower(DrawingContext context, HeroGeometry geometry)
    {
        var r = geometry.Power;
        using var shake = context.PushTransform(Matrix.CreateTranslation(ShakeOffset(), 0));

        var pulse = _connected ? (Math.Sin(_time * 2.2) + 1) / 2 : 0;
        var hoverPulse = _hoverAmount * (Math.Sin(_time * 4.5) + 1) / 2;
        var ringColor = _connected ? _pink : _accent;

        for (var i = 0; i < 3; i++)
        {
            var alpha = (_connected ? 80 - i * 24 : 40 - i * 12) + (int)(hoverPulse * 30);
            Oval(context, null, new Pen(Solid(ringColor, Math.Min(255, alpha)), 2), Inflate(r, 13 + i * 14 + pulse * 6 + hoverPulse * 5));
        }

        Oval(context, Glow(ringColor, (_connected ? 80 : 45) + (int)(hoverPulse * 50)), null, Inflate(r, 26 + hoverPulse * 8));

        var body = Inflate(r, hoverPulse * 4);
        Oval(context, Solid(Colors.White, _dark ? 16 : 110), null, Inflate(body, 18));
        Oval(context, _bodyBrush, null, body);

        var shadow = _dark ? Colors.Black : Color.FromRgb(120, 90, 170);
        var depth = _dark ? 70.0 : 22.0;
        using (context.PushGeometryClip(new EllipseGeometry(body)))
        {
            for (var i = 0; i < 14; i++)
            {
                var k = 1 - i / 14.0;
                var ring = Inflate(body, -i * 1.4);
                ring = new Rect(ring.X, ring.Y - 2.5 * k, ring.Width, ring.Height);
                Oval(context, null, new Pen(Solid(shadow, depth * k * k), 2), ring);
            }
        }

        Oval(context, null, _connected ? new Pen(_rimBrush, 1.6) : new Pen(_borderBrush, 1.6), body);

        if (_hoverAmount > 0.01)
            Oval(context, Solid(_accent, _hoverAmount * 14), null, body);

        DrawIcon(context, r);

        var cx = r.X + r.Width / 2;
        if (_connecting)
        {
            DrawSpinner(context, body);
            DrawSpaced(context, _connectingText, cx, r.Y + r.Height * 0.64);
        }
        else if (_connected)
        {
            DrawSpaced(context, _connectedText, cx, r.Y + r.Height * 0.6);
            DrawTimer(context, cx, r.Y + r.Height * 0.6 + 25);
        }
        else
        {
            DrawSpaced(context, _disconnectedText, cx, r.Y + r.Height * 0.64);
        }
    }

    private void DrawIcon(DrawingContext context, Rect r)
    {
        var cx = r.X + r.Width / 2;
        var size = r.Width * 0.2;
        var cy = r.Y + r.Height * (_connected ? 0.38 : 0.42);
        var key = (cx, cy, size);
        if (_icon == null || _iconKey != key)
        {
            var radius = size / 2;
            var start = -60 * Math.PI / 180.0;
            var end = 240 * Math.PI / 180.0;
            var icon = new StreamGeometry();
            using (var g = icon.Open())
            {
                g.BeginFigure(new Point(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius), false);
                g.ArcTo(new Point(cx + Math.Cos(end) * radius, cy + Math.Sin(end) * radius), new Size(radius, radius), 0, true, SweepDirection.Clockwise);
                g.EndFigure(false);
                g.BeginFigure(new Point(cx, cy - size * 0.62), false);
                g.LineTo(new Point(cx, cy - size * 0.08));
                g.EndFigure(false);
            }

            _icon = icon;
            _iconKey = key;
        }

        var pen = new Pen(_connected ? _iconOnBrush : _iconOffBrush, Math.Max(3.0, r.Width * 0.024), lineCap: PenLineCap.Round);
        context.DrawGeometry(null, pen, _icon);
    }

    private void DrawSpinner(DrawingContext context, Rect body)
    {
        var ring = Inflate(body, 7);
        var radiusX = ring.Width / 2;
        var radiusY = ring.Height / 2;
        var center = ring.Center;
        var start = _time * 300 % 360;
        var sweep = 70 + 50 * Math.Sin(_time * 3);
        const int segments = 10;
        var step = sweep / segments;
        for (var i = 0; i < segments; i++)
        {
            var from = (start + step * i) * Math.PI / 180.0;
            var to = (start + step * (i + 1)) * Math.PI / 180.0;
            var t = (double)i / (segments - 1);
            var color = Color.FromRgb(
                (byte)(_spinnerFromColor.R + (_spinnerToColor.R - _spinnerFromColor.R) * t),
                (byte)(_spinnerFromColor.G + (_spinnerToColor.G - _spinnerFromColor.G) * t),
                (byte)(_spinnerFromColor.B + (_spinnerToColor.B - _spinnerFromColor.B) * t));
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(new Point(center.X + Math.Cos(from) * radiusX, center.Y + Math.Sin(from) * radiusY), false);
                g.ArcTo(new Point(center.X + Math.Cos(to) * radiusX, center.Y + Math.Sin(to) * radiusY), new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
                g.EndFigure(false);
            }

            context.DrawGeometry(null, new Pen(new ImmutableSolidColorBrush(color), 3.2, lineCap: PenLineCap.Round), geometry);
        }
    }

    private FormattedText Text(string text, double size, IBrush brush)
    {
        var key = (text, size, brush);
        if (!_texts.TryGetValue(key, out var value))
        {
            if (_texts.Count > 300)
                _texts.Clear();

            value = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, size, brush);
            _texts[key] = value;
        }

        return value;
    }

    private void DrawSpaced(DrawingContext context, string text, double cx, double cy)
    {
        if (text.Length == 0)
            return;

        const double tracking = 0.3;
        var glyphs = new FormattedText[text.Length];
        var total = tracking * (text.Length - 1);
        for (var i = 0; i < text.Length; i++)
        {
            glyphs[i] = Text(text[i].ToString(), StatusSize, _mutedBrush);
            total += glyphs[i].Width;
        }

        var x = cx - total / 2;
        foreach (var glyph in glyphs)
        {
            context.DrawText(glyph, new Point(x, cy - glyph.Height / 2));
            x += glyph.Width + tracking;
        }
    }

    private void DrawTimer(DrawingContext context, double cx, double cy)
    {
        if (_digitWidth == 0)
        {
            for (var digit = 0; digit < 10; digit++)
                _digitWidth = Math.Max(_digitWidth, Text(digit.ToString(), TimerSize, _accentStrongBrush).Width);

            _colonWidth = Text(":", TimerSize, _accentStrongBrush).Width + 1;
        }

        var total = 0.0;
        foreach (var c in _elapsed)
            total += c == ':' ? _colonWidth : _digitWidth;

        var x = cx - total / 2;
        foreach (var c in _elapsed)
        {
            var cell = c == ':' ? _colonWidth : _digitWidth;
            var glyph = Text(c.ToString(), TimerSize, _accentStrongBrush);
            context.DrawText(glyph, new Point(x + (cell - glyph.Width) / 2, cy - glyph.Height / 2));
            x += cell;
        }
    }
}
