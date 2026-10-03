using System.Globalization;
using System.Net;

namespace Tunnelka.Models;

public static class RoutingValues
{
    public static IEnumerable<string> Split(string values)
    {
        foreach (var chunk in values.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = chunk.Trim();
            if (trimmed.StartsWith(RoutingRule.ProcessPrefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return trimmed;
                continue;
            }

            foreach (var word in trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                yield return word;
        }
    }

    public static bool IsIp(string value)
    {
        if (value.StartsWith("geoip:", StringComparison.OrdinalIgnoreCase))
            return true;

        var address = value.Split('/')[0];
        return IPAddress.TryParse(address, out _) && (address.Contains('.') || address.Contains(':'));
    }

    public static string Clean(string value)
    {
        value = value.Trim().ToLowerInvariant();
        if (IsIp(value))
            return value;

        foreach (var prefix in new[] { "regexp:", "keyword:", "geosite:", "full:", "domain:" })
        {
            if (value.StartsWith(prefix))
                return prefix is "regexp:" or "keyword:" or "geosite:" ? value : prefix + Host(value.Substring(prefix.Length));
        }

        return Host(value);
    }

    private static string Host(string value)
    {
        if (value.Contains("://"))
            value = value.Substring(value.IndexOf("://", StringComparison.Ordinal) + 3);

        value = value.Split('/')[0].TrimStart('*').TrimStart('.');
        return value.Any(c => c > 127) ? new IdnMapping().GetAscii(value) : value;
    }
}
