using Tunnelka.Models;

namespace Tunnelka.Next;

public sealed class RoutingModeGroup : SettingsGroup
{
    private readonly Dictionary<RoutingMode, SettingsRadioCard> _cards = new();

    public RoutingModeGroup() : base(L.T("Режим списка"))
    {
        AddCard(RoutingMode.AllVpn, L.T("Всё через VPN"), L.T("Список не действует, весь трафик идёт через VPN"));
        AddCard(RoutingMode.DirectForListed, L.T("Без VPN для выбранных"), L.T("Программы и сайты из списка идут напрямую, остальное через VPN"));
        AddCard(RoutingMode.VpnForListed, L.T("VPN только для выбранных"), L.T("Через VPN идут только программы и сайты из списка"));
    }

    public event Action<RoutingMode>? ModeSelected;

    public void Show(RoutingMode mode, bool hasRules)
    {
        foreach (var pair in _cards)
        {
            pair.Value.Checked = pair.Key == mode;
            pair.Value.LockedHint = !hasRules && pair.Key != RoutingMode.AllVpn ? L.T("Сначала добавь программу или сайт") : null;
        }
    }

    private void AddCard(RoutingMode mode, string title, string subtitle)
    {
        var card = new SettingsRadioCard(title, subtitle);
        card.Selected += (_, _) => ModeSelected?.Invoke(mode);
        _cards[mode] = card;
        Add(card);
    }
}
