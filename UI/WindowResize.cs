using System.Drawing;

namespace Tunnelka.UI;

public static class WindowResize
{
    public static Rectangle Scale(Rectangle bounds, float ratio, Rectangle work)
    {
        var width = Math.Min((int)Math.Round(bounds.Width * ratio), work.Width);
        var height = Math.Min((int)Math.Round(bounds.Height * ratio), work.Height);
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;
        x = Math.Max(work.Left, Math.Min(work.Right - width, x));
        y = Math.Max(work.Top, Math.Min(work.Bottom - height, y));
        return new Rectangle(x, y, width, height);
    }
}
