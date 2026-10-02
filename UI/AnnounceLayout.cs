using System.Text.RegularExpressions;

namespace Tunnelka.UI;

public static class AnnounceLayout
{
    public const float SymbolSize = 16;

    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.Compiled);

    public static List<List<AnnounceToken>> Build(string announce, float maxWidth) => Wrap(Tokenize(announce), maxWidth);

    private static List<AnnounceToken> Tokenize(string announce)
    {
        var tokens = new List<AnnounceToken>();
        if (announce.Length == 0)
            return tokens;

        var paragraphs = announce.Replace("\r", "").Split('\n');
        for (var p = 0; p < paragraphs.Length; p++)
        {
            if (p > 0)
                tokens.Add(new AnnounceToken("", false, null, 0, true));

            var line = paragraphs[p];
            var position = 0;
            foreach (Match match in UrlPattern.Matches(line))
            {
                AddText(tokens, line.Substring(position, match.Index - position));
                var url = match.Value.TrimEnd('.', ',', ')', '!');
                tokens.Add(new AnnounceToken(url, false, url, Theme.Measure(url, Theme.CaptionBold).Width));
                position = match.Index + match.Value.Length;
            }
            AddText(tokens, line.Substring(position));
        }

        return tokens;
    }

    private static void AddText(List<AnnounceToken> tokens, string text)
    {
        if (text.Trim().Length == 0)
            return;

        foreach (var part in ServerText.Parts(text, null))
        {
            if (part.IsSymbol)
            {
                tokens.Add(new AnnounceToken(part.Text, true, null, SymbolSize));
                continue;
            }

            foreach (var word in part.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                tokens.Add(new AnnounceToken(word, false, null, Theme.Measure(word, Theme.Caption).Width));
        }
    }

    private static List<List<AnnounceToken>> Wrap(List<AnnounceToken> tokens, float maxWidth)
    {
        var lines = new List<List<AnnounceToken>>();
        if (tokens.Count == 0)
            return lines;

        var space = Theme.Measure(" ", Theme.Caption).Width + 1;
        var current = new List<AnnounceToken>();
        var width = 0f;

        foreach (var token in tokens)
        {
            if (token.LineBreak)
            {
                lines.Add(current);
                current = new List<AnnounceToken>();
                width = 0;
                continue;
            }

            var needed = current.Count == 0 ? token.Width : width + space + token.Width;
            if (current.Count > 0 && needed > maxWidth)
            {
                lines.Add(current);
                current = new List<AnnounceToken>();
                needed = token.Width;
            }

            current.Add(token);
            width = needed;
        }

        lines.Add(current);
        return lines;
    }
}
