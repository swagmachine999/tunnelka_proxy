using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Tunnelka.Services;

public static class NetworkAdapters
{
    private static readonly string[] TunnelMarks = { "wintun", "sing-tun", "tap-", "tap ", "wireguard", "tunnel", "vpn" };

    public static string? Physical() =>
        Active()
            .Where(n => n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet)
            .Where(n => !IsTunnel(n) && n.Description.IndexOf("Hyper-V", StringComparison.OrdinalIgnoreCase) < 0)
            .OrderBy(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .Select(n => n.Name)
            .FirstOrDefault();

    public static string? OtherTunnel() =>
        Active()
            .Where(n => IsTunnel(n) && !HasAddress(n, TunConfigBuilder.Address))
            .Select(n => n.Name)
            .FirstOrDefault();

    public static bool HasAddress(NetworkInterface adapter, string address) =>
        adapter.GetIPProperties().UnicastAddresses.Any(a => a.Address.ToString() == address);

    private static IEnumerable<NetworkInterface> Active()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(n => n.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(System.Net.IPAddress.Any)))
                .ToList();
        }
        catch (NetworkInformationException)
        {
            return Array.Empty<NetworkInterface>();
        }
    }

    private static bool IsTunnel(NetworkInterface adapter)
    {
        var text = (adapter.Name + " " + adapter.Description).ToLowerInvariant();
        return TunnelMarks.Any(text.Contains);
    }
}
