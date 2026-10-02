using VpnClient.Models;
using VpnClient.Services;
using VpnClient.UI.Controls;

namespace VpnClient.UI.Pages;

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

    private readonly SearchBox _input = new(L.T("Домены или IP через запятую"), false);
    private readonly Segmented _action = new(L.T("Напрямую"), L.T("Через VPN"), L.T("Блок"));

    private static readonly string RoutingBottomComment = L.T("Напрямую - использование без VPN\nЧерез VPN - использование через VPN\nБлок - полная блокировка трафика");

    public event EventHandler? RulesChanged;

    public RoutingPage(List<RoutingRule> rules, Action onBack)
    {
        _rules = rules;
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);
        Theme.Bind(_list, () => Theme.Surface);
        var title = PageParts.Header(L.T("Маршрутизация"), onBack);
        var subtitle = PageParts.Caption(L.T("Правила проверяются сверху вниз. Всё остальное идёт через VPN."), 34);

        var form = new Panel { Dock = DockStyle.Top, Height = Theme.Px(184) };
        Theme.Bind(form, () => Theme.Surface);

        var commentLabel = Theme.Bind(new Label
        {
            Text = RoutingBottomComment,
            AutoSize = false,
            Dock = DockStyle.Bottom,
            Height = Theme.Px(70),
            TextAlign = ContentAlignment.TopLeft,
            Padding = Theme.Px(16, 0, 16, 20),
            Font = Theme.Scaled(Theme.Caption)
        }, () => Theme.Surface, () => Theme.TextMuted);

        var add = PageParts.Button(L.T("Добавить"), true);
        add.Click += (_, _) => AddFromInput();

        var preset = PageParts.Button(L.T("Российские сайты напрямую"), false);
        preset.Click += (_, _) => AddRule("domain:ru, domain:su, domain:рф", RoutingRule.Direct);

        var process = PageParts.Button(L.T("Процесс"), false);
        process.Click += (_, _) =>
        {
            var name = ProcessPicker.Show(FindForm());
            if (name != null)
                AddRule(XrayConfigBuilder.ProcessPrefix + name, SelectedAction());
        };

        var file = PageParts.Button(L.T("Файл .exe"), false);
        file.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = L.T("Программы (*.exe)|*.exe"), Title = L.T("Выбери программу") };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                AddRule(XrayConfigBuilder.ProcessPrefix + dialog.FileName, SelectedAction());
        };

        form.Controls.AddRange(new Control[] { _input, _action, add, process, file, preset });
        form.Resize += (_, _) =>
        {
            var width = form.Width - Theme.Px(6);
            _input.SetBounds(0, 0, width, Theme.Px(42));
            _action.SetBounds(0, Theme.Px(52), width - Theme.Px(120), Theme.Px(38));
            add.SetBounds(width - Theme.Px(110), Theme.Px(52), Theme.Px(110), Theme.Px(38));
            var gap = Theme.Px(10);
            var half = (width - gap) / 2;
            process.SetBounds(0, Theme.Px(100), half, Theme.Px(34));
            file.SetBounds(half + gap, Theme.Px(100), width - half - gap, Theme.Px(34));
            preset.SetBounds(0, Theme.Px(144), width, Theme.Px(34));
        };

        var gap = new Panel { Dock = DockStyle.Top, Height = Theme.Px(12) };
        Theme.Bind(gap, () => Theme.Surface);

        Controls.Add(commentLabel);
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

        AddRule(_input.Query, SelectedAction());
        _input.Clear();
    }

    private string SelectedAction() => _action.SelectedIndex switch
    {
        1 => RoutingRule.Proxy,
        2 => RoutingRule.Block,
        _ => RoutingRule.Direct
    };

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
            _list.Controls.Add(PageParts.Caption(L.T("Правил пока нет"), 30));

        ResizeCards();
        _list.ResumeLayout();
    }

    private void ResizeCards()
    {
        var width = _list.Width - SystemInformation.VerticalScrollBarWidth - Theme.Px(6);
        if (width <= 0)
            return;

        foreach (Control control in _list.Controls)
            control.Width = width;
    }
}
