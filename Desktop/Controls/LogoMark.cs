using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Tunnelka.Desktop.Kitten;

namespace Tunnelka.Desktop.Controls;

public sealed class LogoMark : Control
{
    private const double Side = 44;
    private const double Radius = 13;

    public LogoMark()
    {
        Width = Side;
        Height = Side;
        IsHitTestVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Ui.Color(this, "PinkColor"), 0),
                new GradientStop(Ui.Color(this, "AccentColor"), 1)
            }
        };

        context.DrawRectangle(gradient, null, new Rect(0, 0, Side - 1, Side - 1), Radius, Radius);
        KittenPainter.DrawFace(context, new Rect(5, 6, Side - 10, Side - 11));
    }
}
