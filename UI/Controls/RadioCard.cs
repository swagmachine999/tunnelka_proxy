namespace Tunnelka.UI.Controls;

public class RadioCard : ThemedControl
{
    private readonly string _title;
    private readonly string _subtitle;
    private bool _checked;

    public event EventHandler? Selected;

    public RadioCard(string title, string subtitle)
    {
        _title = title;
        _subtitle = subtitle;
        Dock = DockStyle.Top;
        Height = Theme.Px(66);
        Cursor = Cursors.Hand;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            Invalidate();
        }
    }

    protected override Color Background => Theme.Surface;

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(Theme.ShadowSide, 1, W - Theme.ShadowSide * 2, H - 7);
        var fill = _checked ? Theme.CardSelected : Theme.Blend(Theme.Card, Theme.CardHover, Hover);
        Theme.DrawCard(g, rect, 14, fill, _checked ? Theme.Accent : Theme.Border);

        var cy = rect.Y + rect.Height / 2;
        using (var pen = new Pen(_checked ? Theme.Accent : Theme.TextMuted, 1.8f))
            g.DrawEllipse(pen, rect.X + 14, cy - 9, 18, 18);
        if (_checked)
        {
            using var dot = new SolidBrush(Theme.Accent);
            g.FillEllipse(dot, rect.X + 18.5f, cy - 4.5f, 9, 9);
        }

        var x = rect.X + 44;
        Theme.DrawText(g, _title, Theme.BodyBold, Theme.Text, new RectangleF(x, rect.Y + 8, rect.Right - x - 12, 22));
        Theme.DrawText(g, _subtitle, Theme.Caption, Theme.TextMuted, new RectangleF(x, rect.Y + 30, rect.Right - x - 12, 20));
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (_checked)
            return;

        Checked = true;
        Selected?.Invoke(this, EventArgs.Empty);
    }
}
