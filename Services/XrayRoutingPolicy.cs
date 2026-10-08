using Tunnelka.Models;

namespace Tunnelka.Services;

public static class XrayRoutingPolicy
{
    public static RoutingSettings For(bool tun, RoutingSettings routing) =>
        tun ? new RoutingSettings() : routing;
}
