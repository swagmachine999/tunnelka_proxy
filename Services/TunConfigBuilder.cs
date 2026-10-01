using System.Text.Json;
using System.Text.Json.Nodes;
using VpnClient.Models;

namespace VpnClient.Services;

public static class TunConfigBuilder
{
    public static string Build(int socksPort, IEnumerable<RoutingRule> rules)
    {
        var routeRules = new JsonArray
        {
            new JsonObject { ["action"] = "sniff" },
            new JsonObject { ["protocol"] = "dns", ["action"] = "hijack-dns" },
            new JsonObject
            {
                ["process_path"] = new JsonArray(JsonValue.Create(XrayRunner.XrayPath), JsonValue.Create(XrayRunner.SingBoxPath)),
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
                    new JsonObject { ["type"] = "local", ["tag"] = "local" }
                },
                ["final"] = "remote",
                ["strategy"] = "ipv4_only"
            },
            ["inbounds"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "tun",
                    ["tag"] = "tun-in",
                    ["interface_name"] = "Tunnelka",
                    ["address"] = new JsonArray(JsonValue.Create("172.19.0.1/30")),
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
