using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace Tunnelka.Next;

public sealed class RoutingAddPanel : UserControl
{
    private readonly Func<int> _scale;
    private readonly TextBox _input;

    public RoutingAddPanel(Func<int> scale)
    {
        _scale = scale;

        var program = SettingsParts.Pill(L.T("+ Программа"), false);
        program.HorizontalAlignment = HorizontalAlignment.Stretch;
        program.Margin = new Thickness(0, 0, 6, 0);
        program.Click += async (_, _) => await PickProcess();

        var file = SettingsParts.Pill(L.T("+ Файл .exe"), false);
        file.HorizontalAlignment = HorizontalAlignment.Stretch;
        file.Margin = new Thickness(6, 0, 0, 0);
        file.Click += async (_, _) => await PickFile();
        Grid.SetColumn(file, 1);

        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        buttons.Children.Add(program);
        buttons.Children.Add(file);

        _input = new TextBox
        {
            Watermark = L.T("Сайт или IP, например sberbank.ru"),
            CornerRadius = new CornerRadius(12),
            MinHeight = 40,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                Submit();
        };

        var add = SettingsParts.Pill(L.T("Добавить"), true);
        add.Margin = new Thickness(12, 0, 0, 0);
        add.VerticalAlignment = VerticalAlignment.Stretch;
        add.Click += (_, _) => Submit();
        Grid.SetColumn(add, 1);

        var site = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        site.Children.Add(_input);
        site.Children.Add(add);

        Content = new StackPanel { Spacing = 12, Children = { buttons, site } };
    }

    public event Action<string>? ProcessChosen;

    public event Action<string>? SitesEntered;

    private void Submit()
    {
        var text = (_input.Text ?? "").Trim();
        _input.Text = "";
        SitesEntered?.Invoke(text);
    }

    private async Task PickProcess()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var name = await ProcessPicker.Pick(owner, _scale());
        if (name != null)
            ProcessChosen?.Invoke(name);
    }

    private async Task PickFile()
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;

        var filter = new FilePickerFileType(L.T("Программы (*.exe)|*.exe").Split('|')[0]) { Patterns = new[] { "*.exe" } };
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = L.T("Выбери программу"),
            AllowMultiple = false,
            FileTypeFilter = new[] { filter }
        });
        if (files.Count == 0)
            return;

        var path = files[0].Path.LocalPath;
        if (path.Length > 0)
            ProcessChosen?.Invoke(path);
    }
}
