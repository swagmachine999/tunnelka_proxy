using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Tunnelka.Next;

internal static class RunningApps
{
    public static List<AppItem> Load()
    {
        var items = new Dictionary<string, AppItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id <= 4)
                    continue;

                var path = ImagePath(process.Id) ?? "";
                var name = path.Length > 0 ? System.IO.Path.GetFileName(path) : process.ProcessName + ".exe";
                var key = path.Length > 0 ? path : name;
                if (items.ContainsKey(key))
                    continue;

                items[key] = new AppItem(name, path, ExeIcons.Load(path));
            }
            catch (Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return items.Values
            .OrderBy(i => i.Path.Length == 0)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ImagePath(int processId)
    {
        try
        {
            var handle = OpenProcess(0x1000, false, processId);
            if (handle == IntPtr.Zero)
                return null;

            try
            {
                var buffer = new StringBuilder(1024);
                var size = buffer.Capacity;
                return QueryFullProcessImageName(handle, 0, buffer, ref size) ? buffer.ToString() : null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int access, bool inherit, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
