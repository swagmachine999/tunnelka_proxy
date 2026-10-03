namespace Tunnelka.UI.Controls;

public class HotkeyBox : ThemedControl
{
    private Keys _keys;
    private bool _capturing;

    public event EventHandler? KeysChanged;

    public HotkeyBox(Keys keys)
    {
        _keys = keys;
        Size = new Size(Theme.Px(170), Theme.Px(34));
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    public Keys Keys => _keys;

    protected override Color Background => Parent?.BackColor ?? Theme.Card;

    protected override void Draw(Graphics g)
    {
        var rect = new RectangleF(0.5f, 0.5f, W - 1.5f, H - 1.5f);
        Theme.FillRounded(g, _capturing ? Theme.CardSelected : Theme.Blend(Theme.Surface, Theme.CardHover, Hover), rect, 10);
        Theme.DrawRounded(g, _capturing ? Theme.Accent : Theme.Border, rect, 10, _capturing ? 1.4f : 1);
        var text = _capturing ? L.T("Нажмите клавиши…") : GlobalHotkey.Describe(_keys);
        Theme.DrawText(g, text, Theme.BodyBold, _capturing ? Theme.AccentStrong : Theme.Text, rect, StringAlignment.Center);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        _capturing = true;
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _capturing = false;
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) => _capturing || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_capturing)
            return;

        e.Handled = true;
        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Escape)
        {
            _capturing = false;
            Invalidate();
            return;
        }

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return;

        var functionKey = e.KeyCode is >= Keys.F1 and <= Keys.F24;
        if (e.Modifiers == Keys.None && !functionKey)
            return;

        _keys = e.KeyData;
        _capturing = false;
        Invalidate();
        KeysChanged?.Invoke(this, EventArgs.Empty);
    }
}
