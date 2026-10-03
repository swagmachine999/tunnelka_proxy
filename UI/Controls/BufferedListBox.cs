namespace Tunnelka.UI.Controls;

public class BufferedListBox : ListBox
{
    private const int ScrollMessage = 0x115;

    public BufferedListBox()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public void InvalidateItem(int index)
    {
        if (index >= 0 && index < Items.Count)
            Invalidate(GetItemRectangle(index));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var back = new SolidBrush(BackColor))
            e.Graphics.FillRectangle(back, ClientRectangle);

        for (var i = TopIndex; i < Items.Count; i++)
        {
            var bounds = GetItemRectangle(i);
            if (bounds.Top > ClientSize.Height)
                break;
            if (!e.ClipRectangle.IntersectsWith(bounds))
                continue;

            var state = SelectedIndices.Contains(i) ? DrawItemState.Selected : DrawItemState.None;
            OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, bounds, i, state));
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == ScrollMessage)
            Invalidate();
    }
}
