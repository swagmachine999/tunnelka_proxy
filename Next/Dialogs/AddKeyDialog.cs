using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Next;

public static class AddKeyDialog
{
    public static async Task<string?> Ask(Window? owner)
    {
        var dialog = new Dialog();
        await dialog.Open(owner);
        return dialog.Result;
    }

    private sealed class Dialog : DialogBase
    {
        private readonly TextBlock _label;
        private readonly TextBox _box;

        public Dialog() : base(L.T("Добавить ключ"), 540)
        {
            _label = new TextBlock
            {
                Text = L.T("Ключ сервера (vless, vmess, trojan, ss, hysteria2, tuic) или ключ подписки"),
                FontSize = 15.4,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            _box = new TextBox
            {
                Watermark = L.T("vless://...  или  https://..."),
                FontSize = 15.4,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 9),
                Margin = new Thickness(0, 10, 0, 0)
            };
            _box.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    Submit();
                }
            };

            var paste = Secondary(L.T("Вставить из буфера"), 170);
            paste.Click += async (_, _) => await Paste();
            var add = Primary(L.T("Добавить"));
            add.Click += (_, _) => Submit();
            var cancel = Secondary(L.T("Отмена"));
            cancel.Click += (_, _) => Close();

            var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), Margin = new Thickness(0, 16, 0, 0) };
            buttons.Children.Add(paste);
            Grid.SetColumn(add, 2);
            buttons.Children.Add(add);
            cancel.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(cancel, 3);
            buttons.Children.Add(cancel);

            Content = new StackPanel
            {
                Margin = new Thickness(20, 18, 20, 20),
                Children = { _label, _box, buttons }
            };
            Opened += (_, _) => _box.Focus();
        }

        public string? Result { get; private set; }

        public Task Open(Window? owner) => Present(owner);

        private void Submit()
        {
            var text = _box.Text?.Trim() ?? "";
            if (text.Length == 0)
                return;

            Result = text;
            Close();
        }

        private async Task Paste()
        {
            var text = await (Clipboard?.GetTextAsync() ?? Task.FromResult<string?>(null));
            text = text?.Trim() ?? "";
            if (text.Length == 0)
            {
                _label.Text = L.T("Буфер обмена пуст");
                return;
            }

            Result = text;
            Close();
        }
    }
}
