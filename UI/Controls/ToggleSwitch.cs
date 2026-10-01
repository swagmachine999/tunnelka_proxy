namespace VpnClient.UI.Controls;

public class ToggleSwitch : ThemedControl
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        Size = new Size(Theme.Px(44), Theme.Px(24));
        Cursor = Cursors.Hand;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override Color Background => Parent?.BackColor ?? Theme.Card;

    protected override void Draw(Graphics g)
    {
        var track = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, _checked ? Theme.Accent : Theme.TrackOff, track, track.Height / 2);

        var knob = H - 7f;
        var x = _checked ? W - knob - 4 : 3;
        using var brush = new SolidBrush(Color.White);
        g.FillEllipse(brush, x, 3, knob, knob);
    }
}
