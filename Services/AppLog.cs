namespace Tunnelka.Services;

public sealed class AppLog
{
    private const long MaxBytes = 1024 * 1024;
    private static readonly object FileLock = new();

    private const int RecentLimit = 300;

    private readonly Queue<string> _recent = new();
    private readonly object _recentLock = new();

    public event Action<string>? Written;

    public IReadOnlyList<string> Recent()
    {
        lock (_recentLock)
            return _recent.ToList();
    }

    public static string FilePath => Path.Combine(Storage.AppStorage.Folder, "tunnelka.log");

    public void Write(string text)
    {
        Append(text);
        Remember($"[{DateTime.Now:HH:mm:ss}] {text}");
        Written?.Invoke(text);
    }

    private void Remember(string line)
    {
        lock (_recentLock)
        {
            _recent.Enqueue(line);
            while (_recent.Count > RecentLimit)
                _recent.Dequeue();
        }
    }

    private static void Append(string text)
    {
        try
        {
            lock (FileLock)
            {
                Directory.CreateDirectory(Storage.AppStorage.Folder);
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > MaxBytes)
                {
                    File.Copy(FilePath, FilePath + ".old", true);
                    File.Delete(FilePath);
                }
                File.AppendAllText(FilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
        }
    }
}
