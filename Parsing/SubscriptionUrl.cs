namespace Tunnelka.Parsing;

public static class SubscriptionUrl
{
    private const string JsonSuffix = "/json";

    public static string Normalize(string url)
    {
        url = url.Trim();
        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
            return url;

        var tail = url.IndexOfAny(new[] { '?', '#' });
        var path = tail < 0 ? url : url.Substring(0, tail);
        var rest = tail < 0 ? "" : url.Substring(tail);

        path = path.TrimEnd('/');
        var start = path.Length - JsonSuffix.Length;
        if (start <= schemeEnd + 3 || !path.EndsWith(JsonSuffix, StringComparison.OrdinalIgnoreCase))
            return url;

        return path.Substring(0, start) + rest;
    }
}
