namespace VpnClient.UI.Controls;

public class ScaleStepper : ThemedControl
{
    public const int Min = 60;
    public const int Max = 130;
    public const int Step = 10;

    private int _value;
    private int _hover;

    public event EventHandler? ValueChanged;

    public ScaleStepper(int value)
    {
        _value = value;
        Size = new Size(Theme.Px(132), Theme.Px(34));
        Cursor = Cursors.Hand;
    }

    public int Value => _value;

    public void SetValue(int value)
    {
        value = Math.Max(Min, Math.Min(Max, value));
        if (value == _value)
            return;

        _value = value;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private RectangleF Minus => new(0, 0, H, H);
    private RectangleF Plus => new(W - H, 0, H, H);

    protected override Color Background => Parent?.BackColor ?? Theme.Card;

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Surface, rect, 10);
        Theme.DrawRounded(g, Theme.Border, rect, 10);

        DrawButton(g, Minus, false, _value > Min, _hover == -1);
        DrawButton(g, Plus, true, _value < Max, _hover == 1);
        Theme.DrawText(g, $"{_value}%", Theme.BodyBold, Theme.Text, new RectangleF(H, 0, W - H * 2, H), StringAlignment.Center);
    }

    private static void DrawButton(Graphics g, RectangleF r, bool plus, bool enabled, bool hover)
    {
        var inner = RectangleF.Inflate(r, -4, -4);
        if (hover && enabled)
            Theme.FillRounded(g, Color.FromArgb(70, Theme.Accent), inner, 8);

        using var pen = new Pen(enabled ? Theme.AccentStrong : Theme.TrackOff, 2f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        g.DrawLine(pen, cx - 5, cy, cx + 5, cy);
        if (plus)
            g.DrawLine(pen, cx, cy - 5, cx, cy + 5);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = Theme.Design(e.Location);
        var hover = Minus.Contains(point) ? -1 : Plus.Contains(point) ? 1 : 0;
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
        var next = Minus.Contains(point) ? _value - Step : Plus.Contains(point) ? _value + Step : _value;
        next = Math.Max(Min, Math.Min(Max, next));
        if (next == _value)
            return;

        _value = next;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}
