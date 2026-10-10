using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Tunnelka.Next;

public sealed class Toast : Window
{
    private const double ToastWidth = 360;
    private const int EdgeGap = 16;

    private static Toast? _current;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(10) };

    private Toast(string text)
    {
        SystemDecorations = SystemDecorations.None;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        CanResize = false;
        SizeToContent = SizeToContent.Height;
        Width = ToastWidth * DialogScale.Factor;
        Title = "Tunnelka";
        WindowStartupLocation = WindowStartupLocation.Manual;
        this.Paint(BackgroundProperty, "CardBrush");

        var stripe = new Border { Width = 5 };
        stripe.Paint(Border.BackgroundProperty, "PingMidBrush");

        var heading = new TextBlock { Text = "Tunnelka", FontSize = 15.4, FontWeight = FontWeight.SemiBold };
        var body = new TextBlock { Text = text, FontSize = 14.3, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
        body.Paint(TextBlock.ForegroundProperty, "TextMutedBrush");
        var texts = new StackPanel { Margin = new Thickness(14, 12, 14, 14), Children = { heading, body } };
        Grid.SetColumn(texts, 1);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(stripe);
        grid.Children.Add(texts);

        var frame = new Border { BorderThickness = new Thickness(1), Child = grid };
        frame.Paint(Border.BorderBrushProperty, "BorderBrush2");
        Content = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(DialogScale.Factor, DialogScale.Factor),
            Child = frame
        };

        PointerPressed += (_, _) => Close();
        Opened += (_, _) =>
        {
            Win32.NoActivate(this);
            Place();
        };
        PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
                Place();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            if (_current == this)
                _current = null;
        };
        _timer.Tick += (_, _) => Close();
    }

    public static void Popup(string text)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Popup(text));
            return;
        }

        _current?.Close();
        var toast = new Toast(text);
        _current = toast;
        toast.Show();
        toast._timer.Start();
    }

    private void Place()
    {
        var screen = Screens.Primary;
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1040);
        var scale = screen?.Scaling ?? 1.0;
        var margin = (int)Math.Round(EdgeGap * scale);
        var width = (int)Math.Ceiling(Width * scale);
        var height = (int)Math.Ceiling(Math.Max(Bounds.Height, 60) * scale);
        Position = new PixelPoint(area.Right - margin - width, area.Bottom - margin - height);
    }
}
