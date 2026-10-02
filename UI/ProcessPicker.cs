using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace VpnClient.UI;

public static class ProcessPicker
{
    private sealed class AppItem
    {
        public AppItem(string name, string path, Image? icon)
        {
            Name = name;
            Path = path;
            Icon = icon;
        }

        public string Name { get; }
        public string Path { get; }
        public Image? Icon { get; }
    }

    public static string? Show(IWin32Window? owner)
    {
        var items = LoadProcesses();

        using var form = new Form
        {
            Text = L.T("Выбор приложения"),
            ClientSize = new Size(Theme.Px(600), Theme.Px(560)),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Surface,
            Font = Theme.Scaled(Theme.Body),
            Padding = Theme.Px(20, 16, 20, 16)
        };

        var title = new Label
        {
            Text = L.T("Выберите приложение"),
            Dock = DockStyle.Top,
            Height = Theme.Px(44),
            Font = Theme.Scaled(Theme.MakeFont(22, FontStyle.Bold)),
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var search = new Controls.SearchBox(L.T("Поиск по имени или пути...")) { Dock = DockStyle.Top };

        var section = new Label
        {
            Text = L.T("Приложения"),
            Dock = DockStyle.Top,
            Height = Theme.Px(40),
            Font = Theme.Scaled(Theme.BodyBold),
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = Theme.Px(0, 0, 0, 6)
        };

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = Theme.Px(54),
            IntegralHeight = false
        };

        var hover = -1;
        list.MouseMove += (_, e) =>
        {
            var index = list.IndexFromPoint(e.Location);
            if (index != hover)
            {
                hover = index;
                list.Invalidate();
            }
        };
        list.MouseLeave += (_, _) =>
        {
            hover = -1;
            list.Invalidate();
        };
        list.DrawItem += (_, e) =>
        {
            if (e.Index < 0 || e.Index >= list.Items.Count)
                return;

            var item = (AppItem)list.Items[e.Index];
            var g = e.Graphics;
            Theme.Smooth(g);

            var selected = (e.State & DrawItemState.Selected) != 0;
            using (var back = new SolidBrush(Theme.Surface))
                g.FillRectangle(back, e.Bounds);

            var state = g.Save();
            g.TranslateTransform(e.Bounds.X, e.Bounds.Y);
            g.ScaleTransform(Theme.S, Theme.S);
            var w = e.Bounds.Width / Theme.S;
            var h = e.Bounds.Height / Theme.S;

            if (selected || e.Index == hover)
                Theme.FillRounded(g, selected ? Theme.CardSelected : Theme.CardHover, new RectangleF(2, 2, w - 4, h - 4), 10);

            if (item.Icon != null)
                g.DrawImage(item.Icon, new RectangleF(16, (h - 28) / 2, 28, 28));
            else
                Theme.DrawBadge(g, new RectangleF(16, (h - 28) / 2, 28, 28), null);

            Theme.DrawText(g, item.Name, Theme.BodyBold, Theme.Text, new RectangleF(58, 7, w - 70, 22));
            Theme.DrawText(g, item.Path, Theme.Caption, Theme.TextMuted, new RectangleF(58, 29, w - 70, 18));

            using (var pen = new Pen(Theme.Border))
                g.DrawLine(pen, 12, h - 0.5f, w - 12, h - 0.5f);

            g.Restore(state);
        };

        void Fill()
        {
            var query = search.Query;
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var item in items.Where(i => query.Length == 0
                || i.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || i.Path.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                list.Items.Add(item);
            list.EndUpdate();
        }

        Fill();
        search.QueryChanged += (_, _) => Fill();

        string? result = null;
        void Accept()
        {
            if (list.SelectedItem is AppItem item)
            {
                result = item.Name;
                form.DialogResult = DialogResult.OK;
            }
        }

        list.DoubleClick += (_, _) => Accept();
        list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                Accept();
        };

        var buttons = new Panel { Dock = DockStyle.Bottom, Height = Theme.Px(56), BackColor = Theme.Surface };
        var choose = Pages.PageParts.Button(L.T("Выбрать"), true);
        var cancel = Pages.PageParts.Button(L.T("Отмена"), false);
        cancel.DialogResult = DialogResult.Cancel;
        choose.Click += (_, _) => Accept();
        buttons.Controls.Add(choose);
        buttons.Controls.Add(cancel);
        buttons.Resize += (_, _) =>
        {
            cancel.SetBounds(buttons.Width - Theme.Px(110), Theme.Px(14), Theme.Px(110), Theme.Px(38));
            choose.SetBounds(cancel.Left - Theme.Px(130), Theme.Px(14), Theme.Px(120), Theme.Px(38));
        };

        form.Controls.Add(list);
        form.Controls.Add(section);
        form.Controls.Add(search);
        form.Controls.Add(title);
        form.Controls.Add(buttons);
        form.CancelButton = cancel;
        form.Shown += (_, _) => search.Focus();

        var answer = form.ShowDialog(owner);
        foreach (var item in items)
            item.Icon?.Dispose();

        return answer == DialogResult.OK ? result : null;
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
                var name = path.Length > 0 ? Path.GetFileName(path) : process.ProcessName + ".exe";
                var key = path.Length > 0 ? path : name;
                if (items.ContainsKey(key))
                    continue;

                items[key] = new AppItem(name, path, LoadIcon(path));
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

    private static Image? LoadIcon(string path)
    {
        if (path.Length == 0)
            return null;

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            return icon?.ToBitmap();
        }
        catch (Exception)
        {
            return null;
        }
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
}
