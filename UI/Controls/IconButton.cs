using System.Drawing.Drawing2D;

namespace VpnClient.UI.Controls;

public enum IconKind
{
    Add,
    Link,
    Refresh,
    Gauge,
    Log
}

public class IconButton : Control
{
    private bool _hover;
    private bool _active;

    public IconButton(IconKind kind)
    {
        Kind = kind;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Sidebar;
        Size = new Size(44, 44);
        Cursor = Cursors.Hand;
    }

    public IconKind Kind { get; }

    public bool Active
    {
        get => _active;
        set { _active = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);
        if (_active)
            Theme.FillRounded(g, Color.FromArgb(70, Theme.Accent), rect, 12);
        else if (_hover)
            Theme.FillRounded(g, Theme.SidebarHover, rect, 12);

        var color = _active || _hover ? Theme.AccentDark : Theme.Text;
        using var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var cx = Width / 2f;
        var cy = Height / 2f;

        switch (Kind)
        {
            case IconKind.Add:
                using (var path = Theme.RoundedRect(new RectangleF(cx - 10, cy - 10, 20, 20), 5))
                    g.DrawPath(pen, path);
                g.DrawLine(pen, cx - 4.5f, cy, cx + 4.5f, cy);
                g.DrawLine(pen, cx, cy - 4.5f, cx, cy + 4.5f);
                break;

            case IconKind.Link:
                var state = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(-45);
                using (var left = Theme.RoundedRect(new RectangleF(-12, -4.5f, 14, 9), 4.5f))
                    g.DrawPath(pen, left);
                using (var right = Theme.RoundedRect(new RectangleF(-2, -4.5f, 14, 9), 4.5f))
                    g.DrawPath(pen, right);
                g.Restore(state);
                break;

            case IconKind.Refresh:
                g.DrawArc(pen, cx - 9, cy - 9, 18, 18, 40, 280);
                var angle = (40 + 280) * Math.PI / 180;
                var tip = new PointF(cx + 9 * (float)Math.Cos(angle), cy + 9 * (float)Math.Sin(angle));
                g.DrawLine(pen, tip.X, tip.Y, tip.X - 5.5f, tip.Y - 1f);
                g.DrawLine(pen, tip.X, tip.Y, tip.X + 0.5f, tip.Y - 5.5f);
                break;

            case IconKind.Gauge:
                g.DrawArc(pen, cx - 10, cy - 8, 20, 20, 180, 180);
                g.DrawLine(pen, cx, cy + 2, cx + 5, cy - 4);
                using (var dot = new SolidBrush(color))
                    g.FillEllipse(dot, cx - 2.5f, cy - 0.5f, 5, 5);
                break;

            case IconKind.Log:
                for (var i = -1; i <= 1; i++)
                {
                    var y = cy + i * 6;
                    g.DrawLine(pen, cx - 3, y, cx + 9, y);
                    using var dot = new SolidBrush(color);
                    g.FillEllipse(dot, cx - 10, y - 1.5f, 3, 3);
                }
                break;
        }
    }
}
