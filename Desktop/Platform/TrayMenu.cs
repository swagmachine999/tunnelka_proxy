using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tunnelka.Desktop;

public sealed record TrayMenuItem(string Text, Action Run);

public sealed class TrayMenu : Window
{
    private const double MenuWidth = 224;
    private const double RowHeight = 38;
    private const double FramePadding = 6;

    public TrayMenu(IReadOnlyList<TrayMenuItem> items, double scale)
    {
        SystemDecorations = SystemDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        CanResize = false;
        Title = "Tunnelka";
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Width = MenuWidth * scale;
        Height = (items.Count * RowHeight + FramePadding * 2 + 2) * scale;
        Content = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(scale, scale),
            Child = BuildFrame(items)
        };

        Deactivated += (_, _) => Dismiss();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Dismiss();
        };
    }

    public void ShowAt(PixelPoint cursor)
    {
        var screen = Screens.ScreenFromPoint(cursor) ?? Screens.Primary;
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1040);
        var density = screen?.Scaling ?? 1.0;
        var width = (int)Math.Ceiling(Width * density);
        var height = (int)Math.Ceiling(Height * density);
        var x = Math.Max(area.X, Math.Min(cursor.X - width, area.Right - width));
        var y = Math.Max(area.Y, Math.Min(cursor.Y - height, area.Bottom - height));
        Position = new PixelPoint(x, y);
        Show();
        Win32.Foreground(this);
        Activate();
    }

    private void Dismiss()
    {
        if (IsVisible)
            Close();
    }

    private Border BuildFrame(IReadOnlyList<TrayMenuItem> items)
    {
        var rows = new StackPanel();
        foreach (var item in items)
            rows.Children.Add(BuildRow(item));

        var frame = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(FramePadding),
            Child = rows
        };
        frame.Paint(Border.BackgroundProperty, "CardBrush");
        frame.Paint(Border.BorderBrushProperty, "BorderBrush2");
        return frame;
    }

    private Border BuildRow(TrayMenuItem item)
    {
        var label = new TextBlock
        {
            Text = item.Text,
            FontSize = 14.3,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        var row = new Border
        {
            Height = RowHeight,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 0),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = label
        };
        row.PointerEntered += (_, _) => row.Background = Ui.Brush(row, "CardSelectedBrush");
        row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
        row.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton != MouseButton.Left)
                return;

            Dismiss();
            item.Run();
        };
        return row;
    }
}
