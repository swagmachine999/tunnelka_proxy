using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using VpnClient.Models;

namespace VpnClient.Services;

public static class XrayConfigBuilder
{
    public const int PreferredSocksPort = 10808;

    public static int SocksPort { get; private set; } = PreferredSocksPort;
    public static int HttpPort { get; private set; } = PreferredSocksPort + 1;
    public static int MetricsPort { get; private set; } = PreferredSocksPort + 5;

    public static void ChoosePorts()
    {
        for (var start = PreferredSocksPort; start < PreferredSocksPort + 200; start += 10)
        {
            if (IsFree(start) && IsFree(start + 1) && IsFree(start + 5))
            {
                SocksPort = start;
                HttpPort = start + 1;
                MetricsPort = start + 5;
                return;
            }
        }
    }

    private static bool IsFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static string Build(ProxyServer server, IEnumerable<RoutingRule> rules)
    {
        var routingRules = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "field",
                ["ip"] = new JsonArray("127.0.0.0/8", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"),
                ["outboundTag"] = "direct"
            }
        };

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            foreach (var node in RuleNodes(rule))
                routingRules.Add(node);
        }

        var config = new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = "warning", ["access"] = "none" },
            ["stats"] = new JsonObject(),
            ["metrics"] = new JsonObject { ["tag"] = "metrics", ["listen"] = $"127.0.0.1:{MetricsPort}" },
            ["policy"] = new JsonObject
            {
                ["system"] = new JsonObject
                {
                    ["statsOutboundUplink"] = true,
                    ["statsOutboundDownlink"] = true
                }
            },
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
                ["rules"] = routingRules
            }
        };

        return config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public const string ProcessPrefix = "process:";

    public static IEnumerable<string> SplitValues(string values)
    {
        foreach (var chunk in values.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = chunk.Trim();
            if (trimmed.StartsWith(ProcessPrefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return trimmed;
                continue;
            }

            foreach (var word in trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                yield return word;
        }
    }

    private static IEnumerable<JsonObject> RuleNodes(RoutingRule rule)
    {
        var domains = new JsonArray();
        var ips = new JsonArray();
        var processes = new JsonArray();

        foreach (var raw in SplitValues(rule.Values))
        {
            if (raw.StartsWith(ProcessPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var process = Process(raw.Substring(ProcessPrefix.Length));
                if (process.Length > 0)
                    processes.Add(JsonValue.Create(process));
                continue;
            }

            var value = raw.ToLowerInvariant();
            if (IsIp(value))
                ips.Add(JsonValue.Create(value));
            else
                domains.Add(JsonValue.Create(Domain(value)));
        }

        var tag = rule.Action switch
        {
            RoutingRule.Proxy => "proxy",
            RoutingRule.Block => "block",
            _ => "direct"
        };

        if (processes.Count > 0)
            yield return new JsonObject { ["type"] = "field", ["process"] = processes, ["outboundTag"] = tag };
        if (domains.Count > 0)
            yield return new JsonObject { ["type"] = "field", ["domain"] = domains, ["outboundTag"] = tag };
        if (ips.Count > 0)
            yield return new JsonObject { ["type"] = "field", ["ip"] = ips, ["outboundTag"] = tag };
    }

    private static string Process(string value)
    {
        value = value.Trim().Trim('"').Replace('\\', '/');
        if (value.Contains('/'))
            return value;

        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? value.Substring(0, value.Length - 4) : value;
    }

    private static bool IsIp(string value)
    {
        if (value.StartsWith("geoip:"))
            return true;

        var address = value.Split('/')[0];
        return IPAddress.TryParse(address, out _) && (address.Contains('.') || address.Contains(':'));
    }

    private static string Domain(string value)
    {
        foreach (var prefix in new[] { "regexp:", "keyword:", "geosite:" })
        {
            if (value.StartsWith(prefix))
                return value;
        }

        var kind = "domain:";
        foreach (var prefix in new[] { "domain:", "full:" })
        {
            if (value.StartsWith(prefix))
            {
                kind = prefix;
                value = value[prefix.Length..];
            }
        }

        if (value.Contains("://"))
            value = value[(value.IndexOf("://", StringComparison.Ordinal) + 3)..];

        value = value.Split('/')[0].TrimStart('*').TrimStart('.');

        if (value.Any(c => c > 127))
            value = new IdnMapping().GetAscii(value);

        return kind + value;
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

    private static JsonObject Outbound(ProxyServer server) => Outbound(server, "proxy");

    public static JsonObject Outbound(ProxyServer server, string tag)
    {
        var outbound = new JsonObject
        {
            ["tag"] = tag,
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
        _ => throw new NotSupportedException(L.F("Протокол {0} не поддерживается", s.Protocol))
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
