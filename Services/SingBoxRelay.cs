using System.Text.Json;
using System.Text.Json.Nodes;
using Tunnelka.Models;

namespace Tunnelka.Services;

public static class SingBoxRelay
{
    public static bool Needs(ProxyServer server) => server.Protocol is "hysteria2" or "tuic";

    public static string Build(IReadOnlyList<(ProxyServer Server, int Port)> relays)
    {
        var inbounds = new JsonArray();
        var outbounds = new JsonArray();
        var rules = new JsonArray();

        for (var i = 0; i < relays.Count; i++)
        {
            inbounds.Add(new JsonObject
            {
                ["type"] = "socks",
                ["tag"] = $"in-{i}",
                ["listen"] = "127.0.0.1",
                ["listen_port"] = relays[i].Port
            });
            outbounds.Add(Outbound(relays[i].Server, $"out-{i}"));
            rules.Add(new JsonObject { ["inbound"] = $"in-{i}", ["outbound"] = $"out-{i}" });
        }

        var config = new JsonObject
        {
            ["log"] = new JsonObject { ["level"] = "warn" },
            ["dns"] = new JsonObject
            {
                ["servers"] = new JsonArray(new JsonObject { ["type"] = "local", ["tag"] = "local" })
            },
            ["inbounds"] = inbounds,
            ["outbounds"] = outbounds,
            ["route"] = new JsonObject
            {
                ["default_domain_resolver"] = "local",
                ["rules"] = rules
            }
        };

        return config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonObject Outbound(ProxyServer s, string tag)
    {
        var outbound = new JsonObject
        {
            ["type"] = s.Protocol,
            ["tag"] = tag,
            ["server"] = s.Address,
            ["server_port"] = s.Port,
            ["tls"] = Tls(s)
        };

        if (s.Protocol == "hysteria2")
        {
            outbound["password"] = s.Secret;
            if (s.Obfs.Length > 0)
                outbound["obfs"] = new JsonObject { ["type"] = s.Obfs, ["password"] = s.ObfsPassword };
        }
        else
        {
            outbound["uuid"] = s.Secret;
            outbound["password"] = s.Password;
            outbound["congestion_control"] = s.Congestion.Length > 0 ? s.Congestion : "bbr";
            if (s.UdpRelayMode.Length > 0)
                outbound["udp_relay_mode"] = s.UdpRelayMode;
        }

        return outbound;
    }

    private static JsonObject Tls(ProxyServer s)
    {
        var tls = new JsonObject
        {
            ["enabled"] = true,
            ["server_name"] = s.Sni.Length > 0 ? s.Sni : s.Address,
            ["alpn"] = new JsonArray((s.Alpn.Length > 0 ? s.Alpn : "h3").Split(',').Select(a => (JsonNode)a.Trim()).ToArray())
        };
        if (s.AllowInsecure)
            tls["insecure"] = true;
        return tls;
    }
}
