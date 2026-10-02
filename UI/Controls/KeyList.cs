namespace Tunnelka.UI.Controls;

public class KeyList : ThemedControl
{
    private const float RowHeight = 44;
    private const float Gap = 6;

    private readonly IReadOnlyList<string> _names;
    private int _hover = -1;

    public event EventHandler? SelectedIndexChanged;

    public KeyList(IReadOnlyList<string> names)
    {
        _names = names;
        Height = Theme.Px(names.Count * (RowHeight + Gap) - Gap + 2);
        Cursor = Cursors.Hand;
    }

    public int SelectedIndex { get; private set; }

    protected override Color Background => Theme.Window;

    protected override void Draw(Graphics g)
    {
        for (var i = 0; i < _names.Count; i++)
        {
            var row = new RectangleF(0.5f, i * (RowHeight + Gap) + 0.5f, W - 1.5f, RowHeight);
            var selected = i == SelectedIndex;
            Theme.FillRounded(g, selected ? Theme.CardSelected : i == _hover ? Theme.CardHover : Theme.Card, row, 12);
            Theme.DrawRounded(g, selected ? Theme.Accent : Theme.Border, row, 12);

            var cy = row.Y + RowHeight / 2;
            using (var pen = new Pen(selected ? Theme.Accent : Theme.TextMuted, 1.6f))
                g.DrawEllipse(pen, 16, cy - 8, 16, 16);
            if (selected)
            {
                using var brush = new SolidBrush(Theme.Accent);
                g.FillEllipse(brush, 20, cy - 4, 8, 8);
            }

            NamePainter.Draw(g, ServerText.Parts(_names[i], _names[i]), Theme.BodyBold, Theme.Text,
                new RectangleF(44, cy - 12, W - 56, 24));
        }
    }

    private int IndexAt(Point location)
    {
        var index = (int)(Theme.Design(location).Y / (RowHeight + Gap));
        return index >= 0 && index < _names.Count ? index : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var index = IndexAt(e.Location);
        if (index == _hover)
            return;

        _hover = index;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var index = IndexAt(e.Location);
        if (index < 0 || index == SelectedIndex)
            return;

        SelectedIndex = index;
        Invalidate();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }
}
