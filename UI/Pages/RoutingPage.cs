using Tunnelka.Models;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class RoutingPage : Panel
{
    private readonly RoutingSettings _routing;
    private readonly FlowLayoutPanel _list = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true
    };

    private readonly ToggleSwitch _enabled = new();
    private readonly Segmented _mode = new(L.T("Что-то через VPN"), L.T("Что-то без VPN"));
    private readonly Label _hint;
    private readonly Label _tunNote;
    private readonly SearchBox _input = new(L.T("Сайт или IP, например sberbank.ru"), false);
    private readonly Panel _reconnectBar = new() { Dock = DockStyle.Top, Visible = false };

    public event EventHandler? RulesChanged;
    public event EventHandler? ReconnectRequested;

    public RoutingPage(RoutingSettings routing, Action onBack)
    {
        _routing = routing;
        Dock = DockStyle.Fill;
        Theme.Bind(this, () => Theme.Surface);
        Theme.Bind(_list, () => Theme.Surface);

        var header = PageParts.Header(L.T("Маршрутизация"), onBack);
        _enabled.Checked = routing.Enabled;
        header.Controls.Add(_enabled);
        _enabled.BringToFront();
        header.Resize += (_, _) => _enabled.Location = new Point(header.Width - _enabled.Width - Theme.Px(8), (header.Height - _enabled.Height) / 2);
        _enabled.CheckedChanged += (_, _) =>
        {
            _routing.Enabled = _enabled.Checked;
            Changed(true);
        };

        _mode.Dock = DockStyle.Top;
        _mode.SelectedIndex = routing.Mode == RoutingMode.SomeViaVpn ? 0 : 1;
        _mode.SelectedIndexChanged += (_, _) =>
        {
            _routing.Mode = _mode.SelectedIndex == 0 ? RoutingMode.SomeViaVpn : RoutingMode.SomeDirect;
            Changed(true);
        };

        _hint = PageParts.Caption("", 36);
        _tunNote = PageParts.Caption(L.T("Правила для программ надёжно работают в режиме TUN"), 30);
        Theme.Bind(_tunNote, () => Theme.Surface, () => Theme.PingMid);

        var addRow = new Panel { Dock = DockStyle.Top, Height = Theme.Px(96) };
        Theme.Bind(addRow, () => Theme.Surface);
        var program = PageParts.Button(L.T("+ Программа"), false);
        program.Click += (_, _) =>
        {
            var name = ProcessPicker.Show(FindForm());
            if (name != null)
                Add(RoutingRule.ForProcess(name, _routing.NewRuleAction));
        };
        var file = PageParts.Button(L.T("+ Файл .exe"), false);
        file.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = L.T("Программы (*.exe)|*.exe"), Title = L.T("Выбери программу") };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                Add(RoutingRule.ForProcess(dialog.FileName, _routing.NewRuleAction));
        };
        var add = PageParts.Button(L.T("Добавить"), true);
        add.Click += (_, _) => AddFromInput();
        _input.Submitted += (_, _) => AddFromInput();
        addRow.Controls.AddRange(new Control[] { program, file, _input, add });
        addRow.Resize += (_, _) =>
        {
            var width = addRow.Width - Theme.Px(6);
            var gap = Theme.Px(10);
            var half = (width - gap) / 2;
            program.SetBounds(0, 0, half, Theme.Px(38));
            file.SetBounds(half + gap, 0, width - half - gap, Theme.Px(38));
            _input.SetBounds(0, Theme.Px(48), width - Theme.Px(120), Theme.Px(42));
            add.SetBounds(width - Theme.Px(110), Theme.Px(50), Theme.Px(110), Theme.Px(38));
        };

        BuildReconnectBar();

        var gap = Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(8) }, () => Theme.Surface);
        Controls.Add(_list);
        Controls.Add(_tunNote);
        Controls.Add(gap);
        Controls.Add(addRow);
        Controls.Add(_reconnectBar);
        Controls.Add(_hint);
        Controls.Add(_mode);
        Controls.Add(header);

        _list.Resize += (_, _) => ResizeCards();
        Rebuild();
    }

    public void ShowReconnectHint(bool show) => _reconnectBar.Visible = show;

    public void SetTunMode(bool tun) => _tunNote.Visible = !tun && _routing.Rules.Any(r => r.IsProcess);

    private void BuildReconnectBar()
    {
        _reconnectBar.Height = Theme.Px(52);
        _reconnectBar.Padding = Theme.Px(0, 4, 6, 10);
        Theme.Bind(_reconnectBar, () => Theme.Surface);
        var reconnect = PageParts.Button(L.T("Переподключить"), true);
        reconnect.Dock = DockStyle.Right;
        reconnect.Width = Theme.Px(150);
        reconnect.Click += (_, _) => ReconnectRequested?.Invoke(this, EventArgs.Empty);
        var text = Theme.Bind(new Label
        {
            Text = L.T("Правила изменены. Они заработают после переподключения"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Scaled(Theme.CaptionBold)
        }, () => Theme.Surface, () => Theme.AccentStrong);
        _reconnectBar.Controls.Add(text);
        _reconnectBar.Controls.Add(reconnect);
    }

    private void AddFromInput()
    {
        foreach (var value in RoutingValues.Split(_input.Query))
            Add(new RoutingRule { Value = RoutingValues.Clean(value), Action = _routing.NewRuleAction }, false);

        _input.Clear();
        Changed(true);
    }

    private void Add(RoutingRule rule, bool notify = true)
    {
        if (rule.Target.Length == 0)
            return;

        var existing = _routing.Rules.FirstOrDefault(r => string.Equals(r.Value, rule.Value, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            existing.Action = rule.Action;
        else
            _routing.Rules.Insert(0, rule);

        if (notify)
            Changed(true);
    }

    private void Changed(bool rebuild)
    {
        if (rebuild)
            Rebuild();
        RulesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Rebuild()
    {
        _hint.Text = !_routing.Enabled
            ? L.T("Правила выключены — весь интернет идёт через VPN")
            : _routing.Mode == RoutingMode.SomeViaVpn
                ? L.T("Через VPN — только строки с пометкой VPN, остальное напрямую")
                : L.T("Напрямую — только строки с пометкой ПРЯМОЕ, остальное через VPN");

        _list.SuspendLayout();
        foreach (Control control in _list.Controls)
            control.Dispose();
        _list.Controls.Clear();

        foreach (var rule in _routing.Rules)
        {
            var card = new RuleCard(rule) { Dimmed = !_routing.Enabled };
            card.Changed += (_, _) => Changed(false);
            card.DeleteClicked += (_, _) =>
            {
                _routing.Rules.Remove(rule);
                Changed(true);
            };
            _list.Controls.Add(card);
        }

        if (_routing.Rules.Count == 0)
            _list.Controls.Add(PageParts.Caption(L.T("Список пуст. Добавь программу или сайт"), 30));

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
