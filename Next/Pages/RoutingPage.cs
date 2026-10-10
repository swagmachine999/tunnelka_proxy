using Avalonia;
using Avalonia.Controls;
using Tunnelka.Models;

namespace Tunnelka.Next;

public sealed class RoutingPage : UserControl
{
    private readonly Session _session;
    private readonly RoutingSettings _routing;
    private readonly InfoStrip _reconnectStrip;
    private readonly InfoStrip _tunStrip;
    private readonly RoutingModeGroup _modes = new();
    private readonly RoutingRuleList _list = new();

    public RoutingPage(Session session, bool reconnectHint)
    {
        _session = session;
        _routing = session.Data.Routing;

        var reconnect = SettingsParts.Pill(L.T("Переподключить"), true);
        reconnect.Click += async (_, _) => await _session.Reconnect();
        _reconnectStrip = new InfoStrip(L.T("Правила изменены. Они заработают после переподключения"), "AccentStrongBrush", reconnect);
        _tunStrip = new InfoStrip(L.T("Правила для программ надёжно работают в режиме TUN"), "PingMidBrush");

        _modes.ModeSelected += OnModeSelected;
        _list.RuleRemoved += OnRuleRemoved;

        var adder = new RoutingAddPanel(() => _session.Data.UiScale);
        adder.ProcessChosen += OnProcessChosen;
        adder.SitesEntered += OnSitesEntered;

        var rules = new SettingsGroup(L.T("Правила"));
        rules.Add(adder);
        rules.Add(SettingsParts.Divider());
        rules.Add(_list);

        Content = new StackPanel { Spacing = 12, Children = { _reconnectStrip, _tunStrip, _modes, rules } };
        Refresh();
        ShowReconnectHint(reconnectHint);
    }

    public void ShowReconnectHint(bool show) => _reconnectStrip.IsVisible = show;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _session.ModeChanged += UpdateTunNote;
        UpdateTunNote();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _session.ModeChanged -= UpdateTunNote;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnModeSelected(RoutingMode mode)
    {
        _routing.ListMode = mode;
        Changed();
    }

    private void OnRuleRemoved(RoutingRule rule)
    {
        _routing.Rules.Remove(rule);
        Changed();
    }

    private void OnProcessChosen(string nameOrPath)
    {
        if (TryAdd(RoutingRule.ForProcess(nameOrPath)))
            Changed();
    }

    private void OnSitesEntered(string text)
    {
        var added = false;
        foreach (var value in RoutingValues.Split(text))
            added |= TryAdd(new RoutingRule { Value = RoutingValues.Clean(value) });

        if (added)
            Changed();
    }

    private bool TryAdd(RoutingRule rule)
    {
        if (rule.Target.Length == 0)
            return false;

        if (_routing.Rules.Any(r => string.Equals(r.Value, rule.Value, StringComparison.OrdinalIgnoreCase)))
            return false;

        _routing.Rules.Insert(0, rule);
        return true;
    }

    private void Changed()
    {
        Refresh();
        _session.OnRulesChanged();
    }

    private void Refresh()
    {
        var empty = _routing.Rules.Count == 0;
        if (empty)
            _routing.ListMode = RoutingMode.AllVpn;

        _modes.Show(_routing.ListMode, !empty);
        _list.Show(_routing.Rules.ToList(), _routing.ListMode == RoutingMode.AllVpn);
        UpdateTunNote();
    }

    private void UpdateTunNote() =>
        _tunStrip.IsVisible = !_session.Data.Tun && _routing.Rules.Any(r => r.IsProcess);
}
