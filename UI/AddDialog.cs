namespace VpnClient.UI;

public static class AddDialog
{
    public static string? Show(IWin32Window owner)
    {
        using var form = new Form
        {
            Text = "Добавить",
            ClientSize = new Size(Theme.Px(540), Theme.Px(170)),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Window,
            Font = Theme.Scaled(Theme.Body)
        };

        var label = new Label
        {
            Text = "Ключ (vless://, vmess://, trojan://, ss://) или ссылка на подписку",
            Left = Theme.Px(20),
            Top = Theme.Px(18),
            AutoSize = true,
            ForeColor = Theme.Text,
            Font = Theme.Scaled(Theme.BodyBold)
        };

        var box = new Controls.SearchBox("vless://...  или  https://подписка", false)
        {
            Left = Theme.Px(20),
            Top = Theme.Px(46),
            Width = Theme.Px(500)
        };

        string? result = null;

        var paste = MakeButton("Вставить из буфера", Theme.Card, Theme.AccentStrong, 20, 170);
        paste.FlatAppearance.BorderSize = 1;
        paste.FlatAppearance.BorderColor = Theme.Accent;
        paste.Click += (_, _) =>
        {
            var text = Clipboard.GetText().Trim();
            if (text.Length == 0)
            {
                label.Text = "Буфер обмена пуст";
                return;
            }

            result = text;
            form.DialogResult = DialogResult.OK;
        };

        var add = MakeButton("Добавить", Theme.Accent, Color.White, 310, 100);
        add.Click += (_, _) =>
        {
            if (box.Query.Length == 0)
                return;

            result = box.Query;
            form.DialogResult = DialogResult.OK;
        };

        var cancel = MakeButton("Отмена", Theme.Sidebar, Theme.Text, 420, 100);
        cancel.DialogResult = DialogResult.Cancel;

        form.Controls.AddRange(new Control[] { label, box, paste, add, cancel });
        form.AcceptButton = add;
        form.CancelButton = cancel;

        return form.ShowDialog(owner) == DialogResult.OK ? result : null;
    }

    private static Button MakeButton(string text, Color back, Color fore, int left, int width)
    {
        var button = new Button
        {
            Text = text,
            Left = Theme.Px(left),
            Top = Theme.Px(106),
            Width = Theme.Px(width),
            Height = Theme.Px(36),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Font = Theme.Scaled(Theme.BodyBold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
