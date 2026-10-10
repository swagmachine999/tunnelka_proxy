using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Tunnelka.UI;
using ZXing;

namespace Tunnelka.Next;

public static class QrTools
{
    public static async Task<string?> ScanAsync(Window? owner)
    {
        var excluded = Win32.ExcludeFromCapture(owner, true);
        if (!excluded && owner != null)
            owner.Opacity = 0;

        await Task.Delay(excluded ? 100 : 250);
        string? text;
        try
        {
            text = await Task.Run(FromScreen);
        }
        finally
        {
            if (excluded)
            {
                Win32.ExcludeFromCapture(owner, false);
            }
            else if (owner != null)
            {
                owner.Opacity = 1;
                owner.Activate();
            }
        }

        if (text != null || owner == null)
            return text;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = L.T("QR-код на экране не найден. Выбери картинку с QR-кодом"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(L.T("Картинки|*.png;*.jpg;*.jpeg;*.bmp;*.gif").Split('|')[0])
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
                }
            }
        });
        if (files.Count == 0)
            return null;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            memory.Position = 0;
            return await Task.Run(() => FromStream(memory));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Bitmap? Render(string text, int size)
    {
        bool[,] modules;
        try
        {
            modules = QrCode.Encode(text);
        }
        catch (Exception)
        {
            return null;
        }

        var count = modules.GetLength(0);
        size = Math.Max(size, count + 8);
        var cell = Math.Max(1, size / (count + 8));
        var offset = (size - cell * count) / 2;
        var pixels = new byte[size * size * 4];
        Array.Fill(pixels, (byte)255);
        for (var y = 0; y < count; y++)
        {
            for (var x = 0; x < count; x++)
            {
                if (!modules[y, x])
                    continue;

                for (var dy = 0; dy < cell; dy++)
                {
                    var row = ((offset + y * cell + dy) * size + offset + x * cell) * 4;
                    for (var dx = 0; dx < cell; dx++)
                    {
                        var index = row + dx * 4;
                        pixels[index] = 0;
                        pixels[index + 1] = 0;
                        pixels[index + 2] = 0;
                    }
                }
            }
        }

        var bitmap = new WriteableBitmap(new PixelSize(size, size), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using (var buffer = bitmap.Lock())
        {
            for (var y = 0; y < size; y++)
                Marshal.Copy(pixels, y * size * 4, buffer.Address + y * buffer.RowBytes, size * 4);
        }

        return bitmap;
    }

    private static string? FromScreen()
    {
        var left = GetSystemMetrics(76);
        var top = GetSystemMetrics(77);
        var width = GetSystemMetrics(78);
        var height = GetSystemMetrics(79);
        if (width <= 0 || height <= 0)
            return null;

        using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
            g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
        return Read(bitmap);
    }

    private static string? FromStream(Stream stream)
    {
        using var image = System.Drawing.Image.FromStream(stream);
        using var bitmap = new System.Drawing.Bitmap(image);
        return Read(bitmap);
    }

    private static string? Read(System.Drawing.Bitmap bitmap)
    {
        var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
