namespace MyPad.App.Services;

public enum UnsavedChangesResult
{
    Save,
    DontSave,
    Cancel
}

public enum ExternalChangeChoice
{
    Reload,
    KeepLocal
}

public interface IFileDialogService
{
    Task<string?> PickOpenFileAsync();
    Task<string?> PickSaveFileAsync(string defaultFileName = "Untitled.txt");
    Task<UnsavedChangesResult> PromptUnsavedChangesAsync(string fileName);
    Task<bool> PromptCrashRecoveryAsync(int snapshotCount);
    Task<ExternalChangeChoice> PromptExternalChangeConflictAsync(string fileName);
}
