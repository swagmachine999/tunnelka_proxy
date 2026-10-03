namespace Tunnelka.UI.Controls;

public abstract class ThemedControl : Control
{
    private float _hover;

    protected ThemedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected float W => Width / Theme.S;
    protected float H => Height / Theme.S;

    protected bool IsHovered { get; private set; }

    protected float Hover => _hover;

    protected virtual Color Background => Theme.Surface;

    protected abstract void Draw(Graphics g);

    protected void Animate() => Animator.Start(this);

    protected virtual bool AnimateMore() => false;

    internal bool StepAnimation()
    {
        var moving = Animator.Approach(ref _hover, IsHovered ? 1 : 0);
        moving |= AnimateMore();
        if (moving)
            Invalidate();
        return moving;
    }

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
        Animate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        IsHovered = false;
        Invalidate();
        Animate();
    }
}
