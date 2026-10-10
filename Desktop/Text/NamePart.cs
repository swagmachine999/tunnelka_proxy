namespace Tunnelka.UI;

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
