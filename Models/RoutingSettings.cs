using System.Text.Json.Serialization;

namespace Tunnelka.Models;

public enum RoutingMode
{
    SomeDirect,
    SomeViaVpn
}

public class RoutingSettings
{
    public bool Enabled { get; set; } = true;
    public RoutingMode Mode { get; set; } = RoutingMode.SomeDirect;
    public List<RoutingRule> Rules { get; set; } = new();

    [JsonIgnore]
    public IEnumerable<RoutingRule> ActiveRules => Enabled ? Rules : Enumerable.Empty<RoutingRule>();

    [JsonIgnore]
    public string Final => Enabled && Mode == RoutingMode.SomeViaVpn ? RoutingRule.Direct : RoutingRule.Proxy;

    [JsonIgnore]
    public string NewRuleAction => Mode == RoutingMode.SomeViaVpn ? RoutingRule.Proxy : RoutingRule.Direct;
}
