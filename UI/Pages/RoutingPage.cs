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
            Changed();
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
                Add(RoutingRule.ForProcess(name, RoutingRule.Proxy));
        };
        var file = PageParts.Button(L.T("+ Файл .exe"), false);
        file.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = L.T("Программы (*.exe)|*.exe"), Title = L.T("Выбери программу") };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                Add(RoutingRule.ForProcess(dialog.FileName, RoutingRule.Proxy));
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
            Add(new RoutingRule { Value = RoutingValues.Clean(value), Action = RoutingRule.Proxy }, false);

        _input.Clear();
        Changed();
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
            Changed();
    }

    private void Changed()
    {
        Rebuild();
        RulesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Rebuild()
    {
        _hint.Text = Hint();

        _list.SuspendLayout();
        foreach (Control control in _list.Controls)
            control.Dispose();
        _list.Controls.Clear();

        foreach (var rule in _routing.Rules)
        {
            var card = new RuleCard(rule) { Dimmed = !_routing.Enabled };
            card.Changed += (_, _) =>
            {
                _hint.Text = Hint();
                RulesChanged?.Invoke(this, EventArgs.Empty);
            };
            card.DeleteClicked += (_, _) =>
            {
                _routing.Rules.Remove(rule);
                Changed();
            };
            _list.Controls.Add(card);
        }

        if (_routing.Rules.Count == 0)
            _list.Controls.Add(PageParts.Caption(L.T("Список пуст. Добавь программу или сайт"), 30));

        ResizeCards();
        _list.ResumeLayout();
    }

    private string Hint()
    {
        if (!_routing.Enabled || _routing.Rules.Count == 0)
            return L.T("Весь трафик идёт через VPN");

        if (_routing.OnlyVpnListed)
            return L.F("Через VPN только: {0}. Остальное напрямую", Names(RoutingRule.Proxy));

        return L.F("Напрямую: {0}. Остальное через VPN", Names(RoutingRule.Direct));
    }

    private string Names(string action)
    {
        var names = _routing.Rules.Where(r => r.Action == action).Select(r => r.DisplayName).ToList();
        return names.Count <= 3 ? string.Join(", ", names) : string.Join(", ", names.Take(3)) + L.F(" и ещё {0}", names.Count - 3);
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
