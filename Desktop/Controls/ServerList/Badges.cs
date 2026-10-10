using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Tunnelka.Desktop;

public sealed class ServerBusyDots : Canvas
{
    private const double Size = 7.5;
    private const double Step = Size * 1.85;

    private readonly DispatcherTimer _frame = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly DateTime _start = DateTime.Now;
    private readonly Ellipse[] _dots = new Ellipse[3];
    private bool _running;

    public ServerBusyDots()
    {
        Width = 36;
        Height = 24;
        IsHitTestVisible = false;
        RenderOptions.SetEdgeMode(this, EdgeMode.Antialias);
        for (var i = 0; i < _dots.Length; i++)
        {
            _dots[i] = new Ellipse { Width = Size, Height = Size };
            SetLeft(_dots[i], i * Step);
            Children.Add(_dots[i]);
        }

        Recolor();
        Tick();
        _frame.Tick += (_, _) => Tick();
        ActualThemeVariantChanged += (_, _) => Recolor();
    }

    public bool Running
    {
        get => _running;
        set
        {
            _running = value;
            Update();
        }
    }

    private void Update()
    {
        if (_running && VisualRoot != null)
            _frame.Start();
        else
            _frame.Stop();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Recolor();
        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _frame.Stop();
    }

    private void Recolor()
    {
        var accent = Ui.Color(this, "AccentColor");
        var pink = Ui.Color(this, "PinkColor");
        for (var i = 0; i < _dots.Length; i++)
            _dots[i].Fill = new SolidColorBrush(i == 1 ? pink : accent);
    }

    private void Tick()
    {
        var time = (DateTime.Now - _start).TotalSeconds;
        var middle = Height / 2 - Size / 2;
        for (var i = 0; i < _dots.Length; i++)
        {
            var phase = Math.Max(0, Math.Sin(time * 6 - i * 0.9));
            _dots[i].Opacity = (170 + 85 * phase) / 255;
            SetTop(_dots[i], middle - phase * Size * 0.55);
        }
    }
}

public sealed class ServerFlagBadge : Control
{
    private string? _code;

    public ServerFlagBadge()
    {
        Width = 28;
        Height = 28;
        IsHitTestVisible = false;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public string? Code
    {
        get => _code;
        set
        {
            _code = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var flag = FlagCache.Get(_code);
        if (flag != null)
        {
            context.DrawImage(flag, rect);
            return;
        }

        var accent = Ui.Color(this, "AccentColor");
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(ServerRes.Lighten(accent, 0.3), 0),
                new GradientStop(accent, 1)
            }
        };
        context.DrawEllipse(gradient, null, rect.Center, rect.Width / 2, rect.Height / 2);

        var pen = new Pen(Brushes.White, Math.Max(1.2, rect.Width / 22));
        var inner = new Rect(rect.X + rect.Width * 0.22, rect.Y + rect.Height * 0.22, rect.Width * 0.56, rect.Height * 0.56);
        context.DrawEllipse(null, pen, inner.Center, inner.Width / 2, inner.Height / 2);
        var narrow = new Rect(inner.X + inner.Width * 0.28, inner.Y, inner.Width * 0.44, inner.Height);
        context.DrawEllipse(null, pen, narrow.Center, narrow.Width / 2, narrow.Height / 2);
        context.DrawLine(pen, new Point(inner.X, inner.Y + inner.Height / 2), new Point(inner.Right, inner.Y + inner.Height / 2));
    }
}
