using VpnClient.Storage;

namespace VpnClient.Services;

public sealed class Settings
{
    private readonly AppLog _log;

    public Settings(AppLog log)
    {
        _log = log;
        Data = AppStorage.Load();
    }

    public AppData Data { get; }

    public void Save()
    {
        try
        {
            AppStorage.Save(Data);
        }
        catch (Exception ex)
        {
            _log.Write(L.F("Не удалось сохранить настройки: {0}", ex.Message));
        }
    }
}
