namespace Tunnelka;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainContext(args.Contains("--connect")));
    }
}
