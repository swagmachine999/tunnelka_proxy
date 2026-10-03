using System.Drawing.Drawing2D;

namespace Tunnelka.UI.Controls;

public class IconButton : ThemedControl
{
    private bool _active;
    private float _activeAmount;

    public IconButton(IconKind kind, string title)
    {
        Kind = kind;
        Title = title;
        Size = new Size(Theme.Px(44), Theme.Px(44));
        Cursor = Cursors.Hand;
    }

    public IconKind Kind { get; }
    public Func<Color> Backdrop { get; set; } = () => Theme.Sidebar;
    public string Title { get; }

    public bool Active
    {
        get => _active;
        set { _active = value; Animate(); }
    }

    protected override Color Background => Backdrop();

    protected override bool AnimateMore() => Animator.Approach(ref _activeAmount, _active ? 1 : 0);

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0, 0, W - 1, H - 1);
        if (Hover > 0.01f)
            Theme.FillRounded(g, Color.FromArgb((int)(Theme.SidebarHover.A * Hover), Theme.SidebarHover), rect, 12);
        if (_activeAmount > 0.01f)
            Theme.FillRounded(g, Color.FromArgb((int)((Theme.IsDark ? 90 : 70) * _activeAmount), Theme.Accent), rect, 12);

        var color = Theme.Blend(Theme.Text, Theme.AccentStrong, Math.Max(Hover, _activeAmount));
        using var pen = Theme.IconPen(color);
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
