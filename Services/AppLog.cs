namespace Tunnelka.Services;

public sealed class AppLog
{
    public event Action<string>? Written;

    public void Write(string text) => Written?.Invoke(text);
}
