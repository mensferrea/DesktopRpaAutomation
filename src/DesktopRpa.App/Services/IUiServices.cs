using System.Windows.Input;

namespace DesktopRpa.App.Services;

public interface IDialogService
{
    string? ShowOpenFileDialog(string title, string filter);
    string? ShowSaveFileDialog(string title, string filter, string defaultFileName);
    void ShowInformation(string message, string title = "Информация");
    void ShowWarning(string message, string title = "Предупреждение");
    void ShowError(string message, string title = "Ошибка");
}

public interface IClipboardService
{
    void SetText(string text);
}
