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

    private readonly Dictionary<RoutingMode, RadioCard> _modes = new();
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
        AddMode(RoutingMode.AllVpn, L.T("Всё через VPN"), L.T("Список не действует, весь трафик идёт через VPN"));
        AddMode(RoutingMode.DirectForListed, L.T("Без VPN для выбранных"), L.T("Программы и сайты из списка идут напрямую, остальное через VPN"));
        AddMode(RoutingMode.VpnForListed, L.T("VPN только для выбранных"), L.T("Через VPN идут только программы и сайты из списка"));
        var listTitle = PageParts.Caption(L.T("ВЫБРАННЫЕ ПРОГРАММЫ И САЙТЫ"), 34);

        _tunNote = PageParts.Caption(L.T("Правила для программ надёжно работают в режиме TUN"), 30);
        Theme.Bind(_tunNote, () => Theme.Surface, () => Theme.PingMid);

        var addRow = new Panel { Dock = DockStyle.Top, Height = Theme.Px(96) };
        Theme.Bind(addRow, () => Theme.Surface);
        var program = PageParts.Button(L.T("+ Программа"), false);
        program.Click += (_, _) =>
        {
            var name = ProcessPicker.Show(FindForm());
            if (name != null)
                Add(RoutingRule.ForProcess(name));
        };
        var file = PageParts.Button(L.T("+ Файл .exe"), false);
        file.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = L.T("Программы (*.exe)|*.exe"), Title = L.T("Выбери программу") };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                Add(RoutingRule.ForProcess(dialog.FileName));
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
        Controls.Add(listTitle);
        Controls.Add(_reconnectBar);
        foreach (var card in _modes.Values.Reverse())
            Controls.Add(card);
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
            Add(new RoutingRule { Value = RoutingValues.Clean(value) }, false);

        _input.Clear();
        Changed();
    }

    private void Add(RoutingRule rule, bool notify = true)
    {
        if (rule.Target.Length == 0)
            return;

        if (_routing.Rules.Any(r => string.Equals(r.Value, rule.Value, StringComparison.OrdinalIgnoreCase)))
            return;

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
        var empty = _routing.Rules.Count == 0;
        if (empty)
            _routing.ListMode = RoutingMode.AllVpn;

        foreach (var pair in _modes)
        {
            pair.Value.Checked = pair.Key == _routing.ListMode;
            pair.Value.LockedHint = empty && pair.Key != RoutingMode.AllVpn ? L.T("Сначала добавь программу или сайт") : null;
        }

        _list.SuspendLayout();
        foreach (Control control in _list.Controls)
            control.Dispose();
        _list.Controls.Clear();

        foreach (var rule in _routing.Rules)
        {
            var card = new RuleCard(rule) { Dimmed = _routing.ListMode == RoutingMode.AllVpn };
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

    private void AddMode(RoutingMode mode, string title, string subtitle)
    {
        var card = new RadioCard(title, subtitle);
        card.Selected += (_, _) =>
        {
            _routing.ListMode = mode;
            Changed();
        };
        _modes[mode] = card;
    }

    private void ResizeCards()
    {
        var width = _list.Width - DisplayScale.ScrollBarWidth(this) - Theme.Px(6);
        if (width <= 0)
            return;

        foreach (Control control in _list.Controls)
            control.Width = width;
    }
}
