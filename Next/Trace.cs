namespace Tunnelka.Next;

internal static class Trace
{
    public static void Write(string text)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tunnelka");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "next-trace.log"), $"{DateTime.Now:HH:mm:ss.fff} {text}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }
    }
}
