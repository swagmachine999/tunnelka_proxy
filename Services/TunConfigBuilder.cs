using System.Text.Json;
using System.Text.Json.Nodes;
using Tunnelka.Models;

namespace Tunnelka.Services;

public static class TunConfigBuilder
{
    public const string InterfaceName = "Tunnelka";
    public const string Address = "172.19.0.1";

    private static readonly string[] DirectProcesses = { "xray.exe", "sing-box.exe", "Tunnelka.exe" };

    public static string Build(int socksPort, IEnumerable<RoutingRule> rules, string serverHost)
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

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            var names = new JsonArray();
            var paths = new JsonArray();
            foreach (var value in XrayConfigBuilder.SplitValues(rule.Values))
            {
                if (!value.StartsWith(XrayConfigBuilder.ProcessPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var target = value.Substring(XrayConfigBuilder.ProcessPrefix.Length).Trim().Trim('"');
                if (target.Contains('/') || target.Contains('\\'))
                    paths.Add(JsonValue.Create(target.Replace('/', '\\')));
                else
                    names.Add(JsonValue.Create(target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? target : target + ".exe"));
            }

            if (names.Count > 0)
                routeRules.Add(Rule("process_name", names, rule.Action));
            if (paths.Count > 0)
                routeRules.Add(Rule("process_path", paths, rule.Action));
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
                ["auto_detect_interface"] = true,
                ["default_domain_resolver"] = "local",
                ["rules"] = routeRules,
                ["final"] = "proxy"
            }
        };

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

    private static JsonObject Rule(string field, JsonArray values, string action)
    {
        var rule = new JsonObject { [field] = values };
        if (action == RoutingRule.Block)
            rule["action"] = "reject";
        else
            rule["outbound"] = action == RoutingRule.Direct ? "direct" : "proxy";
        return rule;
    }
}
