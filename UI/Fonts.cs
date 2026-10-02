using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace VpnClient.UI;

public static class Fonts
{
    public const string Regular = "Onest";
    public const string SemiBold = "Onest SemiBold";
    public const string Bold = "Onest Bold";

    private static readonly PrivateFontCollection Collection = new();

    static Fonts()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        if (!Directory.Exists(folder))
            return;

        foreach (var file in Directory.GetFiles(folder, "*.ttf"))
        {
            try
            {
                AddFontResourceEx(file, PrivateFont, IntPtr.Zero);
            }
            catch (Exception)
            {
            }

            Collection.AddFontFile(file);
        }
    }

    public static Font Make(string family, float pixels, FontStyle style = FontStyle.Regular)
    {
        var loaded = Collection.Families.FirstOrDefault(f => f.Name == family);
        return loaded != null
            ? new Font(loaded, pixels, style, GraphicsUnit.Pixel)
            : new Font("Segoe UI", pixels, family == Regular ? style : style | FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private const uint PrivateFont = 0x10;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);
}
