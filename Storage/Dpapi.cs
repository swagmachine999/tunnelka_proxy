using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Tunnelka.Storage;

public static class Dpapi
{
    private const int UiForbidden = 1;

    public static byte[] Protect(byte[] data) => OperatingSystem.IsWindows() ? Run(data, true) : data;

    public static byte[] Unprotect(byte[] data) => OperatingSystem.IsWindows() ? Run(data, false) : data;

    private static byte[] Run(byte[] data, bool protect)
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var input = new DataBlob { Size = data.Length, Data = handle.AddrOfPinnedObject() };
        var output = new DataBlob();
        try
        {
            var ok = protect
                ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref output);
            if (!ok)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, output.Size);
            return result;
        }
        finally
        {
            handle.Free();
            if (output.Data != IntPtr.Zero)
                LocalFree(output.Data);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr handle);
}
