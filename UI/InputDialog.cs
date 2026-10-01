namespace VpnClient.UI;

public static class InputDialog
{
    public static string? Show(IWin32Window owner, string title, string prompt)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(520, 110),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false
        };

        var label = new Label { Text = prompt, Left = 12, Top = 12, AutoSize = true };
        var box = new TextBox { Left = 12, Top = 38, Width = 496 };
        var ok = new Button { Text = "OK", Left = 352, Top = 72, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Отмена", Left = 433, Top = 72, Width = 75, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { label, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        if (form.ShowDialog(owner) != DialogResult.OK || string.IsNullOrWhiteSpace(box.Text))
            return null;

        return box.Text.Trim();
    }
}
