using System.Text.Json.Serialization;
using Tunnelka.Models;

namespace Tunnelka.Storage;

public class AppData
{
    public List<ProxyServer> Servers { get; set; } = new();
    public List<SubscriptionInfo> Profiles { get; set; } = new();
    public bool Tun { get; set; }
    public string LastServerLink { get; set; } = "";
    public bool DarkTheme { get; set; }
    public int SpeedInterval { get; set; } = 3;
    public int UiScale { get; set; } = 90;
    public bool RealPing { get; set; } = true;
    public string PingUrl { get; set; } = "https://www.gstatic.com/generate_204";
    public RoutingSettings Routing { get; set; } = new();

    [JsonPropertyName("Rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RoutingRule>? LegacyRules { get; set; }
    public long TotalDownload { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDirectDownload { get; set; }
    public long TotalDirectUpload { get; set; }
    public List<TrafficMinute> TrafficMinutes { get; set; } = new();
    public int StatsPeriod { get; set; } = 60;
    public string Language { get; set; } = "ru";
    public bool RefreshOnStart { get; set; }
    public bool PingOnStart { get; set; }
    public bool AutoStart { get; set; }
    public bool ConnectOnStart { get; set; }
    public bool KillSwitch { get; set; }
    public OverlayOptions Overlay { get; set; } = new();

    [JsonPropertyName("Subscriptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacySubscriptions { get; set; }
}
