using System.Text.Json;
using System.Text.Json.Nodes;
using VpnClient.Models;

namespace VpnClient.Services;

public static class XrayConfigBuilder
{
    public const int SocksPort = 10808;
    public const int HttpPort = 10809;

    public static string Build(ProxyServer server)
    {
        var config = new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = "warning" },
            ["inbounds"] = new JsonArray
            {
                Inbound("socks-in", "socks", SocksPort, new JsonObject { ["udp"] = true }),
                Inbound("http-in", "http", HttpPort, new JsonObject())
            },
            ["outbounds"] = new JsonArray
            {
                Outbound(server),
                new JsonObject { ["tag"] = "direct", ["protocol"] = "freedom" },
                new JsonObject { ["tag"] = "block", ["protocol"] = "blackhole" }
            },
            ["routing"] = new JsonObject
            {
                ["domainStrategy"] = "AsIs",
                ["rules"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "field",
                        ["ip"] = new JsonArray("127.0.0.0/8", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"),
                        ["outboundTag"] = "direct"
                    }
                }
            }
        };

        return config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonObject Inbound(string tag, string protocol, int port, JsonObject settings) => new()
    {
        ["tag"] = tag,
        ["listen"] = "127.0.0.1",
        ["port"] = port,
        ["protocol"] = protocol,
        ["settings"] = settings,
        ["sniffing"] = new JsonObject
        {
            ["enabled"] = true,
            ["destOverride"] = new JsonArray("http", "tls")
        }
    };

    private static JsonObject Outbound(ProxyServer server)
    {
        var outbound = new JsonObject
        {
            ["tag"] = "proxy",
            ["protocol"] = server.Protocol == "ss" ? "shadowsocks" : server.Protocol,
            ["settings"] = Settings(server)
        };

        if (server.Protocol != "ss")
            outbound["streamSettings"] = Stream(server);

        return outbound;
    }

    private static JsonObject Settings(ProxyServer s) => s.Protocol switch
    {
        "vless" => new JsonObject
        {
            ["vnext"] = new JsonArray(new JsonObject
            {
                ["address"] = s.Address,
                ["port"] = s.Port,
                ["users"] = new JsonArray(new JsonObject
                {
                    ["id"] = s.Secret,
                    ["encryption"] = Or(s.Method, "none"),
                    ["flow"] = s.Flow
                })
            })
        },
        "vmess" => new JsonObject
        {
            ["vnext"] = new JsonArray(new JsonObject
            {
                ["address"] = s.Address,
                ["port"] = s.Port,
                ["users"] = new JsonArray(new JsonObject
                {
                    ["id"] = s.Secret,
                    ["alterId"] = 0,
                    ["security"] = Or(s.Method, "auto")
                })
            })
        },
        "trojan" => new JsonObject
        {
            ["servers"] = new JsonArray(new JsonObject
            {
                ["address"] = s.Address,
                ["port"] = s.Port,
                ["password"] = s.Secret
            })
        },
        "ss" => new JsonObject
        {
            ["servers"] = new JsonArray(new JsonObject
            {
                ["address"] = s.Address,
                ["port"] = s.Port,
                ["method"] = s.Method,
                ["password"] = s.Secret
            })
        },
        _ => throw new NotSupportedException($"Протокол {s.Protocol} не поддерживается")
    };

    private static JsonObject Stream(ProxyServer s)
    {
        var network = s.Network switch
        {
            "" or "raw" => "tcp",
            "splithttp" => "xhttp",
            var other => other
        };

        var stream = new JsonObject
        {
            ["network"] = network,
            ["security"] = s.Security
        };

        switch (network)
        {
            case "ws":
                stream["wsSettings"] = new JsonObject { ["path"] = Or(s.Path, "/"), ["host"] = s.Host };
                break;
            case "httpupgrade":
                stream["httpupgradeSettings"] = new JsonObject { ["path"] = Or(s.Path, "/"), ["host"] = s.Host };
                break;
            case "xhttp":
                stream["xhttpSettings"] = new JsonObject { ["path"] = Or(s.Path, "/"), ["host"] = s.Host, ["mode"] = Or(s.Mode, "auto") };
                break;
            case "grpc":
                stream["grpcSettings"] = new JsonObject { ["serviceName"] = s.ServiceName, ["multiMode"] = s.Mode == "multi" };
                break;
            case "tcp" when s.HeaderType == "http":
                stream["tcpSettings"] = new JsonObject
                {
                    ["header"] = new JsonObject
                    {
                        ["type"] = "http",
                        ["request"] = new JsonObject
                        {
                            ["path"] = new JsonArray(Or(s.Path, "/")),
                            ["headers"] = new JsonObject { ["Host"] = new JsonArray(Or(s.Host, s.Address)) }
                        }
                    }
                };
                break;
        }

        if (s.Security == "tls")
        {
            var tls = new JsonObject
            {
                ["serverName"] = Or(s.Sni, Or(s.Host, s.Address)),
                ["fingerprint"] = Or(s.Fingerprint, "chrome")
            };
            if (s.AllowInsecure)
                tls["allowInsecure"] = true;
            if (s.Alpn.Length > 0)
                tls["alpn"] = new JsonArray(s.Alpn.Split(',').Select(a => (JsonNode)a.Trim()).ToArray());
            stream["tlsSettings"] = tls;
        }
        else if (s.Security == "reality")
        {
            stream["realitySettings"] = new JsonObject
            {
                ["serverName"] = s.Sni,
                ["fingerprint"] = Or(s.Fingerprint, "chrome"),
                ["publicKey"] = s.PublicKey,
                ["shortId"] = s.ShortId,
                ["spiderX"] = s.SpiderX
            };
        }

        return stream;
    }

    private static string Or(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;
}
