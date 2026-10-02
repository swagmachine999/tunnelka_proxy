namespace VpnClient.UI;

public static class LinkDialog
{
    public static void Show(IWin32Window owner, string title, string link, bool qr)
    {
        var qrSize = qr ? 280 : 0;
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(Theme.Px(460), Theme.Px(qrSize + 150)),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Window,
            Font = Theme.Scaled(Theme.Body)
        };
        NativeTheme.TitleBar(form, Theme.IsDark);

        if (qr)
        {
            var modules = QrCode.Encode(link);
            var picture = new Panel
            {
                Left = (form.ClientSize.Width - Theme.Px(qrSize)) / 2,
                Top = Theme.Px(16),
                Width = Theme.Px(qrSize),
                Height = Theme.Px(qrSize),
                BackColor = Color.White
            };
            picture.Paint += (_, e) => DrawQr(e.Graphics, modules, picture.ClientRectangle);
            form.Controls.Add(picture);
        }

        var box = new TextBox
        {
            Text = link,
            ReadOnly = true,
            Left = Theme.Px(20),
            Top = Theme.Px(qrSize + 32),
            Width = Theme.Px(420),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.Card,
            ForeColor = Theme.Text
        };

        var hint = new Label
        {
            Text = qr ? "Отсканируй камерой на новом устройстве" : "",
            Left = Theme.Px(20),
            Top = Theme.Px(qrSize + 66),
            Width = Theme.Px(420),
            ForeColor = Theme.TextMuted,
            Font = Theme.Scaled(Theme.Caption)
        };

        var copy = Button("Копировать", Theme.Accent, Color.White, 220, qrSize);
        copy.Click += (_, _) =>
        {
            Clipboard.SetText(link);
            copy.Text = "Скопировано";
        };

        var close = Button("Закрыть", Theme.Sidebar, Theme.Text, 340, qrSize);
        close.DialogResult = DialogResult.Cancel;

        form.Controls.AddRange(new Control[] { box, hint, copy, close });
        form.CancelButton = close;
        form.Shown += (_, _) => close.Focus();
        form.ShowDialog(owner);
    }

    private static void DrawQr(Graphics g, bool[,] modules, Rectangle bounds)
    {
        var count = modules.GetLength(0) + 8;
        var cell = Math.Max(1, bounds.Width / count);
        var offset = (bounds.Width - cell * modules.GetLength(0)) / 2;
        for (var y = 0; y < modules.GetLength(0); y++)
        {
            for (var x = 0; x < modules.GetLength(0); x++)
            {
                if (modules[y, x])
                    g.FillRectangle(Brushes.Black, offset + x * cell, offset + y * cell, cell, cell);
            }
        }
    }

    private static Button Button(string text, Color back, Color fore, int left, int qrSize)
    {
        var button = new Button
        {
            Text = text,
            Left = Theme.Px(left),
            Top = Theme.Px(qrSize + 96),
            Width = Theme.Px(110),
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
