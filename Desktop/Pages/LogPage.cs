using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Tunnelka.Desktop;

public sealed class LogPage : UserControl
{
    private const int Limit = 1000;

    private readonly List<string> _lines = new();
    private readonly SelectableTextBlock _text;
    private readonly ScrollViewer _scroll;
    private bool _attached;

    public LogPage(Session session)
    {
        ClearButton = SettingsParts.Pill(L.T("Очистить"), false);
        ClearButton.Click += (_, _) =>
        {
            _lines.Clear();
            Render();
        };

        _text = new SelectableTextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13.2,
            TextWrapping = TextWrapping.Wrap
        };
        _scroll = new ScrollViewer { Content = _text };
        var frame = Ui.Card(_scroll);
        frame.Margin = new Thickness(0, 0, 10, 0);
        Content = frame;

        _lines.AddRange(session.Log.Recent());
        session.LogWritten += Append;
    }

    public Button ClearButton { get; }

    public void Localize() => ClearButton.Content = L.T("Очистить");

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Render();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void Append(string line)
    {
        _lines.Add(line);
        if (_lines.Count > Limit)
            _lines.RemoveRange(0, _lines.Count - Limit);

        if (_attached)
            Render();
    }

    private void Render()
    {
        _text.Text = string.Join("\n", _lines);
        Dispatcher.UIThread.Post(() => _scroll.ScrollToEnd(), DispatcherPriority.Background);
    }
}
