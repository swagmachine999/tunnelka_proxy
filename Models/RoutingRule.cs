namespace Tunnelka.Models;

public class RoutingRule
{
    public const string Proxy = "proxy";
    public const string Direct = "direct";
    public const string Block = "block";

    public bool Enabled { get; set; } = true;
    public string Values { get; set; } = "";
    public string Action { get; set; } = Direct;
}
