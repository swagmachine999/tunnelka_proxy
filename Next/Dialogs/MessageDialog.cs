using Avalonia.Controls;

namespace Tunnelka.Next;

public sealed class MessageDialog : DialogBase
{
    private bool _result;

    private MessageDialog(string text, bool question) : base("Tunnelka", 460)
    {
        var view = new ConfirmView("Tunnelka", text, question ? L.T("Да") : L.T("Закрыть"), question ? L.T("Нет") : null);
        this.Paint(BackgroundProperty, "HeroBrush");
        Content = view;
        view.Accepted += (_, _) =>
        {
            _result = true;
            Close();
        };
        view.Declined += (_, _) => Close();
    }

    public static async Task Show(Window? owner, string text)
    {
        await new MessageDialog(text, false).Present(owner);
    }

    public static async Task<bool> Ask(Window? owner, string text)
    {
        var dialog = new MessageDialog(text, true);
        await dialog.Present(owner);
        return dialog._result;
    }
}
