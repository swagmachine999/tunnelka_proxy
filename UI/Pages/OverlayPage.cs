using Tunnelka.Models;
using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class OverlayPage : Panel
{
    private static readonly int[] Scales = { 75, 100, 125, 150, 200 };

    private readonly OverlayOptions _options;
    private readonly SettingRow _keysRow;

    public event EventHandler? OptionsChanged;
    public event EventHandler? HotkeyChanged;

    public OverlayPage(OverlayOptions options, Action onBack)
    {
        _options = options;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        var hotkeyToggle = new ToggleSwitch { Checked = options.HotkeyEnabled };
        hotkeyToggle.CheckedChanged += (_, _) =>
        {
            _options.HotkeyEnabled = hotkeyToggle.Checked;
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        };

        var keys = new HotkeyBox((Keys)options.Hotkey);
        keys.KeysChanged += (_, _) =>
        {
            _options.Hotkey = (int)keys.Keys;
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        };

        var corner = new CornerPicker(options.Corner);
        corner.CornerChanged += (_, _) => Change(() => _options.Corner = corner.Corner);

        var scale = new OptionStepper(Scales, v => $"{v}%", options.Scale);
        scale.ValueChanged += (_, _) => Change(() => _options.Scale = scale.Value);

        var ping = Toggle(options.ShowPing, on => _options.ShowPing = on);
        var speed = Toggle(options.ShowSpeed, on => _options.ShowSpeed = on);
        var loss = Toggle(options.ShowLoss, on => _options.ShowLoss = on);

        _keysRow = new SettingRow(L.T("Сочетание клавиш"), "", keys);

        Controls.Add(new SettingRow(L.T("Потеря пакетов"), L.T("Сколько проверок до сервера не дошло за последние 10 секунд"), loss));
        Controls.Add(new SettingRow(L.T("Скорость"), L.T("Загрузка и отдача, каждую секунду"), speed));
        Controls.Add(new SettingRow(L.T("Пинг"), L.T("Задержка через VPN, каждую секунду"), ping));
        Controls.Add(PageParts.Caption(L.T("ЧТО ПОКАЗЫВАТЬ"), 36));
        Controls.Add(new SettingRow(L.T("Размер"), L.T("Масштаб надписей оверлея"), scale));
        Controls.Add(new SettingRow(L.T("Положение"), L.T("Угол экрана"), corner));
        Controls.Add(_keysRow);
        Controls.Add(new SettingRow(L.T("Горячая клавиша"), L.T("Показывать и скрывать оверлей поверх всех окон"), hotkeyToggle));
        Controls.Add(new SettingRow(L.T("Показать оверлей"), L.T("Можно включить и здесь, без горячей клавиши"), ShowToggle));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Header(L.T("Оверлей"), onBack));
        ShowHotkeyState(false);
    }

    public ToggleSwitch ShowToggle { get; } = new();

    public void ShowHotkeyState(bool failed)
    {
        _keysRow.Subtitle = failed
            ? L.T("Это сочетание занято другой программой, выберите другое")
            : L.T("Нажмите на поле, затем нужные клавиши");
        _keysRow.Invalidate();
    }

    private ToggleSwitch Toggle(bool value, Action<bool> apply)
    {
        var toggle = new ToggleSwitch { Checked = value };
        toggle.CheckedChanged += (_, _) => Change(() => apply(toggle.Checked));
        return toggle;
    }

    private void Change(Action apply)
    {
        apply();
        OptionsChanged?.Invoke(this, EventArgs.Empty);
    }
}
