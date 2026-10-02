namespace VpnClient.UI.Controls;

public class Segmented : ThemedControl
{
    private readonly string[] _options;
    private int _selected;
    private int _hover = -1;

    public Segmented(params string[] options)
    {
        _options = options;
        Height = Theme.Px(38);
        Cursor = Cursors.Hand;
    }

    public event EventHandler? SelectedIndexChanged;

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;
            _selected = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private RectangleF Segment(int index)
    {
        var width = (W - 8f) / _options.Length;
        return new RectangleF(4 + width * index, 4, width, H - 8);
    }

    protected override Color Background => Parent?.BackColor ?? Theme.Surface;

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Card, rect, 12);
        Theme.DrawRounded(g, Theme.Border, rect, 12);

        for (var i = 0; i < _options.Length; i++)
        {
            var segment = Segment(i);
            if (i == _selected)
                Theme.FillRounded(g, Theme.Accent, segment, 9);
            else if (i == _hover)
                Theme.FillRounded(g, Theme.CardHover, segment, 9);

            Theme.DrawText(g, _options[i], Theme.CaptionBold, i == _selected ? Color.White : Theme.Text, segment, StringAlignment.Center);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hover = -1;
        for (var i = 0; i < _options.Length; i++)
        {
            if (Segment(i).Contains(Theme.Design(e.Location)))
                hover = i;
        }

        if (hover != _hover)
        {
            _hover = hover;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        for (var i = 0; i < _options.Length; i++)
        {
            if (Segment(i).Contains(Theme.Design(e.Location)))
                SelectedIndex = i;
        }
    }
}
