using Avalonia;
using Avalonia.Controls;
using Tunnelka.Storage;

namespace Tunnelka.Desktop;

public sealed class InterfacePage : UserControl
{
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
