namespace Tunnelka.Services;

public static class CrashLog
{
    public static string FilePath => Path.Combine(Storage.AppStorage.Folder, "crash.log");

    public static void Write(Exception? exception)
    {
        try
        {
            File.AppendAllText(FilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }
    }
}
