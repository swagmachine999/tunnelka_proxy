namespace Tunnelka.UI.Controls;

public abstract class ThemedControl : Control
{
    private float _hover;
    private Bitmap? _cache;
    private bool _cacheValid;

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

    protected virtual bool CachePaint => false;

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
        if (!CachePaint || Width <= 0 || Height <= 0)
        {
            Theme.Begin(e.Graphics, Background);
            Draw(e.Graphics);
            return;
        }

        if (_cache == null || _cache.Size != Size)
        {
            _cache?.Dispose();
            _cache = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            _cacheValid = false;
        }

        if (!_cacheValid)
        {
            using var g = Graphics.FromImage(_cache);
            Theme.Begin(g, Background);
            Draw(g);
            _cacheValid = true;
        }

        e.Graphics.DrawImageUnscaled(_cache, 0, 0);
    }

    protected override void OnInvalidated(InvalidateEventArgs e)
    {
        _cacheValid = false;
        base.OnInvalidated(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cache?.Dispose();
        base.Dispose(disposing);
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
