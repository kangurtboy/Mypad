namespace MyPad.App.Services;

public enum UnsavedChangesResult
{
    Save,
    DontSave,
    Cancel
}

public interface IFileDialogService
{
    Task<string?> PickOpenFileAsync();
    Task<string?> PickSaveFileAsync(string defaultFileName = "Untitled.txt");
    Task<UnsavedChangesResult> PromptUnsavedChangesAsync(string fileName);
}
