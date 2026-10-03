using System.Text.Json;
using System.Text.Json.Nodes;
using Tunnelka.Models;

namespace Tunnelka.Services;

public static class TunConfigBuilder
{
    public const string InterfaceName = "Tunnelka";
    public const string Address = "172.19.0.1";

    private static readonly string[] DirectProcesses = { "xray.exe", "sing-box.exe", "Tunnelka.exe" };

    public static string Build(int socksPort, RoutingSettings routing, string serverHost, string? physicalInterface)
    {
        var routeRules = new JsonArray
        {
            new JsonObject { ["action"] = "sniff" },
            new JsonObject { ["protocol"] = "dns", ["action"] = "hijack-dns" },
            new JsonObject
            {
                ["process_name"] = new JsonArray(DirectProcesses.Select(p => (JsonNode)p).ToArray()),
                ["outbound"] = "direct"
            },
            new JsonObject { ["ip_is_private"] = true, ["outbound"] = "direct" }
        };

        foreach (var rule in routing.ActiveRules)
        {
            if (Rule(rule, routing) is { } node)
                routeRules.Add(node);
        }

        var config = new JsonObject
        {
            ["log"] = new JsonObject { ["level"] = "warn" },
            ["dns"] = new JsonObject
            {
                ["servers"] = new JsonArray
                {
                    new JsonObject { ["type"] = "tcp", ["tag"] = "remote", ["server"] = "1.1.1.1", ["detour"] = "proxy" },
                    new JsonObject { ["type"] = "udp", ["tag"] = "local", ["server"] = "77.88.8.8" }
                },
                ["rules"] = DnsRules(serverHost),
                ["final"] = "remote",
                ["strategy"] = "ipv4_only"
            },
            ["inbounds"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "tun",
                    ["tag"] = "tun-in",
                    ["interface_name"] = InterfaceName,
                    ["address"] = new JsonArray(JsonValue.Create(Address + "/30")),
                    ["mtu"] = 9000,
                    ["auto_route"] = true,
                    ["strict_route"] = true,
                    ["stack"] = "mixed"
                }
            },
            ["outbounds"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "socks",
                    ["tag"] = "proxy",
                    ["server"] = "127.0.0.1",
                    ["server_port"] = socksPort,
                    ["version"] = "5"
                },
                new JsonObject { ["type"] = "direct", ["tag"] = "direct" }
            },
            ["route"] = new JsonObject
            {
                ["default_domain_resolver"] = "local",
                ["rules"] = routeRules,
                ["final"] = routing.Final == RoutingRule.Direct ? "direct" : "proxy"
            }
        };

        var route = config["route"]!.AsObject();
        if (physicalInterface != null)
            route["default_interface"] = physicalInterface;
        else
            route["auto_detect_interface"] = true;

        return config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonArray DnsRules(string serverHost)
    {
        var rules = new JsonArray
        {
            new JsonObject
            {
                ["process_name"] = new JsonArray(DirectProcesses.Select(p => (JsonNode)p).ToArray()),
                ["server"] = "local"
            }
        };

        if (!System.Net.IPAddress.TryParse(serverHost, out _) && serverHost.Length > 0)
            rules.Add(new JsonObject { ["domain"] = new JsonArray(JsonValue.Create(serverHost)), ["server"] = "local" });

        return rules;
    }

    private static JsonObject? Rule(RoutingRule rule, RoutingSettings routing)
    {
        var (field, value) = Match(rule);
        if (field == null)
            return null;

        return new JsonObject
        {
            [field] = new JsonArray(JsonValue.Create(value)),
            ["outbound"] = routing.ListedAction == RoutingRule.Proxy ? "proxy" : "direct"
        };
    }

    private static (string? Field, string Value) Match(RoutingRule rule)
    {
        if (rule.IsProcess)
            return ("process_name", RoutingRule.ProcessName(rule.Target));

        var value = rule.Value;
        if (RoutingValues.IsIp(value))
            return value.StartsWith("geoip:") ? (null, value) : ("ip_cidr", value.Contains('/') ? value : value + (value.Contains(':') ? "/128" : "/32"));

        foreach (var (prefix, field) in new[] { ("full:", "domain"), ("domain:", "domain_suffix"), ("keyword:", "domain_keyword"), ("regexp:", "domain_regex") })
        {
            if (value.StartsWith(prefix))
                return (field, value[prefix.Length..]);
        }

        return value.StartsWith("geosite:") ? (null, value) : ("domain_suffix", value);
    }
}
