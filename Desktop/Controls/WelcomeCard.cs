using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed class WelcomeCard : Border
{
    public WelcomeCard()
    {
        Margin = new Thickness(0, 4, 0, 6);
        CornerRadius = new CornerRadius(16);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(20, 18, 20, 20);
        Background = ServerRes.CardGradient(this);
        ServerRes.Bind(this, BorderBrushProperty, "BorderBrush2");
        Build();
    }

    public event Action? PasteClicked;

    public event Action? ManualClicked;

    public void Localize() => Build();

    private static TextBlock Text(string text, double size, bool bold, string brushKey)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = size * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
            TextWrapping = TextWrapping.Wrap
        };
        ServerRes.Bind(block, TextBlock.ForegroundProperty, brushKey);
        return block;
    }

    private void Build()
    {
        var steps = new[]
        {
            (L.T("Получите ключ"), L.T("Ключ выдаёт ваш VPN-сервис: обычно его присылает Telegram-бот или он есть в личном кабинете на сайте. Это ссылка вида https://… или vless://…")),
            (L.T("Добавьте его сюда"), L.T("Скопируйте ключ и нажмите «Вставить ключ». Если ключ показан QR-кодом, нажмите «Сканировать QR» выше.")),
            (L.T("Подключитесь"), L.T("Появится список серверов. Выберите любой и нажмите большую круглую кнопку справа."))
        };

        var root = new StackPanel();
        var title = Text(L.T("Как начать"), 15, true, "TextBrush");
        title.Height = 26;
        root.Children.Add(title);
        var subtitle = Text(L.T("Три шага — и VPN работает"), 13, false, "TextMutedBrush");
        subtitle.Margin = new Thickness(0, 2, 0, 26);
        root.Children.Add(subtitle);

        for (var i = 0; i < steps.Length; i++)
        {
            var badge = new Grid { Width = 28, Height = 28, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
            var circle = new Ellipse { Width = 28, Height = 28 };
            ServerRes.Bind(circle, Shape.FillProperty, "AccentBrush");
            badge.Children.Add(circle);
            badge.Children.Add(new TextBlock
            {
                Text = (i + 1).ToString(),
                FontSize = 14 * 1.1,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var heading = Text(steps[i].Item1, 14, true, "TextBrush");
            heading.Margin = new Thickness(0, 2, 0, 2);
            var body = Text(steps[i].Item2, 14, false, "TextMutedBrush");
            var texts = new StackPanel { Margin = new Thickness(44, 0, 0, 0) };
            texts.Children.Add(heading);
            texts.Children.Add(body);

            var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row.Children.Add(badge);
            row.Children.Add(texts);
            root.Children.Add(row);
        }

        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,10,*"),
            Margin = new Thickness(0, 6, 0, 0)
        };
        var paste = MakeButton(L.T("Вставить ключ"), true);
        paste.Click += (_, _) => PasteClicked?.Invoke();
        var manual = MakeButton(L.T("Ввести вручную"), false);
        manual.Click += (_, _) => ManualClicked?.Invoke();
        Grid.SetColumn(paste, 0);
        Grid.SetColumn(manual, 2);
        buttons.Children.Add(paste);
        buttons.Children.Add(manual);
        root.Children.Add(buttons);

        Child = root;
    }

    private static Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Height = 40,
            Padding = new Thickness(12, 0),
            FontSize = 14 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        button.Classes.Add("soft");
        if (primary)
        {
            button.Classes.Add("accent");
            return button;
        }

        button.BorderThickness = new Thickness(1.4);
        ServerRes.Bind(button, TemplatedControl.BorderBrushProperty, "AccentBrush");
        return button;
    }
}
