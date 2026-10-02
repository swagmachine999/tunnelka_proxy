namespace Tunnelka.UI;

public sealed class AnnounceToken
{
    public AnnounceToken(string text, bool symbol, string? url, float width, bool lineBreak = false)
    {
        Text = text;
        Symbol = symbol;
        Url = url;
        Width = width;
        LineBreak = lineBreak;
    }

    public string Text { get; }
    public bool Symbol { get; }
    public string? Url { get; }
    public float Width { get; }
    public bool LineBreak { get; }
}
