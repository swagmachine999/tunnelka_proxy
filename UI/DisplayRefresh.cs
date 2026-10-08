using System.Runtime.InteropServices;

namespace Tunnelka.UI;

public static class DisplayRefresh
{
    private const int CurrentSettings = -1;

    public static int Hertz(Control control)
    {
        try
        {
            var mode = new DeviceMode { Size = (short)Marshal.SizeOf<DeviceMode>() };
            var device = Screen.FromControl(control).DeviceName;
            return EnumDisplaySettings(device, CurrentSettings, ref mode) && mode.DisplayFrequency > 1
                ? mode.DisplayFrequency
                : Animation.FrameRate.FallbackHertz;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return Animation.FrameRate.FallbackHertz;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DeviceMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        public short SpecVersion;
        public short DriverVersion;
        public short Size;
        public short DriverExtra;
        public int Fields;
        public int PositionX;
        public int PositionY;
        public int DisplayOrientation;
        public int DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;
        public short LogPixels;
        public int BitsPerPel;
        public int PelsWidth;
        public int PelsHeight;
        public int DisplayFlags;
        public int DisplayFrequency;
        public int IcmMethod;
        public int IcmIntent;
        public int MediaType;
        public int DitherType;
        public int Reserved1;
        public int Reserved2;
        public int PanningWidth;
        public int PanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DeviceMode mode);
}
