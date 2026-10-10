namespace Tunnelka.Desktop;

public static class EmojiKeys
{
    private const int VariationSelector15 = 0xFE0E;
    private const int VariationSelector16 = 0xFE0F;
    private const int FirstSkinTone = 0x1F3FB;
    private const int LastSkinTone = 0x1F3FF;

    public static IEnumerable<string> For(string symbol)
    {
        var codes = CodePoints(symbol);
        if (codes.Count == 0)
            return Array.Empty<string>();

        var withoutTones = codes.Where(c => c < FirstSkinTone || c > LastSkinTone).ToList();
        var keys = new List<string> { Join(codes) };
        if (withoutTones.Count > 0)
            keys.Add(Join(withoutTones));
        keys.Add(Join(codes.Take(1)));
        return keys.Distinct();
    }

    public static string Normalize(string entryName) =>
        string.Join("-", entryName.Split('-').Where(part => part != "fe0f"));

    private static List<int> CodePoints(string symbol) =>
        symbol.EnumerateRunes()
            .Select(rune => rune.Value)
            .Where(value => value != VariationSelector15 && value != VariationSelector16)
            .ToList();

    private static string Join(IEnumerable<int> codes) =>
        string.Join("-", codes.Select(code => code.ToString("x")));
}
