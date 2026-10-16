using Tunnelka.Models;

namespace Tunnelka.Services.Privileged;

public sealed record TunParameters(int SocksPort, string ServerHost, string? PhysicalInterface, RoutingSettings Routing, string SocksUser = "", string SocksPassword = "");

public static class TunRequestValidator
{
    private const int MaxRules = 2000;
    private const int MaxValueLength = 300;
    private const int MaxHostLength = 253;
    private const int MaxInterfaceLength = 100;

    public static bool TryValidate(ServiceRequest request, out TunParameters? parameters, out string error)
    {
        parameters = null;

        if (request.SocksPort is < 1024 or > 65535)
            return Reject("socks port out of range", out error);

        if (!IsToken(request.SocksUser) || !IsToken(request.SocksPassword))
            return Reject("invalid socks credentials", out error);

        if (!IsHost(request.ServerHost))
            return Reject("invalid server host", out error);

        if (request.PhysicalInterface != null && !IsPlain(request.PhysicalInterface, MaxInterfaceLength, false))
            return Reject("invalid interface name", out error);

        if (!Enum.TryParse<RoutingMode>(request.Mode, false, out var mode) || !Enum.IsDefined(mode))
            return Reject("invalid routing mode", out error);

        if (request.Rules.Count > MaxRules)
            return Reject("too many rules", out error);

        var rules = new List<RoutingRule>();
        foreach (var value in request.Rules)
        {
            if (!IsPlain(value, MaxValueLength, true))
                return Reject("invalid rule", out error);

            rules.Add(new RoutingRule { Value = value });
        }

        parameters = new TunParameters(request.SocksPort, request.ServerHost, request.PhysicalInterface,
            new RoutingSettings { ListMode = mode, Rules = rules }, request.SocksUser, request.SocksPassword);
        error = "";
        return true;
    }

    private static bool IsToken(string value) =>
        value.Length is >= 8 and <= 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z');

    private static bool Reject(string reason, out string error)
    {
        error = reason;
        return false;
    }

    private static bool IsHost(string value)
    {
        if (value.Length > MaxHostLength)
            return false;

        foreach (var c in value)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or ':'))
                return false;
        }

        return true;
    }

    private static bool IsPlain(string value, int maxLength, bool allowEscapes)
    {
        if (value.Length > maxLength || value.Length == 0)
            return false;

        foreach (var c in value)
        {
            if (char.IsControl(c) || (!allowEscapes && (c == '"' || c == '\\')))
                return false;
        }

        return true;
    }
}
