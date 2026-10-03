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
    public IEnumerable<RoutingRule> ActiveRules => ListMode == RoutingMode.AllVpn ? Enumerable.Empty<RoutingRule>() : Rules;

    [JsonIgnore]
    public string ListedAction => ListMode == RoutingMode.VpnForListed ? RoutingRule.Proxy : RoutingRule.Direct;

    [JsonIgnore]
    public string Final => ListMode == RoutingMode.VpnForListed ? RoutingRule.Direct : RoutingRule.Proxy;
}
