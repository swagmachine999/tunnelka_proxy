using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Tunnelka.Models;

namespace Tunnelka.Desktop;

public sealed class OverlayPage : UserControl
{
    private static readonly int[] Scales = { 75, 100, 125, 150, 200 };

    private readonly Session _session;
    private readonly OverlayOptions _options;
    private readonly ToggleSwitch _show;
    private readonly TextBlock _keysHint;
    private bool _syncing;

    public OverlayPage(Session session)
    {
        _session = session;
        _options = session.Data.Overlay;
        var overlay = session.Overlay;
        var panel = new StackPanel();

        _show = SettingsParts.Toggle(overlay.IsShown, on =>
        {
            if (!_syncing && on != overlay.IsShown)
                overlay.Toggle();
        });
        panel.Children.Add(Ui.Row(L.T("Показать оверлей"), L.T("Можно включить и здесь, без горячей клавиши"), _show, out _, out _));

        var hotkeyToggle = SettingsParts.Toggle(_options.HotkeyEnabled, on =>
        {
            _options.HotkeyEnabled = on;
            HotkeyChanged();
        });
        panel.Children.Add(Ui.Row(L.T("Горячая клавиша"), L.T("Показывать и скрывать оверлей поверх всех окон"), hotkeyToggle, out _, out _));

        var keys = new SettingsHotkeyBox(_options.Hotkey);
        keys.KeysChanged += (_, _) =>
        {
            _options.Hotkey = keys.Keys;
            HotkeyChanged();
        };
        panel.Children.Add(Ui.Row(L.T("Сочетание клавиш"), "", keys, out _, out _keysHint));

        var corner = new SettingsCornerPicker(_options.Corner);
        corner.CornerChanged += (_, _) => OptionsChanged(() => _options.Corner = corner.Corner);
        panel.Children.Add(Ui.Row(L.T("Положение"), L.T("Угол экрана"), corner, out _, out _));

        var scale = new SettingsStepper(Scales, v => $"{v}%", _options.Scale, 150);
        scale.ValueChanged += (_, _) => OptionsChanged(() => _options.Scale = scale.Value);
        panel.Children.Add(Ui.Row(L.T("Размер"), L.T("Масштаб надписей оверлея"), scale, out _, out _));

        panel.Children.Add(SettingsParts.Caption(L.T("ЧТО ПОКАЗЫВАТЬ")));

        var ping = SettingsParts.Toggle(_options.ShowPing, on => OptionsChanged(() => _options.ShowPing = on));
        panel.Children.Add(Ui.Row(L.T("Пинг"), L.T("Задержка через VPN, каждую секунду"), ping, out _, out _));

        var speed = SettingsParts.Toggle(_options.ShowSpeed, on => OptionsChanged(() => _options.ShowSpeed = on));
        panel.Children.Add(Ui.Row(L.T("Скорость"), L.T("Загрузка и отдача, каждую секунду"), speed, out _, out _));

        var loss = SettingsParts.Toggle(_options.ShowLoss, on => OptionsChanged(() => _options.ShowLoss = on));
        panel.Children.Add(Ui.Row(L.T("Потеря пакетов"), L.T("Сколько проверок до сервера не дошло за последние 10 секунд"), loss, out _, out _));

        ShowHotkeyState(overlay.HotkeyFailed);
        Content = panel;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _session.Overlay.VisibilityChanged += OnVisibilityChanged;
        Sync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _session.Overlay.VisibilityChanged -= OnVisibilityChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnVisibilityChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(Sync);

    private void Sync()
    {
        _syncing = true;
        _show.IsChecked = _session.Overlay.IsShown;
        _syncing = false;
    }

    private void HotkeyChanged()
    {
        _session.Save();
        _session.Overlay.ApplyHotkey();
        ShowHotkeyState(_session.Overlay.HotkeyFailed);
    }

    private void OptionsChanged(Action apply)
    {
        apply();
        _session.Save();
        _session.Overlay.ApplyOptions();
    }

    private void ShowHotkeyState(bool failed) =>
        _keysHint.Text = failed
            ? L.T("Это сочетание занято другой программой, выберите другое")
            : L.T("Нажмите на поле, затем нужные клавиши");
}
