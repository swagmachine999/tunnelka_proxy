using VpnClient.Models;
using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

public class RuleCard : Control
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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = 70;
        Margin = new Padding(0, 0, 0, 8);
    }

    public RoutingRule Rule { get; }

    public static string ActionTitle(string action) => action switch
    {
        RoutingRule.Proxy => "Через VPN",
        RoutingRule.Block => "Блокировать",
        _ => "Напрямую"
    };

    public static Color ActionColor(string action) => action switch
    {
        RoutingRule.Proxy => Theme.Accent,
        RoutingRule.Block => Theme.PingBad,
        _ => Theme.PingGood
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Theme.FillRounded(g, Theme.Card, rect, 14);
        Theme.DrawRounded(g, Theme.Border, rect, 14);

        _deleteRect = new RectangleF(Width - 36, 10, 24, 24);
        _toggleRect = new RectangleF(Width - 82, 10, 40, 22);

        var textColor = Rule.Enabled ? Theme.Text : Theme.TextMuted;
        Theme.DrawText(g, Rule.Values, Theme.BodyBold, textColor, new RectangleF(16, 9, Width - 112, 24));

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
        var overDelete = _deleteRect.Contains(e.Location);
        Cursor = overDelete || _toggleRect.Contains(e.Location) || _actionRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
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
        if (_deleteRect.Contains(e.Location))
        {
            DeleteClicked?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_toggleRect.Contains(e.Location))
            Rule.Enabled = !Rule.Enabled;
        else if (_actionRect.Contains(e.Location))
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

public class RoutingPage : Panel
{
    private readonly List<RoutingRule> _rules;
    private readonly FlowLayoutPanel _list = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true
    };

    private readonly SearchBox _input = new("Домены или IP через запятую", false);
    private readonly Segmented _action = new("Напрямую", "Через VPN", "Блок");

    public event EventHandler? RulesChanged;

    public RoutingPage(List<RoutingRule> rules, Action onBack)
    {
        _rules = rules;
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);
        Theme.Bind(_list, () => Theme.Surface);
        var title = PageParts.Header("Маршрутизация", onBack);
        var subtitle = PageParts.Caption("Правила проверяются сверху вниз. Всё остальное идёт через VPN.", 34);

        var form = new Panel { Dock = DockStyle.Top, Height = 140 };
        Theme.Bind(form, () => Theme.Surface);

        _input.SetBounds(0, 0, 360, 42);

        var add = PageParts.Button("Добавить", true);
        add.Click += (_, _) => AddFromInput();

        var preset = PageParts.Button("Российские сайты напрямую", false);
        preset.Click += (_, _) => AddRule("domain:ru, domain:su, domain:рф", RoutingRule.Direct);

        form.Controls.AddRange(new Control[] { _input, _action, add, preset });
        form.Resize += (_, _) =>
        {
            var width = form.Width - 6;
            _input.SetBounds(0, 0, width, 42);
            _action.SetBounds(0, 52, width - 120, 38);
            add.SetBounds(width - 110, 52, 110, 38);
            preset.SetBounds(0, 100, width, 34);
        };

        var gap = new Panel { Dock = DockStyle.Top, Height = 12 };
        Theme.Bind(gap, () => Theme.Surface);

        Controls.Add(_list);
        Controls.Add(gap);
        Controls.Add(form);
        Controls.Add(subtitle);
        Controls.Add(title);

        _list.Resize += (_, _) => ResizeCards();
        Rebuild();
    }

    private void AddFromInput()
    {
        if (_input.Query.Length == 0)
            return;

        var action = _action.SelectedIndex switch
        {
            1 => RoutingRule.Proxy,
            2 => RoutingRule.Block,
            _ => RoutingRule.Direct
        };

        AddRule(_input.Query, action);
        _input.Clear();
    }

    private void AddRule(string values, string action)
    {
        _rules.Add(new RoutingRule { Values = values, Action = action });
        Rebuild();
        RulesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Rebuild()
    {
        _list.SuspendLayout();
        foreach (Control control in _list.Controls)
            control.Dispose();
        _list.Controls.Clear();

        foreach (var rule in _rules)
        {
            var card = new RuleCard(rule);
            card.Changed += (_, _) => RulesChanged?.Invoke(this, EventArgs.Empty);
            card.DeleteClicked += (_, _) =>
            {
                _rules.Remove(rule);
                Rebuild();
                RulesChanged?.Invoke(this, EventArgs.Empty);
            };
            _list.Controls.Add(card);
        }

        if (_rules.Count == 0)
            _list.Controls.Add(PageParts.Caption("Правил пока нет", 30));

        ResizeCards();
        _list.ResumeLayout();
    }

    private void ResizeCards()
    {
        var width = _list.Width - SystemInformation.VerticalScrollBarWidth - 6;
        if (width <= 0)
            return;

        foreach (Control control in _list.Controls)
            control.Width = width;
    }
}
