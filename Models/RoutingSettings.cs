using System.Text.Json.Serialization;

namespace Tunnelka.Models;

public enum RoutingMode
{
    AllVpn,
    DirectForListed,
    VpnForListed
}

public class RoutingSettings
{
    public RoutingMode ListMode { get; set; } = RoutingMode.AllVpn;
    public List<RoutingRule> Rules { get; set; } = new();

    [JsonPropertyName("Enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyEnabled { get; set; }

    [JsonIgnore]
    public RoutingMode EffectiveMode => Rules.Count == 0 ? RoutingMode.AllVpn : ListMode;

    [JsonIgnore]
    public IEnumerable<RoutingRule> ActiveRules => EffectiveMode == RoutingMode.AllVpn ? Enumerable.Empty<RoutingRule>() : Rules;

    [JsonIgnore]
    public string ListedAction => EffectiveMode == RoutingMode.VpnForListed ? RoutingRule.Proxy : RoutingRule.Direct;

    [JsonIgnore]
    public string Final => EffectiveMode == RoutingMode.VpnForListed ? RoutingRule.Direct : RoutingRule.Proxy;
}
