using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Tunnelka.Services;

public sealed class XrayRunner : IDisposable
{
    private static readonly Regex Colors = new(@"\x1B\[[0-9;]*m", RegexOptions.Compiled);

    private readonly string _exePath;
    private readonly string _configName;
    private Process? _process;

    public XrayRunner()
        : this(XrayPath, "config.json")
    {
    }

    public XrayRunner(string exePath, string configName)
    {
        _exePath = exePath;
        _configName = configName;
    }

    public event Action<string>? Output;
    public event Action? Exited;

    public static string CoreDir => Path.Combine(AppContext.BaseDirectory, "core");
    public static string ConfigDir => Storage.AppStorage.Folder;
    public static string XrayPath => Path.Combine(CoreDir, "xray.exe");
    public static string SingBoxPath => Path.Combine(CoreDir, "sing-box.exe");

    public bool IsRunning => _process is { HasExited: false };

    public int? ExitCode => _process is { HasExited: true } process ? process.ExitCode : null;

    public bool WaitForExit(int milliseconds) => _process == null || _process.WaitForExit(milliseconds);

    public void Start(string configJson)
    {
        Stop();

        if (!File.Exists(_exePath))
            throw new FileNotFoundException(L.F("Не найден {0}", Path.GetFileName(_exePath)), _exePath);

        Directory.CreateDirectory(ConfigDir);
        var configPath = ConfigPath;
        File.WriteAllText(configPath, configJson);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(_exePath, $"run -c \"{configPath}\"")
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

        process.OutputDataReceived += (_, e) => { if (e.Data != null) Output?.Invoke(Colors.Replace(e.Data, "")); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Output?.Invoke(Colors.Replace(e.Data, "")); };
        process.Exited += (_, _) => { if (ReferenceEquals(process, _process)) Exited?.Invoke(); };

        _process = process;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private string ConfigPath => Path.Combine(ConfigDir, _configName);

    public static void DeleteConfigs()
    {
        try
        {
            if (!Directory.Exists(ConfigDir))
                return;

            foreach (var pattern in new[] { "config.json", "tun.json", "relay*.json", "ping-*.json" })
            {
                foreach (var file in Directory.GetFiles(ConfigDir, pattern))
                    File.Delete(file);
            }
        }
        catch (Exception)
        {
        }
    }

    public bool WaitForPort(int port, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (!IsRunning)
                return false;

            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                client.Connect(System.Net.IPAddress.Loopback, port);
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                Thread.Sleep(100);
            }
        }

        return false;
    }

    public static void KillOrphans(string exePath)
    {
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath)))
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path != null && string.Equals(Path.GetFullPath(path), Path.GetFullPath(exePath), StringComparison.OrdinalIgnoreCase))
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
        DeleteConfig();
    }

    private void DeleteConfig()
    {
        try
        {
            File.Delete(ConfigPath);
        }
        catch (Exception)
        {
        }
    }

    public void Dispose() => Stop();
}
