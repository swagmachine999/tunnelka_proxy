namespace Tunnelka.Services.Privileged;

public sealed class ServiceLog
{
    private const long MaxBytes = 512 * 1024;

    private readonly string _path;
    private readonly object _gate = new();

    public ServiceLog(string path)
    {
        _path = path;
    }

    public void Write(string message)
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                if (File.Exists(_path) && new FileInfo(_path).Length > MaxBytes)
                    File.Move(_path, _path + ".old", true);
                File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch (Exception)
            {
            }
        }
    }
}
