using Tunnelka.Models;

namespace Tunnelka.UI.Controls;

public class CornerPicker : ThemedControl
{
    private const float Inset = 6;
    private const float Spot = 24;
    private const float SpotHeight = 12;

    private OverlayCorner _corner;
    private OverlayCorner? _hover;

    public event EventHandler? CornerChanged;

    public CornerPicker(OverlayCorner corner)
    {
        _corner = corner;
        Size = new Size(Theme.Px(100), Theme.Px(54));
        Cursor = Cursors.Hand;
    }

    public OverlayCorner Corner => _corner;

    protected override Color Background => Parent?.BackColor ?? Theme.Card;

    private RectangleF SpotRect(OverlayCorner corner)
    {
        var left = corner is OverlayCorner.TopLeft or OverlayCorner.BottomLeft;
        var top = corner is OverlayCorner.TopLeft or OverlayCorner.TopRight;
        return new RectangleF(left ? Inset : W - Inset - Spot - 1, top ? Inset : H - Inset - SpotHeight - 1, Spot, SpotHeight);
    }

    protected override void Draw(Graphics g)
    {
        var screen = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Surface, screen, 8);
        Theme.DrawRounded(g, Theme.Border, screen, 8);

        foreach (var corner in Enum.GetValues(typeof(OverlayCorner)).Cast<OverlayCorner>())
        {
            var spot = SpotRect(corner);
            var selected = corner == _corner;
            var color = selected ? Theme.Accent : corner == _hover ? Theme.Blend(Theme.TrackOff, Theme.Accent, 0.45f) : Theme.TrackOff;
            Theme.FillRounded(g, color, spot, SpotHeight / 2);
        }
    }

    private OverlayCorner? CornerAt(Point location)
    {
        var point = Theme.Design(location);
        return point.X < W / 2
            ? point.Y < H / 2 ? OverlayCorner.TopLeft : OverlayCorner.BottomLeft
            : point.Y < H / 2 ? OverlayCorner.TopRight : OverlayCorner.BottomRight;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var corner = CornerAt(e.Location);
        if (corner == _hover)
            return;

        _hover = corner;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = null;
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (CornerAt(e.Location) is not { } corner || corner == _corner)
            return;

        _corner = corner;
        Invalidate();
        CornerChanged?.Invoke(this, EventArgs.Empty);
    }
}
