using Tunnelka.UI.Controls;

namespace Tunnelka.UI.Pages;

public class InterfacePage : Panel
{
    private static readonly int[] Scales = { 60, 70, 80, 90, 100, 110, 120, 130 };

    public InterfacePage(bool dark, int uiScale, string language, Action onBack)
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Theme.Bind(this, () => Theme.Surface);

        DarkToggle.Checked = dark;
        ScaleSelector = new OptionStepper(Scales, v => $"{v}%", uiScale);
        LanguageSelector.Size = new Size(Theme.Px(190), Theme.Px(34));
        LanguageSelector.SelectedIndex = language == "en" ? 1 : 0;

        Controls.Add(new SettingRow(L.T("Язык"), "Русский · English", LanguageSelector));
        Controls.Add(new SettingRow(L.T("Масштаб интерфейса"), L.T("Ctrl + колесо мыши, Ctrl и +/−, Ctrl+0"), ScaleSelector));
        Controls.Add(new SettingRow(L.T("Тёмная тема"), L.T("Мягкие тёмные цвета"), DarkToggle));
        Controls.Add(Theme.Bind(new Panel { Dock = DockStyle.Top, Height = Theme.Px(10) }, () => Theme.Surface));
        Controls.Add(PageParts.Header(L.T("Интерфейс"), onBack));
    }

    public ToggleSwitch DarkToggle { get; } = new();
    public OptionStepper ScaleSelector { get; }
    public Segmented LanguageSelector { get; } = new("Русский", "English");

    public string Language => LanguageSelector.SelectedIndex == 1 ? "en" : "ru";
}
