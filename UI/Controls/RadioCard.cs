namespace Tunnelka.UI.Controls;

public class RadioCard : ThemedControl
{
    private readonly string _title;
    private readonly string _subtitle;
    private bool _checked;
    private string? _lockedHint;

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

    public string? LockedHint
    {
        get => _lockedHint;
        set
        {
            if (_lockedHint == value)
                return;

            _lockedHint = value;
            Cursor = value == null ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    private bool Locked => _lockedHint != null;

    protected override Color Background => Theme.Surface;

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(Theme.ShadowSide, 1, W - Theme.ShadowSide * 2, H - 7);
        var fill = _checked ? Theme.CardSelected : Theme.Blend(Theme.Card, Theme.CardHover, Locked ? 0 : Hover);
        Theme.DrawCard(g, rect, 14, fill, _checked ? Theme.Accent : Theme.Border);

        var cy = rect.Y + rect.Height / 2;
        var muted = Locked ? Theme.Blend(Theme.TextMuted, Theme.Card, 0.5f) : Theme.TextMuted;
        using (var pen = new Pen(_checked ? Theme.Accent : muted, 1.8f))
            g.DrawEllipse(pen, rect.X + 14, cy - 9, 18, 18);
        if (_checked)
        {
            using var dot = new SolidBrush(Theme.Accent);
            g.FillEllipse(dot, rect.X + 18.5f, cy - 4.5f, 9, 9);
        }

        var x = rect.X + 44;
        Theme.DrawText(g, _title, Theme.BodyBold, Locked ? muted : Theme.Text, new RectangleF(x, rect.Y + 8, rect.Right - x - 12, 22));
        Theme.DrawText(g, _lockedHint ?? _subtitle, Theme.Caption, muted, new RectangleF(x, rect.Y + 30, rect.Right - x - 12, 20));
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (_checked || Locked)
            return;

        Checked = true;
        Selected?.Invoke(this, EventArgs.Empty);
    }
}
