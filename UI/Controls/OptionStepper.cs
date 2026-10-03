using System.Drawing.Drawing2D;

namespace Tunnelka.UI.Controls;

public class OptionStepper : ThemedControl
{
    private readonly int[] _values;
    private readonly Func<int, string> _label;
    private int _index;
    private int _hover;

    public event EventHandler? ValueChanged;

    public OptionStepper(int[] values, Func<int, string> label, int value, int width = 132)
    {
        _values = values;
        _label = label;
        _index = Nearest(value);
        Size = new Size(Theme.Px(width), Theme.Px(34));
        Cursor = Cursors.Hand;
    }

    public int Value => _values[_index];

    public void SetValue(int value)
    {
        var index = Nearest(value);
        if (index == _index)
            return;

        _index = index;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Step(int direction) => SetValue(_values[Math.Max(0, Math.Min(_values.Length - 1, _index + direction))]);

    private int Nearest(int value)
    {
        var best = 0;
        for (var i = 1; i < _values.Length; i++)
        {
            if (Math.Abs(_values[i] - value) < Math.Abs(_values[best] - value))
                best = i;
        }
        return best;
    }

    private RectangleF Previous => new(0, 0, H, H);
    private RectangleF Next => new(W - H, 0, H, H);

    protected override Color Background => Parent?.BackColor ?? Theme.Card;

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Surface, rect, 10);
        Theme.DrawRounded(g, Theme.Border, rect, 10);

        DrawArrow(g, Previous, false, _index > 0, _hover == -1);
        DrawArrow(g, Next, true, _index < _values.Length - 1, _hover == 1);
        Theme.DrawText(g, _label(Value), Theme.BodyBold, Theme.Text, new RectangleF(H, 0, W - H * 2, H), StringAlignment.Center);
    }

    private static void DrawArrow(Graphics g, RectangleF r, bool next, bool enabled, bool hover)
    {
        var inner = RectangleF.Inflate(r, -4, -4);
        if (hover && enabled)
            Theme.FillRounded(g, Color.FromArgb(70, Theme.Accent), inner, 8);

        using var pen = Theme.IconPen(enabled ? Theme.AccentStrong : Theme.TrackOff);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var dx = next ? 2.5f : -2.5f;
        g.DrawLines(pen, new[] { new PointF(cx - dx, cy - 5), new PointF(cx + dx, cy), new PointF(cx - dx, cy + 5) });
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = Theme.Design(e.Location);
        var hover = Previous.Contains(point) ? -1 : Next.Contains(point) ? 1 : 0;
        if (hover != _hover)
        {
            _hover = hover;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = 0;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var point = Theme.Design(e.Location);
        var index = Previous.Contains(point) ? _index - 1 : Next.Contains(point) ? _index + 1 : _index;
        if (index < 0 || index >= _values.Length || index == _index)
            return;

        _index = index;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}
