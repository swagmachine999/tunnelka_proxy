using System.Text;

namespace Tunnelka.Parsing;

public static class Base64Helper
{
    public static string Decode(string text)
    {
        var s = text.Trim()
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace('-', '+')
            .Replace('_', '/')
            .TrimEnd('=');

        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
