using System.Diagnostics;

namespace VpnClient.UI;

public static class ProcessPicker
{
    public static string? Show(IWin32Window? owner)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id > 4)
                    names.Add(process.ProcessName + ".exe");
            }
            catch (Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        using var form = new Form
        {
            Text = "Выбор процесса",
            ClientSize = new Size(440, 520),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Surface,
            Font = Theme.Body,
            Padding = new Padding(16)
        };

        var search = new Controls.SearchBox("Найти процесс") { Dock = DockStyle.Top };
        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Card,
            ForeColor = Theme.Text,
            Font = Theme.Body,
            IntegralHeight = false,
            ItemHeight = 24
        };

        void Fill()
        {
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var name in names.Where(n => n.IndexOf(search.Query, StringComparison.OrdinalIgnoreCase) >= 0))
                list.Items.Add(name);
            list.EndUpdate();
        }

        Fill();
        search.QueryChanged += (_, _) => Fill();

        string? result = null;
        void Accept()
        {
            if (list.SelectedItem is string name)
            {
                result = name;
                form.DialogResult = DialogResult.OK;
            }
        }

        list.DoubleClick += (_, _) => Accept();

        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Theme.Surface };
        var choose = Pages.PageParts.Button("Выбрать", true);
        choose.SetBounds(200, 14, 110, 36);
        choose.Click += (_, _) => Accept();
        var cancel = Pages.PageParts.Button("Отмена", false);
        cancel.SetBounds(318, 14, 90, 36);
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(choose);
        buttons.Controls.Add(cancel);

        var gap = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Theme.Surface };

        form.Controls.Add(list);
        form.Controls.Add(gap);
        form.Controls.Add(search);
        form.Controls.Add(buttons);
        form.CancelButton = cancel;

        return form.ShowDialog(owner) == DialogResult.OK ? result : null;
    }
}
