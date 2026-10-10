using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Tunnelka.UI;

public static class Fonts
{
    public const string Regular = "Segoe UI";
    public const string SemiBold = "Segoe UI Semibold";
    public const string Bold = "Segoe UI";

    private static readonly PrivateFontCollection Collection = new();

    static Fonts()
    {
        foreach (var name in EmbeddedAssets.Names("Fonts", ".ttf"))
        {
            var data = EmbeddedAssets.Read(name);
            if (data != null)
                Register(data);
        }
    }

    private static void Register(byte[] data)
    {
        var memory = Marshal.AllocCoTaskMem(data.Length);
        Marshal.Copy(data, 0, memory, data.Length);

        try
        {
            uint count = 0;
            AddFontMemResourceEx(memory, (uint)data.Length, IntPtr.Zero, ref count);
        }
        catch (Exception)
        {
        }

        Collection.AddMemoryFont(memory, data.Length);
    }

    public static Font Make(string family, float pixels, FontStyle style = FontStyle.Regular)
    {
        var loaded = Collection.Families.FirstOrDefault(f => f.Name == family);
        return loaded != null
            ? new Font(loaded, pixels, style, GraphicsUnit.Pixel)
            : new Font(family, pixels, style, GraphicsUnit.Pixel);
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr font, uint size, IntPtr reserved, ref uint count);
}
