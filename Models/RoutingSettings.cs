using System.Text.Json.Serialization;

namespace Tunnelka.Models;

public class RoutingSettings
{
    public bool Enabled { get; set; } = true;
    public List<RoutingRule> Rules { get; set; } = new();

    [JsonIgnore]
    public IEnumerable<RoutingRule> ActiveRules => Enabled ? Rules : Enumerable.Empty<RoutingRule>();

    [JsonIgnore]
    public bool OnlyVpnListed => Enabled && Rules.Count > 0 && Rules.All(r => r.Action == RoutingRule.Proxy);

    [JsonIgnore]
    public string Final => OnlyVpnListed ? RoutingRule.Direct : RoutingRule.Proxy;
}
