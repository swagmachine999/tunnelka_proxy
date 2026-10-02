namespace VpnClient.UI;

public static class LinkDialog
{
    public static void Show(IWin32Window owner, string title, IReadOnlyList<(string Name, string Link)> keys, bool qr)
    {
        var top = keys.Count > 1 ? 56 : 0;
        var qrSize = qr ? 280 : 0;
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(Theme.Px(460), Theme.Px(top + qrSize + 150)),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Window,
            Font = Theme.Scaled(Theme.Body)
        };
        NativeTheme.TitleBar(form, Theme.IsDark);

        var link = keys[0].Link;
        var modules = qr ? QrCode.Encode(link) : null;

        var picture = new Panel
        {
            Left = (form.ClientSize.Width - Theme.Px(qrSize)) / 2,
            Top = Theme.Px(top + 16),
            Width = Theme.Px(qrSize),
            Height = Theme.Px(qrSize),
            BackColor = Color.White,
            Visible = qr
        };
        picture.Paint += (_, e) =>
        {
            if (modules != null)
                DrawQr(e.Graphics, modules, picture.ClientRectangle);
        };

        var box = new TextBox
        {
            Text = link,
            ReadOnly = true,
            Left = Theme.Px(20),
            Top = Theme.Px(top + qrSize + 32),
            Width = Theme.Px(420),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.Card,
            ForeColor = Theme.Text
        };

        var hint = new Label
        {
            Text = qr ? "Отсканируй камерой на новом устройстве" : "",
            Left = Theme.Px(20),
            Top = Theme.Px(top + qrSize + 66),
            Width = Theme.Px(420),
            ForeColor = Theme.TextMuted,
            Font = Theme.Scaled(Theme.Caption)
        };

        var copy = Button("Копировать", Theme.Accent, Color.White, 220, top + qrSize);
        copy.Click += (_, _) =>
        {
            Clipboard.SetText(link);
            copy.Text = "Скопировано";
        };

        var close = Button("Закрыть", Theme.Sidebar, Theme.Text, 340, top + qrSize);
        close.DialogResult = DialogResult.Cancel;

        if (keys.Count > 1)
        {
            var choice = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = Theme.Px(20),
                Top = Theme.Px(18),
                Width = Theme.Px(420),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Card,
                ForeColor = Theme.Text
            };
            choice.Items.AddRange(keys.Select(k => (object)k.Name).ToArray());
            choice.SelectedIndex = 0;
            choice.SelectedIndexChanged += (_, _) =>
            {
                link = keys[choice.SelectedIndex].Link;
                modules = qr ? QrCode.Encode(link) : null;
                box.Text = link;
                copy.Text = "Копировать";
                picture.Invalidate();
            };
            form.Controls.Add(choice);
        }

        form.Controls.AddRange(new Control[] { picture, box, hint, copy, close });
        form.CancelButton = close;
        form.Shown += (_, _) => close.Focus();
        form.ShowDialog(owner);
    }

    private static void DrawQr(Graphics g, bool[,] modules, Rectangle bounds)
    {
        var size = modules.GetLength(0);
        var cell = Math.Max(1, bounds.Width / (size + 8));
        var offset = (bounds.Width - cell * size) / 2;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (modules[y, x])
                    g.FillRectangle(Brushes.Black, offset + x * cell, offset + y * cell, cell, cell);
            }
        }
    }

    private static Button Button(string text, Color back, Color fore, int left, int top)
    {
        var button = new Button
        {
            Text = text,
            Left = Theme.Px(left),
            Top = Theme.Px(top + 96),
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
