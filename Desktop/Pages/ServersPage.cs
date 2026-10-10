using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Tunnelka.Desktop;

public sealed class ServersPage : UserControl
{
    private readonly Session _session;
    private readonly TextBlock _title;
    private readonly ServerSearchBox _search;
    private readonly ServerIcon _pingAll;
    private readonly Button _scan;
    private readonly Button _share;
    private readonly TextBlock _hint;
    private readonly DispatcherTimer _hintTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private readonly ServerListView _list;
    private IDisposable? _hintBinding;

    public ServersPage(Session session)
    {
        _session = session;

        _title = Ui.Title(L.T("Серверы"));

        _search = new ServerSearchBox();
        _search.QueryChanged += () => _list!.Filter(_search.Query);

        _pingAll = new ServerIcon(ServerIconKind.Gauge, 44, 44);
        ToolTip.SetTip(_pingAll, L.T("Проверить пинг всех серверов"));
        _pingAll.Clicked += () => _ = _session.PingAll();

        var searchRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,Auto"), Height = 44 };
        Grid.SetColumn(_search, 0);
        _search.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_pingAll, 2);
        searchRow.Children.Add(_search);
        searchRow.Children.Add(_pingAll);

        _scan = ActionButton(L.T("Сканировать QR"));
        _scan.Click += async (_, _) => await Scan();
        _share = ActionButton(L.T("Поделиться ключом"));
        _share.Click += (_, _) => Share();
        var actionRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*"), Margin = new Thickness(0, 10, 0, 0), Height = 36 };
        Grid.SetColumn(_scan, 0);
        Grid.SetColumn(_share, 2);
        actionRow.Children.Add(_scan);
        actionRow.Children.Add(_share);

        _hint = new TextBlock
        {
            FontSize = 13 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(2, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false
        };
        _hintTimer.Tick += (_, _) =>
        {
            _hintTimer.Stop();
            _hint.IsVisible = false;
        };

        _list = new ServerListView(session) { Margin = new Thickness(0, 8, 0, 0) };
        _list.PasteRequested += async () => await Paste();
        _list.ManualRequested += () => ManualRequested?.Invoke();
        _list.ShowKeyRequested += info => ShowKeys(info.Title, new List<(string Title, string Url)> { (info.Title, info.Url) });

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*") };
        Grid.SetRow(_title, 0);
        Grid.SetRow(searchRow, 1);
        Grid.SetRow(actionRow, 2);
        Grid.SetRow(_hint, 3);
        Grid.SetRow(_list, 4);
        root.Children.Add(_title);
        root.Children.Add(searchRow);
        root.Children.Add(actionRow);
        root.Children.Add(_hint);
        root.Children.Add(_list);
        Content = root;

        _list.Rebuild();
    }

    public event Action? ManualRequested;

    public void Localize()
    {
        _title.Text = L.T("Серверы");
        _search.Localize();
        ToolTip.SetTip(_pingAll, L.T("Проверить пинг всех серверов"));
        _scan.Content = L.T("Сканировать QR");
        _share.Content = L.T("Поделиться ключом");
        _list.Localize();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _session.ServersChanged += OnServersChanged;
        _session.CardsChanged += OnCardsChanged;
        _session.StateChanged += OnCardsChanged;
        _list.Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _session.ServersChanged -= OnServersChanged;
        _session.CardsChanged -= OnCardsChanged;
        _session.StateChanged -= OnCardsChanged;
    }

    private void OnServersChanged() => _list.Rebuild();

    private void OnCardsChanged() => _list.UpdateCards();

    private static Button ActionButton(string text)
    {
        var button = new Button
        {
            Content = text,
            Height = 36,
            Padding = new Thickness(12, 0),
            FontSize = 14 * 1.1,
            FontFamily = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        button.Classes.Add("soft");
        ServerRes.Bind(button, TemplatedControl.BorderBrushProperty, "AccentBrush");
        return button;
    }

    private void ShowHint(string text, Tone tone)
    {
        _hint.Text = text;
        _hintBinding?.Dispose();
        _hintBinding = ServerRes.Bind(_hint, TextBlock.ForegroundProperty, Ui.ToneBrush(tone));
        _hint.IsVisible = true;
        _hintTimer.Stop();
        _hintTimer.Start();
    }

    private Window? Owner => TopLevel.GetTopLevel(this) as Window;

    private async Task Scan()
    {
        string? text;
        try
        {
            text = await QrTools.ScanAsync(Owner);
        }
        catch (Exception)
        {
            text = null;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            ShowHint(L.T("QR-код не найден"), Tone.Bad);
            return;
        }

        _session.AddInput(text);
    }

    private void Share()
    {
        var keys = _session.Subscriptions.Profiles.Select(p => (p.Title, p.Url)).ToList();
        if (keys.Count == 0)
        {
            ShowHint(L.T("Нет подписок, чтобы поделиться"), Tone.Muted);
            return;
        }

        ShowKeys(L.T("Поделиться ключом"), keys);
    }

    private void ShowKeys(string title, List<(string Title, string Url)> keys) =>
        _ = ShareDialog.Show(Owner, title, keys);

    private async Task Paste()
    {
        string? text = null;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            try
            {
                text = await clipboard.GetTextAsync();
            }
            catch (Exception)
            {
                text = null;
            }
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            ShowHint(L.T("Буфер обмена пуст: сначала скопируй ключ"), Tone.Mid);
            return;
        }

        _session.AddInput(text);
    }
}
