namespace Tunnelka.Models;

public class TrafficMinute
{
    public long Minute { get; set; }
    public long ProxyDown { get; set; }
    public long ProxyUp { get; set; }
    public long DirectDown { get; set; }
    public long DirectUp { get; set; }
}
