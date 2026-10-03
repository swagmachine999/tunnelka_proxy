namespace Tunnelka.UI.Controls;

public abstract class ThemedControl : Control
{
    private float _hover;
    private BufferedGraphicsContext? _cacheContext;
    private BufferedGraphics? _cache;
    private Size _cacheSize;
    private bool _cacheValid;

    protected ThemedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.StandardDoubleClick, false);
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

        if (_cache == null || _cacheSize != Size)
        {
            _cache?.Dispose();
            _cacheContext ??= new BufferedGraphicsContext();
            _cacheContext.MaximumBuffer = new Size(Width + 1, Height + 1);
            _cache = _cacheContext.Allocate(e.Graphics, ClientRectangle);
            _cacheSize = Size;
            _cacheValid = false;
        }

        if (!_cacheValid)
        {
            var g = _cache.Graphics;
            g.ResetTransform();
            g.ResetClip();
            Theme.Begin(g, Background);
            Draw(g);
            _cacheValid = true;
        }

        _cache.Render(e.Graphics);
    }

    protected override void OnInvalidated(InvalidateEventArgs e)
    {
        _cacheValid = false;
        base.OnInvalidated(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache?.Dispose();
            _cacheContext?.Dispose();
        }
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
