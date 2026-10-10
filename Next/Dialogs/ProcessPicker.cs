using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Tunnelka.Next;

public sealed class ProcessPicker : Window
{
    private readonly List<AppItem> _items;
    private readonly TextBox _search;
    private readonly ListBox _list;

    private ProcessPicker(List<AppItem> items, int scalePercent)
    {
        _items = items;
        var factor = scalePercent / 100.0;
        Title = L.T("Выбор приложения");
        Width = 600 * factor;
        Height = 560 * factor;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SettingsTheme.Paint(this, BackgroundProperty, "SurfaceBrush");

        var heading = new TextBlock { Text = L.T("Выберите приложение"), FontSize = 24, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 12) };
        _search = new TextBox { Watermark = L.T("Поиск по имени или пути..."), CornerRadius = new CornerRadius(12) };
        var section = new TextBlock { Text = L.T("Приложения"), FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 14, 0, 6) };

        _list = new ListBox
        {
            Background = Brushes.Transparent,
            ItemTemplate = new FuncDataTemplate<AppItem>((item, _) => BuildRow(item))
        };

        var choose = SettingsParts.Pill(L.T("Выбрать"), true);
        var cancel = SettingsParts.Pill(L.T("Отмена"), false);
        choose.Click += (_, _) => Accept();
        cancel.Click += (_, _) => Close();
        choose.Margin = new Thickness(10, 0, 0, 0);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0),
            Children = { cancel, choose }
        };

        var dock = new DockPanel { Margin = new Thickness(20, 16, 20, 16) };
        DockPanel.SetDock(heading, Dock.Top);
        DockPanel.SetDock(_search, Dock.Top);
        DockPanel.SetDock(section, Dock.Top);
        DockPanel.SetDock(buttons, Dock.Bottom);
        dock.Children.Add(heading);
        dock.Children.Add(_search);
        dock.Children.Add(section);
        dock.Children.Add(buttons);
        dock.Children.Add(_list);

        Content = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(factor, factor),
            Child = dock
        };

        _search.TextChanged += (_, _) => Fill();
        _list.DoubleTapped += (_, _) => Accept();
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                Accept();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };
        Opened += (_, _) => _search.Focus();
        Fill();
    }

    public static async Task<string?> Pick(Window owner, int scalePercent = 100)
    {
        var items = await Task.Run(LoadProcesses);
        var dialog = new ProcessPicker(items, scalePercent);
        return await dialog.ShowDialog<string?>(owner);
    }

    private static Control BuildRow(AppItem? item)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(12, 9) };
        if (item == null)
            return grid;

        Control icon;
        if (item.Icon != null)
        {
            icon = new Image { Source = item.Icon, Width = 28, Height = 28 };
        }
        else
        {
            var badge = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(8) };
            SettingsTheme.Paint(badge, Border.BackgroundProperty, "CardSelectedBrush");
            icon = badge;
        }

        icon.Margin = new Thickness(0, 0, 14, 0);
        icon.VerticalAlignment = VerticalAlignment.Center;

        var name = new TextBlock { Text = item.Name, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        var path = new TextBlock { Text = item.Path, Classes = { "muted" }, TextTrimming = TextTrimming.CharacterEllipsis };
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { name, path } };
        Grid.SetColumn(texts, 1);
        grid.Children.Add(icon);
        grid.Children.Add(texts);
        return grid;
    }

    private void Fill()
    {
        var query = (_search.Text ?? "").Trim();
        _list.ItemsSource = _items
            .Where(i => query.Length == 0
                || i.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || i.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void Accept()
    {
        if (_list.SelectedItem is AppItem item)
            Close(item.Path.Length > 0 ? item.Path : item.Name);
    }

    private static List<AppItem> LoadProcesses()
    {
        var items = new Dictionary<string, AppItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id <= 4)
                    continue;

                var path = ImagePath(process.Id) ?? "";
                var name = path.Length > 0 ? System.IO.Path.GetFileName(path) : process.ProcessName + ".exe";
                var key = path.Length > 0 ? path : name;
                if (items.ContainsKey(key))
                    continue;

                items[key] = new AppItem(name, path, SettingsIcons.Load(path));
            }
            catch (Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return items.Values
            .OrderBy(i => i.Path.Length == 0)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ImagePath(int processId)
    {
        try
        {
            var handle = OpenProcess(0x1000, false, processId);
            if (handle == IntPtr.Zero)
                return null;

            try
            {
                var buffer = new StringBuilder(1024);
                var size = buffer.Capacity;
                return QueryFullProcessImageName(handle, 0, buffer, ref size) ? buffer.ToString() : null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int access, bool inherit, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    private sealed class AppItem
    {
        public AppItem(string name, string path, Bitmap? icon)
        {
            Name = name;
            Path = path;
            Icon = icon;
        }

        public string Name { get; }
        public string Path { get; }
        public Bitmap? Icon { get; }
    }
}
