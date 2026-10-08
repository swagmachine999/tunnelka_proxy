namespace Tunnelka.Models;

public class OverlayOptions
{
    public const int DefaultHotkey = 0x60050;

    public bool HotkeyEnabled { get; set; } = true;
    public int Hotkey { get; set; } = DefaultHotkey;
    public OverlayCorner Corner { get; set; } = OverlayCorner.TopRight;
    public int Scale { get; set; } = 100;
    public bool ShowSpeed { get; set; } = true;
    public bool ShowPing { get; set; } = true;
    public bool ShowLoss { get; set; } = false;
}

public enum OverlayCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
