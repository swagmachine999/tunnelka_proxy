using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Tunnelka.Next.Controls;

internal sealed class HeroBusyDots : Control
{
    private const double Dot = 7;
    private float _time;
    private Color _first = Colors.Gray;
    private Color _second = Colors.Gray;

    public HeroBusyDots()
    {
        Width = 40;
        Height = 22;
        IsHitTestVisible = false;
    }

    public void SetColors(Color first, Color second)
    {
        _first = first;
        _second = second;
        InvalidateVisual();
    }

    public void Tick(float time)
    {
        _time = time;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var left = Bounds.Width / 2 - 16;
        var cy = Bounds.Height / 2;
        var step = Dot * 1.85;
        for (var i = 0; i < 3; i++)
        {
            var phase = Math.Max(0, Math.Sin(_time * 6 - i * 0.9));
            var color = i == 1 ? _second : _first;
            var brush = new ImmutableSolidColorBrush(Color.FromArgb((byte)(170 + 85 * phase), color.R, color.G, color.B));
            var x = left + i * step;
            var y = cy - Dot / 2 - phase * Dot * 0.55;
            context.DrawEllipse(brush, null, new Point(x + Dot / 2, y + Dot / 2), Dot / 2, Dot / 2);
        }
    }
}
