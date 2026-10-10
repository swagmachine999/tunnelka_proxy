using System.Text.RegularExpressions;

namespace Tunnelka.UI;

public static class AnnounceLayout
{
    public const float SymbolSize = 16;

    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.Compiled);
    private static readonly Regex ColorPattern = new(@"^#([0-9A-Fa-f]{6})", RegexOptions.Compiled);

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
            Color? color = null;
            foreach (Match match in UrlPattern.Matches(line))
            {
                AddText(tokens, line.Substring(position, match.Index - position), ref color);
                var url = match.Value.TrimEnd('.', ',', ')', '!');
                tokens.Add(new AnnounceToken(url, false, url, Theme.Measure(url, Theme.CaptionBold).Width));
                position = match.Index + match.Value.Length;
            }
            AddText(tokens, line.Substring(position), ref color);
        }

        return tokens;
    }

    private static void AddText(List<AnnounceToken> tokens, string text, ref Color? color)
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

            foreach (var raw in part.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var word = raw;
                var match = ColorPattern.Match(word);
                if (match.Success)
                {
                    color = ColorTranslator.FromHtml("#" + match.Groups[1].Value);
                    word = word.Substring(match.Length);
                    if (word.Length == 0)
                        continue;
                }

                var font = color == null ? Theme.Caption : Theme.CaptionBold;
                tokens.Add(new AnnounceToken(word, false, null, Theme.Measure(word, font).Width, color: color));
            }
        }
    }

    public static float SpaceWidth() => Math.Max(2, Theme.Measure(" ", Theme.Caption).Width - 1);

    private static List<List<AnnounceToken>> Wrap(List<AnnounceToken> tokens, float maxWidth)
    {
        var lines = new List<List<AnnounceToken>>();
        if (tokens.Count == 0)
            return lines;

        var space = SpaceWidth();
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
