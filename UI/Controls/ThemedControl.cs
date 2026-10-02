namespace Tunnelka.UI.Controls;

public abstract class ThemedControl : Control
{
    protected ThemedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected float W => Width / Theme.S;
    protected float H => Height / Theme.S;

    protected bool IsHovered { get; private set; }

    protected virtual Color Background => Theme.Surface;

    protected abstract void Draw(Graphics g);

    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.Begin(e.Graphics, Background);
        Draw(e.Graphics);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        IsHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        IsHovered = false;
        Invalidate();
    }
}
