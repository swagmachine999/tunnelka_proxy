namespace Tunnelka.UI;

public static class Fonts
{
    public const string Regular = "Segoe UI";
    public const string SemiBold = "Segoe UI Semibold";
    public const string Bold = "Segoe UI";

    public static Font Make(string family, float pixels, FontStyle style = FontStyle.Regular) =>
        new(family, pixels, style, GraphicsUnit.Pixel);
}
