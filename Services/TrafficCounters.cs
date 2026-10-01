namespace VpnClient.Services;

public sealed class TrafficCounters
{
    public long ProxyDown { get; set; }
    public long ProxyUp { get; set; }
    public long DirectDown { get; set; }
    public long DirectUp { get; set; }
}
