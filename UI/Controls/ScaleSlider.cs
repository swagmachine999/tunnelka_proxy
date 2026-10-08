namespace Tunnelka.UI.Controls;

public class ScaleSlider : ThemedControl
{
    private const float ThumbRadius = 9;
    private const float TrackHeight = 6;
    private const float LabelWidth = 46;
    private static readonly SliderRange Range = new(50, 200, 5);

    private int _value;
    private bool _drag;
    private bool _hover;

    public event EventHandler? ValueChanged;

    public ScaleSlider(int value, int width = 180)
    {
        _value = Range.Snap(value);
        Size = new Size(Theme.Px(width), Theme.Px(34));
        Cursor = Cursors.Hand;
    }

    public int Value => _value;

    public void SetValue(int value)
    {
        var next = Range.Snap(value);
        if (next == _value)
            return;

        _value = next;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Step(int direction) => SetValue(_value + direction * Range.Step);

    protected override Color Background => Parent?.BackColor ?? Theme.Card;

    private float TrackLeft => ThumbRadius + 2;

    private float TrackRight => W - LabelWidth - ThumbRadius;

    private float ThumbX => TrackLeft + (TrackRight - TrackLeft) * Range.ToFraction(_value);

    protected override void Draw(Graphics g)
    {
        var cy = H / 2;
        var track = new RectangleF(TrackLeft, cy - TrackHeight / 2, TrackRight - TrackLeft, TrackHeight);
        Theme.FillRounded(g, Theme.TrackOff, track, TrackHeight / 2);

        var filled = new RectangleF(track.X, track.Y, Math.Max(TrackHeight, ThumbX - track.X), track.Height);
        Theme.FillRounded(g, Theme.Accent, filled, TrackHeight / 2);

        var radius = _drag || _hover ? ThumbRadius + 1 : ThumbRadius;
        var thumb = new RectangleF(ThumbX - radius, cy - radius, radius * 2, radius * 2);
        Theme.DrawShadow(g, thumb, radius);
        using (var fill = new SolidBrush(Theme.Card))
            g.FillEllipse(fill, thumb);
        using (var ring = new Pen(Theme.AccentStrong, 2))
            g.DrawEllipse(ring, thumb);

        Theme.DrawText(g, _value + "%", Theme.BodyBold, Theme.Text, new RectangleF(W - LabelWidth, 0, LabelWidth, H), StringAlignment.Far);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        _drag = true;
        Capture = true;
        Follow(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hover = Math.Abs(Theme.Design(e.Location).X - ThumbX) <= ThumbRadius + 3;
        if (hover != _hover)
        {
            _hover = hover;
            Invalidate();
        }

        if (_drag)
            Follow(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _drag = false;
        Capture = false;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    private void Follow(int x)
    {
        var design = Theme.Design(new Point(x, 0)).X;
        SetValue(Range.FromFraction((design - TrackLeft) / (TrackRight - TrackLeft)));
    }
}
