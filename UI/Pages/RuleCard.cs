using Tunnelka.Models;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class RuleCard : ThemedControl
{
    private const float PhaseWidth = 150;

    private RectangleF _vpnRect;
    private RectangleF _directRect;
    private RectangleF _deleteRect;
    private int _hover;

    public event EventHandler? Changed;
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
        var muted = Dimmed ? Theme.TextMuted : Theme.Text;
        DrawIcon(g, new RectangleF(rect.X + 14, cy - 10, 20, 20), Rule.IsProcess, Dimmed ? Theme.TextMuted : Theme.AccentStrong);

        _deleteRect = new RectangleF(rect.Right - 34, cy - 12, 24, 24);
        var phase = new RectangleF(_deleteRect.X - 8 - PhaseWidth, cy - 15, PhaseWidth, 30);
        _vpnRect = new RectangleF(phase.X, phase.Y, phase.Width / 2, phase.Height);
        _directRect = new RectangleF(phase.X + phase.Width / 2, phase.Y, phase.Width / 2, phase.Height);

        Theme.DrawText(g, Rule.DisplayName, Theme.BodyBold, muted, new RectangleF(rect.X + 44, rect.Y, phase.X - rect.X - 52, rect.Height));

        Theme.FillRounded(g, Theme.Surface, phase, phase.Height / 2);
        Theme.DrawRounded(g, Theme.Border, phase, phase.Height / 2);
        DrawSegment(g, _vpnRect, "VPN", Rule.Action == RoutingRule.Proxy, Theme.Accent, _hover == 1);
        DrawSegment(g, _directRect, L.T("ПРЯМОЕ"), Rule.Action == RoutingRule.Direct, Theme.PingGood, _hover == 2);

        using var pen = Theme.IconPen(_hover == 3 ? Theme.PingBad : Theme.TextMuted);
        var c = new PointF(_deleteRect.X + 12, _deleteRect.Y + 12);
        g.DrawLine(pen, c.X - 5, c.Y - 5, c.X + 5, c.Y + 5);
        g.DrawLine(pen, c.X + 5, c.Y - 5, c.X - 5, c.Y + 5);
    }

    private void DrawSegment(Graphics g, RectangleF r, string text, bool active, Color color, bool hover)
    {
        var inner = RectangleF.Inflate(r, -3, -3);
        if (active)
            Theme.FillRounded(g, Dimmed ? Color.FromArgb(90, color) : color, inner, inner.Height / 2);
        else if (hover)
            Theme.FillRounded(g, Theme.CardHover, inner, inner.Height / 2);

        Theme.DrawText(g, text, Theme.CaptionBold, active ? Color.White : Theme.TextMuted, r, StringAlignment.Center);
    }

    private static void DrawIcon(Graphics g, RectangleF r, bool process, Color color)
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

    private int HitTest(Point location)
    {
        var point = Theme.Design(location);
        return _vpnRect.Contains(point) ? 1 : _directRect.Contains(point) ? 2 : _deleteRect.Contains(point) ? 3 : 0;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hover = HitTest(e.Location);
        Cursor = hover == 0 ? Cursors.Default : Cursors.Hand;
        if (hover == _hover)
            return;

        _hover = hover;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = 0;
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        switch (HitTest(e.Location))
        {
            case 1:
                SetAction(RoutingRule.Proxy);
                break;
            case 2:
                SetAction(RoutingRule.Direct);
                break;
            case 3:
                DeleteClicked?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void SetAction(string action)
    {
        if (Rule.Action == action)
            return;

        Rule.Action = action;
        Invalidate();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
