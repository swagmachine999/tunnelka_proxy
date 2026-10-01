using System.Globalization;
using System.Text;
using VpnClient.Models;

namespace VpnClient.UI;

public sealed class NamePart
{
    public NamePart(string text, bool isSymbol)
    {
        Text = text;
        IsSymbol = isSymbol;
    }

    public string Text { get; }
    public bool IsSymbol { get; }
}

public static class ServerText
{
    public static string? CountryCode(string name)
    {
        var letters = new StringBuilder();
        for (var i = 0; i < name.Length - 1; i++)
        {
            if (!char.IsHighSurrogate(name[i]) || !char.IsLowSurrogate(name[i + 1]))
                continue;

            var codePoint = char.ConvertToUtf32(name[i], name[i + 1]);
            if (IsRegionalIndicator(codePoint))
            {
                letters.Append((char)('A' + codePoint - 0x1F1E6));
                if (letters.Length == 2)
                    return letters.ToString();
            }
            else
            {
                letters.Clear();
            }

            i++;
        }

        return null;
    }

    public static List<NamePart> Parts(ProxyServer server)
    {
        var parts = new List<NamePart>();
        var text = new StringBuilder();
        var name = server.Name;

        void FlushText()
        {
            var value = string.Join(" ", text.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if (value.Length > 0)
                parts.Add(new NamePart(value, false));
            text.Clear();
        }

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            string symbol;

            if (char.IsHighSurrogate(c) && i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
            {
                var codePoint = char.ConvertToUtf32(c, name[i + 1]);
                i++;
                if (IsRegionalIndicator(codePoint) || (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF))
                    continue;
                symbol = char.ConvertFromUtf32(codePoint);
            }
            else if (c == '️' || c == '︎' || c == '‍' || char.IsSurrogate(c))
            {
                continue;
            }
            else if (char.GetUnicodeCategory(c) == UnicodeCategory.OtherSymbol)
            {
                symbol = c.ToString();
            }
            else
            {
                text.Append(char.IsWhiteSpace(c) ? ' ' : c);
                continue;
            }

            FlushText();
            parts.Add(new NamePart(symbol, true));
        }

        FlushText();

        if (!parts.Any(p => !p.IsSymbol))
            parts.Add(new NamePart($"{server.Address}:{server.Port}", false));

        return parts;
    }

    public static string CleanName(ProxyServer server) =>
        string.Join(" ", Parts(server).Where(p => !p.IsSymbol).Select(p => p.Text));

    public static string Describe(ProxyServer server)
    {
        if (server.Protocol == "ss")
            return "SHADOWSOCKS";

        var parts = new List<string> { server.Protocol.ToUpperInvariant() };
        if (!string.IsNullOrEmpty(server.Network))
            parts.Add(server.Network.ToUpperInvariant());
        if (!string.IsNullOrEmpty(server.Security) && server.Security != "none")
            parts.Add(server.Security.ToUpperInvariant());

        return string.Join(" / ", parts);
    }

    public static string Plural(int count, string one, string few, string many)
    {
        var mod100 = count % 100;
        var mod10 = count % 10;
        var word = mod100 is >= 11 and <= 14 ? many : mod10 switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many
        };
        return $"{count} {word}";
    }

    public static string Bytes(double bytes)
    {
        string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
        var unit = 0;
        while (bytes >= 1024 && unit < units.Length - 1)
        {
            bytes /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes:0} {units[unit]}" : $"{bytes:0.#} {units[unit]}";
    }

    private static bool IsRegionalIndicator(int codePoint) => codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF;
}
