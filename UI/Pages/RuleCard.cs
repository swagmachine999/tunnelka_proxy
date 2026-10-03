using Tunnelka.Models;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class RuleCard : ThemedControl
{
    private static readonly Dictionary<string, Image?> Icons = new(StringComparer.OrdinalIgnoreCase);

    private RectangleF _deleteRect;
    private bool _hoverDelete;

    public event EventHandler? DeleteClicked;

    public RuleCard(RoutingRule rule)
    {
        Rule = rule;
        Height = Theme.Px(56 + Theme.ShadowBottom);
        Margin = Theme.Px(0, 0, 0, 3);
    }

    public RoutingRule Rule { get; }

    public bool Dimmed { get; set; }

    protected override bool CachePaint => true;

    protected override void Draw(Graphics g)
    {
        var rect = Theme.CardRect(W, H);
        Theme.DrawCard(g, rect, 14, Theme.Card, Theme.Border);

        var cy = rect.Y + rect.Height / 2;
        var icon = new RectangleF(rect.X + 14, cy - 12, 24, 24);
        if (LoadIcon(Rule.IconPath) is { } image)
            g.DrawImage(image, icon);
        else
            DrawGlyph(g, RectangleF.Inflate(icon, -2, -2), Rule.IsProcess, Dimmed ? Theme.TextMuted : Theme.AccentStrong);

        _deleteRect = new RectangleF(rect.Right - 34, cy - 12, 24, 24);
        var textX = icon.Right + 12;
        var textWidth = _deleteRect.X - textX - 8;
        var color = Dimmed ? Theme.TextMuted : Theme.Text;
        Theme.DrawText(g, Rule.DisplayName, Theme.BodyBold, color, new RectangleF(textX, rect.Y, textWidth, rect.Height));

        using var pen = Theme.IconPen(_hoverDelete ? Theme.PingBad : Theme.TextMuted);
        var c = new PointF(_deleteRect.X + 12, _deleteRect.Y + 12);
        g.DrawLine(pen, c.X - 5, c.Y - 5, c.X + 5, c.Y + 5);
        g.DrawLine(pen, c.X + 5, c.Y - 5, c.X - 5, c.Y + 5);
    }

    private static Image? LoadIcon(string path)
    {
        if (path.Length == 0)
            return null;

        if (Icons.TryGetValue(path, out var cached))
            return cached;

        Image? image = null;
        try
        {
            if (File.Exists(path))
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                image = icon?.ToBitmap();
            }
        }
        catch (Exception)
        {
        }

        Icons[path] = image;
        return image;
    }

    private static void DrawGlyph(Graphics g, RectangleF r, bool process, Color color)
    {
        using var pen = Theme.IconPen(color);
        if (process)
        {
            using (var path = Theme.RoundedRect(r, 5))
                g.DrawPath(pen, path);
            g.DrawLine(pen, r.X, r.Y + 6, r.Right, r.Y + 6);
            return;
        }

        g.DrawEllipse(pen, r);
        g.DrawEllipse(pen, r.X + r.Width * 0.28f, r.Y, r.Width * 0.44f, r.Height);
        g.DrawLine(pen, r.X, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var over = _deleteRect.Contains(Theme.Design(e.Location));
        Cursor = over ? Cursors.Hand : Cursors.Default;
        if (over == _hoverDelete)
            return;

        _hoverDelete = over;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverDelete = false;
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (_deleteRect.Contains(Theme.Design(e.Location)))
            DeleteClicked?.Invoke(this, EventArgs.Empty);
    }
}
