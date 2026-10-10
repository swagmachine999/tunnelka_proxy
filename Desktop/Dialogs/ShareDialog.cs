using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Templates;

namespace Tunnelka.Desktop;

public static class ShareDialog
{
    private const int QrSize = 280;

    public static Task Show(Window? owner, string title, IReadOnlyList<(string Title, string Url)> keys)
    {
        if (keys.Count == 0)
            return Task.CompletedTask;

        var dialog = new Dialog(title, keys);
        return dialog.Open(owner);
    }

    private sealed class Dialog : DialogBase
    {
        private readonly IReadOnlyList<(string Title, string Url)> _keys;
        private readonly Image _image = new() { Width = QrSize, Height = QrSize, Stretch = Stretch.Uniform };
        private readonly TextBox _box = new() { IsReadOnly = true, FontSize = 14.3, Margin = new Thickness(0, 16, 0, 0) };
        private readonly Button _copy;
        private string _link;

        public Dialog(string title, IReadOnlyList<(string Title, string Url)> keys) : base(title, 460)
        {
            _keys = keys;
            _link = keys[0].Url;
            RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.None);

            var panel = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };

            if (keys.Count > 1)
            {
                var list = new ListBox
                {
                    ItemsSource = keys.Select(k => k.Title).ToList(),
                    SelectedIndex = 0,
                    Background = Brushes.Transparent,
                    Height = Math.Min(keys.Count, 4) * 50,
                    Margin = new Thickness(0, 0, 0, 10),
                    ItemTemplate = new FuncDataTemplate<string>((name, _) => new TextBlock
                    {
                        Text = name,
                        FontSize = 15.4,
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(14, 12),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    })
                };
                list.SelectionChanged += (_, _) =>
                {
                    if (list.SelectedIndex >= 0)
                        Choose(list.SelectedIndex);
                };
                panel.Children.Add(list);
            }

            var frame = new Border
            {
                Width = QrSize,
                Height = QrSize,
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = _image
            };
            panel.Children.Add(frame);
            panel.Children.Add(_box);

            var hint = Body(L.T("Отсканируй камерой на новом устройстве"));
            hint.FontSize = 14.3;
            hint.Margin = new Thickness(0, 8, 0, 0);
            panel.Children.Add(hint);

            _copy = Primary(L.T("Копировать"), 130);
            _copy.Click += async (_, _) => await Copy();
            var close = Secondary(L.T("Закрыть"));
            close.Click += (_, _) => Close();
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0),
                Children = { _copy, close }
            };
            panel.Children.Add(buttons);

            Content = panel;
            Choose(0);
        }

        public Task Open(Window? owner) => Present(owner);

        private void Choose(int index)
        {
            _link = _keys[index].Url;
            _box.Text = _link;
            _image.Source = QrTools.Render(_link, QrSize);
            _copy.Content = L.T("Копировать");
        }

        private async Task Copy()
        {
            if (Clipboard == null)
                return;

            await Clipboard.SetTextAsync(_link);
            _copy.Content = L.T("Скопировано");
        }
    }
}
