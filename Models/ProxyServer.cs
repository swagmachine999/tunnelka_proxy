using System.Text.Json.Serialization;

namespace VpnClient.Models;

public class ProxyServer
{
    public string Protocol { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public int Port { get; set; }
    public string Secret { get; set; } = "";
    public string Method { get; set; } = "";
    public string Flow { get; set; } = "";
    public string Network { get; set; } = "tcp";
    public string Security { get; set; } = "none";
    public string Sni { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string ShortId { get; set; } = "";
    public string SpiderX { get; set; } = "";
    public string Path { get; set; } = "";
    public string Host { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string Mode { get; set; } = "";
    public string HeaderType { get; set; } = "";
    public string Alpn { get; set; } = "";
    public bool AllowInsecure { get; set; }
    public string Link { get; set; } = "";
    public string? SubscriptionUrl { get; set; }

    [JsonIgnore]
    public int? PingMs { get; set; }
}
