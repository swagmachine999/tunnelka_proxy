using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Tunnelka.Models;

namespace Tunnelka.Next;

public sealed class RoutingPage : UserControl
{
    private readonly Session _session;
    private readonly RoutingSettings _routing;
    private readonly Dictionary<RoutingMode, SettingsRadioCard> _modes = new();
    private readonly StackPanel _list = new();
    private readonly TextBlock _tunNote;
    private readonly TextBox _input;
    private readonly Border _reconnectBar;

    public RoutingPage(Session session, bool reconnectHint)
    {
        _session = session;
        _routing = session.Data.Routing;
        var panel = new StackPanel();

        AddMode(panel, RoutingMode.AllVpn, L.T("Всё через VPN"), L.T("Список не действует, весь трафик идёт через VPN"));
        AddMode(panel, RoutingMode.DirectForListed, L.T("Без VPN для выбранных"), L.T("Программы и сайты из списка идут напрямую, остальное через VPN"));
        AddMode(panel, RoutingMode.VpnForListed, L.T("VPN только для выбранных"), L.T("Через VPN идут только программы и сайты из списка"));

        _reconnectBar = BuildReconnectBar();
        panel.Children.Add(_reconnectBar);
        panel.Children.Add(SettingsParts.Caption(L.T("ВЫБРАННЫЕ ПРОГРАММЫ И САЙТЫ")));

        var program = SettingsParts.Pill(L.T("+ Программа"), false);
        program.HorizontalAlignment = HorizontalAlignment.Stretch;
        program.Margin = new Thickness(0, 0, 5, 0);
        program.Click += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
                return;

            var name = await ProcessPicker.Pick(owner, _session.Data.UiScale);
            if (name != null)
                Add(RoutingRule.ForProcess(name));
        };

        var file = SettingsParts.Pill(L.T("+ Файл .exe"), false);
        file.HorizontalAlignment = HorizontalAlignment.Stretch;
        file.Margin = new Thickness(5, 0, 0, 0);
        file.Click += async (_, _) => await PickFile();
        Grid.SetColumn(file, 1);

        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, 0, 0, 8) };
        buttons.Children.Add(program);
        buttons.Children.Add(file);
        panel.Children.Add(buttons);

        _input = new TextBox
        {
            Watermark = L.T("Сайт или IP, например sberbank.ru"),
            CornerRadius = new CornerRadius(12),
            MinHeight = 38,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                AddFromInput();
        };
        var add = SettingsParts.Pill(L.T("Добавить"), true);
        add.Margin = new Thickness(10, 0, 0, 0);
        add.Click += (_, _) => AddFromInput();
        Grid.SetColumn(add, 1);
        var inputRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 8) };
        inputRow.Children.Add(_input);
        inputRow.Children.Add(add);
        panel.Children.Add(inputRow);

        _tunNote = new TextBlock
        {
            Text = L.T("Правила для программ надёжно работают в режиме TUN"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, 4, 0, 8)
        };
        SettingsTheme.Paint(_tunNote, TextBlock.ForegroundProperty, "PingMidBrush");
        panel.Children.Add(_tunNote);
        panel.Children.Add(_list);

        Content = panel;
        Rebuild();
        UpdateNote();
        ShowReconnectHint(reconnectHint);
    }

    public void ShowReconnectHint(bool show) => _reconnectBar.IsVisible = show;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _session.ModeChanged += UpdateNote;
        UpdateNote();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _session.ModeChanged -= UpdateNote;
        base.OnDetachedFromVisualTree(e);
    }

    private Border BuildReconnectBar()
    {
        var text = new TextBlock
        {
            Text = L.T("Правила изменены. Они заработают после переподключения"),
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        SettingsTheme.Paint(text, TextBlock.ForegroundProperty, "AccentStrongBrush");

        var reconnect = SettingsParts.Pill(L.T("Переподключить"), true);
        reconnect.Margin = new Thickness(12, 0, 0, 0);
        reconnect.Click += async (_, _) => await _session.Reconnect();
        Grid.SetColumn(reconnect, 1);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(text);
        grid.Children.Add(reconnect);
        return new Border { Margin = new Thickness(0, 4, 0, 8), Child = grid, IsVisible = false };
    }

    private async Task PickFile()
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;

        var filter = new FilePickerFileType(L.T("Программы (*.exe)|*.exe").Split('|')[0]) { Patterns = new[] { "*.exe" } };
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = L.T("Выбери программу"),
            AllowMultiple = false,
            FileTypeFilter = new[] { filter }
        });
        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        if (path.Length > 0)
            Add(RoutingRule.ForProcess(path));
    }

    private void AddFromInput()
    {
        foreach (var value in RoutingValues.Split((_input.Text ?? "").Trim()))
            Add(new RoutingRule { Value = RoutingValues.Clean(value) }, false);

        _input.Text = "";
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
        UpdateNote();
        _session.OnRulesChanged();
    }

    private void UpdateNote() =>
        _tunNote.IsVisible = !_session.Data.Tun && _routing.Rules.Any(r => r.IsProcess);

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

        _list.Children.Clear();
        foreach (var rule in _routing.Rules.ToList())
        {
            var card = new RuleCard(rule, _routing.ListMode == RoutingMode.AllVpn);
            card.DeleteClicked += (_, _) =>
            {
                _routing.Rules.Remove(rule);
                Changed();
            };
            _list.Children.Add(card);
        }

        if (_routing.Rules.Count == 0)
            _list.Children.Add(SettingsParts.Caption(L.T("Список пуст. Добавь программу или сайт")));
    }

    private void AddMode(StackPanel panel, RoutingMode mode, string title, string subtitle)
    {
        var card = new SettingsRadioCard(title, subtitle);
        card.Selected += (_, _) =>
        {
            _routing.ListMode = mode;
            Changed();
        };
        _modes[mode] = card;
        panel.Children.Add(card);
    }
}
