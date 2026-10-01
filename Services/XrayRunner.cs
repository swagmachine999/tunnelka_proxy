using System.Diagnostics;
using System.Text;

namespace VpnClient.Services;

public sealed class XrayRunner : IDisposable
{
    private Process? _process;

    public event Action<string>? Output;
    public event Action? Exited;

    public static string CoreDir => Path.Combine(AppContext.BaseDirectory, "core");
    public static string XrayPath => Path.Combine(CoreDir, "xray.exe");

    public bool IsRunning => _process is { HasExited: false };

    public void Start(string configJson)
    {
        Stop();

        if (!File.Exists(XrayPath))
            throw new FileNotFoundException("Не найден xray.exe", XrayPath);

        var configPath = Path.Combine(CoreDir, "config.json");
        File.WriteAllText(configPath, configJson);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(XrayPath, $"run -c \"{configPath}\"")
            {
                WorkingDirectory = CoreDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) => { if (e.Data != null) Output?.Invoke(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Output?.Invoke(e.Data); };
        process.Exited += (_, _) => { if (ReferenceEquals(process, _process)) Exited?.Invoke(); };

        _process = process;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public static void KillOrphans()
    {
        foreach (var process in Process.GetProcessesByName("xray"))
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path != null && string.Equals(Path.GetFullPath(path), Path.GetFullPath(XrayPath), StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill(true);
                    process.WaitForExit(3000);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

        public void Stop()
    {
        var process = _process;
        if (process == null)
            return;

        _process = null;
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                process.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException)
        {
        }

        process.Dispose();
    }

    public void Dispose() => Stop();
}
