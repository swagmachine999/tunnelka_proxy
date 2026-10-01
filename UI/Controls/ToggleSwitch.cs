namespace VpnClient.UI.Controls;

public class ToggleSwitch : Control
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(44, 24);
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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Parent?.BackColor ?? Theme.Card);

        var track = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Theme.FillRounded(g, _checked ? Theme.Accent : Theme.TrackOff, track, track.Height / 2);

        var knob = Height - 7f;
        var x = _checked ? Width - knob - 4 : 3;
        using var brush = new SolidBrush(Color.White);
        g.FillEllipse(brush, x, 3, knob, knob);
    }
}
