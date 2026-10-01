using System.Globalization;
using System.Text;
using VpnClient.Models;

namespace VpnClient.UI;

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
            if (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF)
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

    public static string CleanName(ProxyServer server)
    {
        var result = new StringBuilder();
        var name = server.Name;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsSurrogate(c) || c == '️' || c == '‍')
                continue;

            var category = char.GetUnicodeCategory(c);
            if (category is UnicodeCategory.OtherSymbol or UnicodeCategory.Format or UnicodeCategory.NonSpacingMark)
                continue;

            result.Append(char.IsWhiteSpace(c) ? ' ' : c);
        }

        var clean = string.Join(" ", result.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        return clean.Length > 0 ? clean : $"{server.Address}:{server.Port}";
    }

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
}
