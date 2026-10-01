using System.Text.Json;
using System.Web;
using VpnClient.Models;

namespace VpnClient.Parsing;

public static class LinkParser
{
    public static ProxyServer Parse(string link)
    {
        link = link.Trim();
        var schemeEnd = link.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd <= 0)
            throw new FormatException("Это не ссылка на сервер");

        var scheme = link[..schemeEnd].ToLowerInvariant();
        var server = scheme switch
        {
            "vless" => ParseUriBased(link, "vless"),
            "trojan" => ParseUriBased(link, "trojan"),
            "vmess" => ParseVmess(link),
            "ss" => ParseShadowsocks(link),
            _ => throw new FormatException($"Протокол {scheme} не поддерживается")
        };

        server.Link = link;
        if (string.IsNullOrWhiteSpace(server.Name))
            server.Name = $"{server.Address}:{server.Port}";

        return server;
    }

    public static List<ProxyServer> ParseMany(string text)
    {
        var result = new List<ProxyServer>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            try
            {
                result.Add(Parse(line));
            }
            catch (Exception)
            {
            }
        }

        return result;
    }

    private static ProxyServer ParseUriBased(string link, string protocol)
    {
        var uri = new Uri(link);
        var query = HttpUtility.ParseQueryString(uri.Query);
        string Get(string key, string fallback = "") => query[key] is { Length: > 0 } value ? value : fallback;

        var server = new ProxyServer
        {
            Protocol = protocol,
            Secret = Uri.UnescapeDataString(uri.UserInfo),
            Address = uri.Host.Trim('[', ']'),
            Port = uri.Port,
            Name = Uri.UnescapeDataString(uri.Fragment.TrimStart('#')),
            Method = protocol == "vless" ? Get("encryption", "none") : "",
            Flow = Get("flow"),
            Network = Get("type", "tcp"),
            Security = Get("security", protocol == "trojan" ? "tls" : "none"),
            Sni = Get("sni", Get("peer")),
            Fingerprint = Get("fp"),
            PublicKey = Get("pbk"),
            ShortId = Get("sid"),
            SpiderX = Get("spx"),
            Path = Get("path"),
            Host = Get("host"),
            ServiceName = Get("serviceName"),
            Mode = Get("mode"),
            HeaderType = Get("headerType"),
            Alpn = Get("alpn"),
            AllowInsecure = Get("allowInsecure") is "1" or "true"
        };

        if (server.Port <= 0)
            throw new FormatException("В ссылке нет порта");
        if (string.IsNullOrEmpty(server.Secret))
            throw new FormatException("В ссылке нет ключа");

        return server;
    }

    private static ProxyServer ParseVmess(string link)
    {
        var json = Base64Helper.Decode(link["vmess://".Length..]);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string Get(string key)
        {
            if (!root.TryGetProperty(key, out var value))
                return "";
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
        }

        var network = Get("net") is { Length: > 0 } net ? net : "tcp";

        return new ProxyServer
        {
            Protocol = "vmess",
            Name = Get("ps"),
            Address = Get("add"),
            Port = int.Parse(Get("port")),
            Secret = Get("id"),
            Method = Get("scy") is { Length: > 0 } scy ? scy : "auto",
            Network = network,
            HeaderType = Get("type"),
            Host = Get("host"),
            Path = Get("path"),
            ServiceName = network == "grpc" ? Get("path") : "",
            Security = Get("tls") == "tls" ? "tls" : "none",
            Sni = Get("sni"),
            Alpn = Get("alpn"),
            Fingerprint = Get("fp")
        };
    }

    private static ProxyServer ParseShadowsocks(string link)
    {
        var body = link["ss://".Length..];
        var name = "";

        var hashIndex = body.IndexOf('#');
        if (hashIndex >= 0)
        {
            name = Uri.UnescapeDataString(body[(hashIndex + 1)..]);
            body = body[..hashIndex];
        }

        var queryIndex = body.IndexOf('?');
        if (queryIndex >= 0)
            body = body[..queryIndex];

        body = body.TrimEnd('/');

        string userInfo;
        string hostPort;
        var atIndex = body.LastIndexOf('@');

        if (atIndex >= 0)
        {
            userInfo = Uri.UnescapeDataString(body[..atIndex]);
            hostPort = body[(atIndex + 1)..];
            if (!userInfo.Contains(':'))
                userInfo = Base64Helper.Decode(userInfo);
        }
        else
        {
            var decoded = Base64Helper.Decode(body);
            atIndex = decoded.LastIndexOf('@');
            userInfo = decoded[..atIndex];
            hostPort = decoded[(atIndex + 1)..];
        }

        var colon = userInfo.IndexOf(':');
        var portColon = hostPort.LastIndexOf(':');

        return new ProxyServer
        {
            Protocol = "ss",
            Name = name,
            Method = userInfo[..colon],
            Secret = userInfo[(colon + 1)..],
            Address = hostPort[..portColon].Trim('[', ']'),
            Port = int.Parse(hostPort[(portColon + 1)..])
        };
    }
}
