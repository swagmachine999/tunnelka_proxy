using System.Drawing.Drawing2D;

namespace VpnClient.UI.Controls;

public enum IconKind
{
    Add,
    Servers,
    Stats,
    Gauge,
    Routing,
    Log,
    Settings,
    Ping
}

public class IconButton : Control
{
    private float W => Width / Theme.S;
    private float H => Height / Theme.S;

    private bool _hover;
    private bool _active;

    public IconButton(IconKind kind, string title)
    {
        Kind = kind;
        Title = title;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(Theme.Px(44), Theme.Px(44));
        Cursor = Cursors.Hand;
    }

    public IconKind Kind { get; }
    public Func<Color> Background { get; set; } = () => Theme.Sidebar;
    public string Title { get; }

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
        Theme.Begin(g, Background());

        var rect = new RectangleF(0, 0, W - 1, H - 1);
        if (_active)
            Theme.FillRounded(g, Color.FromArgb(Theme.IsDark ? 90 : 70, Theme.Accent), rect, 12);
        else if (_hover)
            Theme.FillRounded(g, Theme.SidebarHover, rect, 12);

        var color = _active || _hover ? Theme.AccentStrong : Theme.Text;
        using var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var fill = new SolidBrush(color);
        var cx = W / 2f;
        var cy = H / 2f;

        switch (Kind)
        {
            case IconKind.Add:
                using (var path = Theme.RoundedRect(new RectangleF(cx - 10, cy - 10, 20, 20), 5))
                    g.DrawPath(pen, path);
                g.DrawLine(pen, cx - 4.5f, cy, cx + 4.5f, cy);
                g.DrawLine(pen, cx, cy - 4.5f, cx, cy + 4.5f);
                break;

            case IconKind.Servers:
                g.DrawEllipse(pen, cx - 10, cy - 10, 20, 20);
                g.DrawEllipse(pen, cx - 4.5f, cy - 10, 9, 20);
                g.DrawLine(pen, cx - 10, cy, cx + 10, cy);
                break;

            case IconKind.Stats:
                g.DrawLine(pen, cx - 10, cy + 9, cx + 10, cy + 9);
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - 9, cy + 3), new PointF(cx - 3, cy - 3),
                    new PointF(cx + 2, cy + 1), new PointF(cx + 9, cy - 7)
                });
                g.FillEllipse(fill, cx + 7, cy - 9, 4, 4);
                break;

            case IconKind.Gauge:
                g.DrawArc(pen, cx - 10, cy - 8, 20, 20, 180, 180);
                g.DrawLine(pen, cx, cy + 2, cx + 5, cy - 4);
                g.FillEllipse(fill, cx - 2.5f, cy - 0.5f, 5, 5);
                break;

            case IconKind.Settings:
                using (var teeth = new Pen(color, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = Math.PI / 4 * i;
                        var cos = (float)Math.Cos(angle);
                        var sin = (float)Math.Sin(angle);
                        g.DrawLine(teeth, cx + cos * 7.5f, cy + sin * 7.5f, cx + cos * 9.5f, cy + sin * 9.5f);
                    }
                }
                g.DrawEllipse(pen, cx - 7, cy - 7, 14, 14);
                g.DrawEllipse(pen, cx - 3, cy - 3, 6, 6);
                break;
        }
    }
}
