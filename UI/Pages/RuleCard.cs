using VpnClient.Models;
using VpnClient.Services;
using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class RuleCard : ThemedControl
{
    private RectangleF _actionRect;
    private RectangleF _toggleRect;
    private RectangleF _deleteRect;
    private bool _hoverDelete;

    public event EventHandler? Changed;
    public event EventHandler? DeleteClicked;

    public RuleCard(RoutingRule rule)
    {
        Rule = rule;
        Height = Theme.Px(70);
        Margin = Theme.Px(0, 0, 0, 8);
    }

    public RoutingRule Rule { get; }

    private static string Describe(string values) => string.Join(", ", XrayConfigBuilder.SplitValues(values).Select(value =>
    {
        if (!value.StartsWith(XrayConfigBuilder.ProcessPrefix, StringComparison.OrdinalIgnoreCase))
            return value;

        var target = value.Substring(XrayConfigBuilder.ProcessPrefix.Length).Trim();
        return target.Contains('/') || target.Contains('\\')
            ? L.F("{0} (файл)", Path.GetFileName(target.Replace('/', '\\')))
            : L.F("{0} (процесс)", target);
    }));

    public static string ActionTitle(string action) => action switch
    {
        RoutingRule.Proxy => L.T("Через VPN"),
        RoutingRule.Block => L.T("Блокировать"),
        _ => L.T("Напрямую")
    };

    public static Color ActionColor(string action) => action switch
    {
        RoutingRule.Proxy => Theme.Accent,
        RoutingRule.Block => Theme.PingBad,
        _ => Theme.PingGood
    };

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);

        _deleteRect = new RectangleF(W - 36, 10, 24, 24);
        _toggleRect = new RectangleF(W - 82, 10, 40, 22);

        var textColor = Rule.Enabled ? Theme.Text : Theme.TextMuted;
        Theme.DrawText(g, Describe(Rule.Values), Theme.BodyBold, textColor, new RectangleF(16, 9, W - 112, 24));

        var title = ActionTitle(Rule.Action);
        var color = ActionColor(Rule.Action);
        var chipWidth = Theme.Measure(title, Theme.CaptionBold).Width + 18;
        _actionRect = new RectangleF(16, 38, chipWidth, 22);
        Theme.FillRounded(g, Color.FromArgb(Rule.Enabled ? 60 : 30, color), _actionRect, 11);
        Theme.DrawText(g, title, Theme.CaptionBold, Rule.Enabled ? color : Theme.TextMuted, _actionRect, StringAlignment.Center);

        Theme.FillRounded(g, Rule.Enabled ? Theme.Accent : Theme.TrackOff, _toggleRect, 11);
        using (var knob = new SolidBrush(Color.White))
            g.FillEllipse(knob, Rule.Enabled ? _toggleRect.Right - 19 : _toggleRect.X + 3, _toggleRect.Y + 3, 16, 16);

        using var pen = new Pen(_hoverDelete ? Theme.PingBad : Theme.TextMuted, 2f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        var c = new PointF(_deleteRect.X + 12, _deleteRect.Y + 12);
        g.DrawLine(pen, c.X - 5, c.Y - 5, c.X + 5, c.Y + 5);
        g.DrawLine(pen, c.X + 5, c.Y - 5, c.X - 5, c.Y + 5);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = Theme.Design(e.Location);
        var overDelete = _deleteRect.Contains(point);
        Cursor = overDelete || _toggleRect.Contains(point) || _actionRect.Contains(point) ? Cursors.Hand : Cursors.Default;
        if (overDelete != _hoverDelete)
        {
            _hoverDelete = overDelete;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverDelete = false;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var point = Theme.Design(e.Location);
        if (_deleteRect.Contains(point))
        {
            DeleteClicked?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_toggleRect.Contains(point))
            Rule.Enabled = !Rule.Enabled;
        else if (_actionRect.Contains(point))
            Rule.Action = Rule.Action switch
            {
                RoutingRule.Direct => RoutingRule.Proxy,
                RoutingRule.Proxy => RoutingRule.Block,
                _ => RoutingRule.Direct
            };
        else
            return;

        Invalidate();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
