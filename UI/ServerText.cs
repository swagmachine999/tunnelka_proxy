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

            if (c == '\uFE0F' || c == '\uFE0E' || c == '\u200D' || (char.IsLowSurrogate(c)))
                continue;

            var codePoint = char.IsHighSurrogate(c) && i + 1 < name.Length ? char.ConvertToUtf32(c, name[i + 1]) : c;
            var isPair = codePoint > 0xFFFF;

            if (IsRegionalIndicator(codePoint))
            {
                i++;
                continue;
            }

            if (!isPair && char.GetUnicodeCategory(c) != UnicodeCategory.OtherSymbol)
            {
                text.Append(char.IsWhiteSpace(c) ? ' ' : c);
                continue;
            }

            var symbol = new StringBuilder(char.ConvertFromUtf32(codePoint));
            i += isPair ? 1 : 0;

            while (i + 1 < name.Length)
            {
                var next = name[i + 1];
                if (next == '\uFE0F' || next == '\u20E3')
                {
                    symbol.Append(next);
                    i++;
                }
                else if (next == '\u200D' && i + 2 < name.Length)
                {
                    var joined = char.IsHighSurrogate(name[i + 2]) && i + 3 < name.Length
                        ? char.ConvertFromUtf32(char.ConvertToUtf32(name[i + 2], name[i + 3]))
                        : name[i + 2].ToString();
                    symbol.Append(next).Append(joined);
                    i += 1 + joined.Length;
                }
                else if (char.IsHighSurrogate(next) && i + 2 < name.Length &&
                         char.ConvertToUtf32(next, name[i + 2]) is >= 0x1F3FB and <= 0x1F3FF)
                {
                    symbol.Append(next).Append(name[i + 2]);
                    i += 2;
                }
                else
                {
                    break;
                }
            }

            FlushText();
            parts.Add(new NamePart(symbol.ToString(), true));
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
