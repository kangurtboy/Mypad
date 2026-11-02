using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MyPad.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    private readonly Func<IntPtr> _getWindowHandle;
    private readonly Func<XamlRoot?> _getXamlRoot;

    public FileDialogService(Func<IntPtr> getWindowHandle, Func<XamlRoot?> getXamlRoot)
    {
        _getWindowHandle = getWindowHandle ?? throw new ArgumentNullException(nameof(getWindowHandle));
        _getXamlRoot = getXamlRoot ?? throw new ArgumentNullException(nameof(getXamlRoot));
    }

    public async Task<string?> PickOpenFileAsync()
    {
        var picker = new FileOpenPicker();
        var hwnd = _getWindowHandle();
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".markdown");
        picker.FileTypeFilter.Add(".log");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".xml");
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickSaveFileAsync(string defaultFileName = "Untitled.txt")
    {
        var picker = new FileSavePicker();
        var hwnd = _getWindowHandle();
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = defaultFileName;
        picker.FileTypeChoices.Add("Plain Text (*.txt)", new List<string> { ".txt" });
        picker.FileTypeChoices.Add("Markdown Document (*.md)", new List<string> { ".md" });
        picker.FileTypeChoices.Add("All Files (*.*)", new List<string> { "." });

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task<UnsavedChangesResult> PromptUnsavedChangesAsync(string fileName)
    {
        var root = _getXamlRoot();
        if (root is null)
        {
            return UnsavedChangesResult.Cancel;
        }

        var dialog = new ContentDialog
        {
            Title = "Save Changes?",
            Content = $"Do you want to save changes to \"{fileName}\" before closing?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Don't Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root
        };

        var result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => UnsavedChangesResult.Save,
            ContentDialogResult.Secondary => UnsavedChangesResult.DontSave,
            _ => UnsavedChangesResult.Cancel
        };
    }
}
