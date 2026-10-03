namespace Tunnelka.Services;

public sealed class AppLog
{
    private const long MaxBytes = 1024 * 1024;
    private static readonly object FileLock = new();

    public event Action<string>? Written;

    public static string FilePath => Path.Combine(Storage.AppStorage.Folder, "tunnelka.log");

    public void Write(string text)
    {
        Append(text);
        Written?.Invoke(text);
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
