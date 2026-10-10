using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Tunnelka.Next;

public sealed class ServerBusyDots : Control
{
    private const double Size = 6;

    private readonly DispatcherTimer _frame = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly DateTime _start = DateTime.Now;
    private bool _running;

    public ServerBusyDots()
    {
        Width = 30;
        Height = 24;
        IsHitTestVisible = false;
        _frame.Tick += (_, _) => InvalidateVisual();
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
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
        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _frame.Stop();
    }

    public override void Render(DrawingContext context)
    {
        var time = (DateTime.Now - _start).TotalSeconds;
        var accent = Ui.Color(this, "AccentColor");
        var pink = Ui.Color(this, "PinkColor");
        var step = Size * 1.85;
        var cy = Bounds.Height / 2;
        for (var i = 0; i < 3; i++)
        {
            var phase = Math.Max(0, Math.Sin(time * 6 - i * 0.9));
            var color = Color.FromArgb((byte)(170 + 85 * phase), i == 1 ? pink.R : accent.R, i == 1 ? pink.G : accent.G, i == 1 ? pink.B : accent.B);
            var center = new Point(i * step + Size / 2, cy - phase * Size * 0.55);
            context.DrawEllipse(new SolidColorBrush(color), null, center, Size / 2, Size / 2);
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
