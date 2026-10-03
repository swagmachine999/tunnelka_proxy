namespace Tunnelka.UI.Controls;

public class Segmented : ThemedControl
{
    private readonly string[] _options;
    private int _selected;
    private int _hover = -1;
    private float _pill = -1;

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
            Animate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private RectangleF Segment(int index)
    {
        var width = (W - 8f) / _options.Length;
        return new RectangleF(4 + width * index, 4, width, H - 8);
    }

    protected override Color Background => Parent?.BackColor ?? Theme.Surface;

    protected override bool AnimateMore() => Animator.Approach(ref _pill, _selected);

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Card, rect, 12);
        Theme.DrawRounded(g, Theme.Border, rect, 12);

        if (_pill < 0)
            _pill = _selected;

        if (_hover >= 0 && _hover != _selected)
            Theme.FillRounded(g, Theme.CardHover, Segment(_hover), 9);

        var width = (W - 8f) / _options.Length;
        Theme.FillRounded(g, Theme.Accent, new RectangleF(4 + width * _pill, 4, width, H - 8), 9);

        for (var i = 0; i < _options.Length; i++)
        {
            var onPill = 1 - Math.Min(1, Math.Abs(i - _pill));
            Theme.DrawText(g, _options[i], Theme.CaptionBold, Theme.Blend(Theme.Text, Color.White, onPill), Segment(i), StringAlignment.Center);
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
