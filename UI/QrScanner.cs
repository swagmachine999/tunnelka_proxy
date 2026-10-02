using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;

namespace VpnClient.UI;

public static class QrScanner
{
    public static string? FromScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return Read(bitmap);
    }

    public static string? FromFile(string path)
    {
        try
        {
            using var image = Image.FromFile(path);
            using var bitmap = new Bitmap(image);
            return Read(bitmap);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? Read(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var pixels = new byte[data.Stride * data.Height];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        bitmap.UnlockBits(data);

        var reader = new BarcodeReaderGeneric
        {
            Options = { PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }, TryHarder = true }
        };
        var source = new RGBLuminanceSource(pixels, data.Stride / 4, data.Height, RGBLuminanceSource.BitmapFormat.BGRA32);
        return reader.Decode(source)?.Text;
    }
}
