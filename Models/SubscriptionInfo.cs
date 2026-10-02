namespace Tunnelka.Models;

public class SubscriptionInfo
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime? Expire { get; set; }
    public long Upload { get; set; }
    public long Download { get; set; }
    public long Total { get; set; }
    public int UpdateIntervalHours { get; set; } = 1;
    public string Announce { get; set; } = "";
    public string SupportUrl { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public bool Collapsed { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool ExpiresSoon => Expire != null && Expire.Value - DateTime.Now < TimeSpan.FromDays(3);

    public static SubscriptionInfo Placeholder(string url) => new()
    {
        Url = url,
        Title = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url
    };
}
