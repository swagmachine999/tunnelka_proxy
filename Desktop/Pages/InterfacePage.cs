using Avalonia;
using Avalonia.Controls;
using Tunnelka.Storage;
using Tunnelka.UI;

namespace Tunnelka.Desktop;

public sealed class InterfacePage : UserControl
{
    private static readonly int[] SpeedIntervals = { 1, 2, 3, 5, 10, 15, 30, 60, 300, 600, 1800, 3600 };

    private readonly Session _session;
    private readonly SettingsStepper _scale;

    public InterfacePage(Session session)
    {
        _session = session;
        var data = session.Data;
        var panel = new StackPanel();

        var language = new SettingsSegmented(190, "Русский", "English");
        language.Select(data.Language == "en" ? 1 : 0);
        language.SelectedIndexChanged += (_, _) => session.SetLanguage(language.SelectedIndex == 1 ? "en" : "ru");
        panel.Children.Add(Ui.Row(L.T("Язык"), "Русский · English", language, out _, out _));

        _scale = new SettingsStepper(UiScaleMigration.Steps, v => $"{v}%", data.UiScale, 150);
        _scale.ValueChanged += (_, _) => session.SetScale(_scale.Value);
        panel.Children.Add(Ui.Row(L.T("Масштаб интерфейса"), L.T("Ctrl + колесо мыши, Ctrl и +/−, Ctrl+0"), _scale, out _, out _));

        var dark = SettingsParts.Toggle(data.DarkTheme, on => session.SetDark(on));
        panel.Children.Add(Ui.Row(L.T("Тёмная тема"), L.T("Мягкие тёмные цвета"), dark, out _, out _));

        var speed = new SettingsStepper(SpeedIntervals, ServerText.Duration, data.SpeedInterval, 150);
        speed.ValueChanged += (_, _) =>
        {
            data.SpeedInterval = speed.Value;
            session.RestartSpeedAveraging();
            session.Save();
        };
        panel.Children.Add(Ui.Row(L.T("Отображение скорости"), L.T("Как часто обновлять скорость в интерфейсе"), speed, out _, out _));

        Content = panel;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _session.ScaleChanged += OnScaleChanged;
        OnScaleChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _session.ScaleChanged -= OnScaleChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnScaleChanged() => _scale.Select(_session.Data.UiScale);
}
