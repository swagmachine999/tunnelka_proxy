namespace Tunnelka.UI.Controls.ServerList;

public abstract class ListRow
{
    public float Top { get; set; }

    public float Extent { get; set; }

    public bool Shown { get; set; } = true;

    public event Action? Changed;

    public abstract float Measure(float width);

    public abstract void Draw(Graphics g, float width, float time);

    public virtual bool Step() => false;

    public virtual bool NeedsFrames => false;

    public virtual void PointerMoved(PointF local)
    {
    }

    public virtual void PointerLeft()
    {
    }

    public virtual bool Hits(PointF local) => false;

    public virtual void Click(PointF local)
    {
    }

    public virtual void DoubleClick(PointF local)
    {
    }

    public virtual void RightClick(PointF local)
    {
    }

    protected void RaiseChanged() => Changed?.Invoke();
}
