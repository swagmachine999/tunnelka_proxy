namespace VpnClient.UI;

public static class InputDialog
{
    public static string? Show(IWin32Window owner, string title, string prompt)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(520, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Theme.Window,
            Font = Theme.Body
        };

        var label = new Label
        {
            Text = prompt,
            Left = 20,
            Top = 18,
            AutoSize = true,
            ForeColor = Theme.Text,
            Font = Theme.BodyBold
        };

        var box = new TextBox
        {
            Left = 20,
            Top = 46,
            Width = 480,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = Theme.Text
        };

        var ok = MakeButton("Добавить", Theme.Accent, Color.White, 300, DialogResult.OK);
        var cancel = MakeButton("Отмена", Theme.Sidebar, Theme.Text, 410, DialogResult.Cancel);

        form.Controls.AddRange(new Control[] { label, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        if (form.ShowDialog(owner) != DialogResult.OK || string.IsNullOrWhiteSpace(box.Text))
            return null;

        return box.Text.Trim();
    }

    private static Button MakeButton(string text, Color back, Color fore, int left, DialogResult result)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = 98,
            Width = 100,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Font = Theme.BodyBold,
            DialogResult = result,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
