namespace VpnClient.Models;

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
}
