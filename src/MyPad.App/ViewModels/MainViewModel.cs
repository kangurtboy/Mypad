using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using MyPad.App.Common;
using MyPad.App.Services;
using MyPad.Core.Enums;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;
using Windows.ApplicationModel.DataTransfer;

namespace MyPad.App.ViewModels;

/// <summary>
/// Root ViewModel for MainWindow managing tabs, file operations, recovery snapshots, and editor state.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IDocumentFileService _documentFileService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ISessionService _sessionService;
    private readonly IRecoveryService _recoveryService;
    private readonly IBackupService _backupService;
    private readonly IThemeService _themeService;

    private DocumentTabViewModel? _selectedTab;
    private bool _isStatusBarVisible = true;
    private bool _isAutosaveEnabled;
    private ElementTheme _appTheme = ElementTheme.Default;
    private int _untitledCounter = 1;
    private readonly DispatcherTimer _protectionTimer;

    public MainViewModel(
        IDocumentFileService? documentFileService = null,
        IFileDialogService? fileDialogService = null,
        ISessionService? sessionService = null,
        IRecoveryService? recoveryService = null,
        IBackupService? backupService = null,
        IThemeService? themeService = null)
    {
        _documentFileService = documentFileService ?? AppServices.DocumentFileService;
        _fileDialogService = fileDialogService;
        _sessionService = sessionService ?? AppServices.SessionService;
        _recoveryService = recoveryService ?? AppServices.RecoveryService;
        _backupService = backupService ?? AppServices.BackupService;
        _themeService = themeService ?? AppServices.ThemeService;

        _themeService.ThemeChanged += (s, theme) =>
        {
            OnPropertyChanged(nameof(CurrentEditorTheme));
        };

        Tabs = new ObservableCollection<DocumentTabViewModel>();

        NewTabCommand = new RelayCommand(() => CreateNewTab());
        OpenCommand = new AsyncRelayCommand(OpenFileAsync);
        SaveCommand = new AsyncRelayCommand(SaveCurrentFileAsync, () => SelectedTab is not null);
        SaveAsCommand = new AsyncRelayCommand(SaveCurrentFileAsAsync, () => SelectedTab is not null);
        CloseTabCommand = new AsyncRelayCommand<DocumentTabViewModel>(CloseTabAsync);
        CloseOtherTabsCommand = new AsyncRelayCommand<DocumentTabViewModel>(CloseOtherTabsAsync, t => Tabs.Count > 1);
        CloseTabsToTheRightCommand = new AsyncRelayCommand<DocumentTabViewModel>(CloseTabsToTheRightAsync);
        CopyFilePathCommand = new RelayCommand<DocumentTabViewModel>(CopyFilePath, t => !string.IsNullOrEmpty(t?.FilePath));
        OpenContainingFolderCommand = new RelayCommand<DocumentTabViewModel>(OpenContainingFolder, t => !string.IsNullOrEmpty(t?.FilePath));

        ZoomInCommand = new RelayCommand(ZoomIn, () => SelectedTab is not null);
        ZoomOutCommand = new RelayCommand(ZoomOut, () => SelectedTab is not null);
        ResetZoomCommand = new RelayCommand(ResetZoom, () => SelectedTab is not null);
        ToggleWordWrapCommand = new RelayCommand(ToggleWordWrap, () => SelectedTab is not null);
        ToggleStatusBarCommand = new RelayCommand(ToggleStatusBar);
        ToggleAutosaveCommand = new RelayCommand(ToggleAutosave);
        TogglePreviewCommand = new RelayCommand(TogglePreview);
        SetViewModeCommand = new RelayCommand<string>(SetViewMode);
        SetThemeCommand = new RelayCommand<string>(SetTheme);
        SetEditorPaletteCommand = new RelayCommand<string>(palette =>
        {
            if (!string.IsNullOrWhiteSpace(palette))
            {
                _themeService.SetEditorTheme(palette);
            }
        });
        NextTabCommand = new RelayCommand(SelectNextTab, () => Tabs.Count > 1);
        PreviousTabCommand = new RelayCommand(SelectPreviousTab, () => Tabs.Count > 1);
        SetLineEndingCommand = new RelayCommand<string>(SetLineEnding, _ => SelectedTab is not null);
        SetEncodingCommand = new RelayCommand<string>(SetEncoding, _ => SelectedTab is not null);
        NewWindowCommand = new RelayCommand(RequestNewWindow);

        // Data protection timer: takes recovery snapshots of dirty tabs & executes autosave if enabled
        _protectionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _protectionTimer.Tick += async (s, e) => await OnProtectionTimerTickAsync();
        _protectionTimer.Start();

        // Initial blank tab (can be replaced by session restore or crash recovery)
        CreateNewTab();
    }

    public ObservableCollection<DocumentTabViewModel> Tabs { get; }

    public DocumentTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(HasSelectedTab));
                SaveCommand.RaiseCanExecuteChanged();
                SaveAsCommand.RaiseCanExecuteChanged();
                ZoomInCommand.RaiseCanExecuteChanged();
                ZoomOutCommand.RaiseCanExecuteChanged();
                ResetZoomCommand.RaiseCanExecuteChanged();
                ToggleWordWrapCommand.RaiseCanExecuteChanged();
                SetLineEndingCommand.RaiseCanExecuteChanged();
                SetEncodingCommand.RaiseCanExecuteChanged();
                CopyFilePathCommand.RaiseCanExecuteChanged();
                OpenContainingFolderCommand.RaiseCanExecuteChanged();

                _ = CheckExternalChangesForSelectedTabAsync();
            }
        }
    }

    public bool HasSelectedTab => SelectedTab is not null;

    public bool IsStatusBarVisible
    {
        get => _isStatusBarVisible;
        set => SetProperty(ref _isStatusBarVisible, value);
    }

    private MarkdownViewMode _viewMode = MarkdownViewMode.EditorOnly;

    public MarkdownViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (SetProperty(ref _viewMode, value))
            {
                OnPropertyChanged(nameof(IsEditorVisible));
                OnPropertyChanged(nameof(IsPreviewVisible));
                OnPropertyChanged(nameof(IsSplitterVisible));
                OnPropertyChanged(nameof(ViewModeDisplayName));
            }
        }
    }

    public bool IsEditorVisible => ViewMode != MarkdownViewMode.PreviewOnly;
    public bool IsPreviewVisible => ViewMode != MarkdownViewMode.EditorOnly;
    public bool IsSplitterVisible => ViewMode == MarkdownViewMode.SplitView;

    public string ViewModeDisplayName => ViewMode switch
    {
        MarkdownViewMode.EditorOnly => "Editor Only",
        MarkdownViewMode.SplitView => "Split View",
        MarkdownViewMode.PreviewOnly => "Preview Only",
        _ => "Editor Only"
    };

    public bool IsAutosaveEnabled
    {
        get => _isAutosaveEnabled;
        set => SetProperty(ref _isAutosaveEnabled, value);
    }

    public ElementTheme AppTheme
    {
        get => _appTheme;
        set => SetProperty(ref _appTheme, value);
    }

    public EditorTheme CurrentEditorTheme => _themeService.CurrentEditorTheme;
    public IReadOnlyList<EditorTheme> AvailableEditorThemes => _themeService.GetAvailableEditorThemes();

    public RelayCommand NewTabCommand { get; }
    public RelayCommand NewWindowCommand { get; }
    public AsyncRelayCommand OpenCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand SaveAsCommand { get; }
    public AsyncRelayCommand<DocumentTabViewModel> CloseTabCommand { get; }
    public AsyncRelayCommand<DocumentTabViewModel> CloseOtherTabsCommand { get; }
    public AsyncRelayCommand<DocumentTabViewModel> CloseTabsToTheRightCommand { get; }
    public RelayCommand<DocumentTabViewModel> CopyFilePathCommand { get; }
    public RelayCommand<DocumentTabViewModel> OpenContainingFolderCommand { get; }

    public RelayCommand ZoomInCommand { get; }
    public RelayCommand ZoomOutCommand { get; }
    public RelayCommand ResetZoomCommand { get; }
    public RelayCommand ToggleWordWrapCommand { get; }
    public RelayCommand ToggleStatusBarCommand { get; }
    public RelayCommand ToggleAutosaveCommand { get; }
    public RelayCommand TogglePreviewCommand { get; }
    public RelayCommand<string> SetViewModeCommand { get; }
    public RelayCommand<string> SetThemeCommand { get; }
    public RelayCommand NextTabCommand { get; }
    public RelayCommand PreviousTabCommand { get; }
    public RelayCommand<string> SetLineEndingCommand { get; }
    public RelayCommand<string> SetEncodingCommand { get; }
    public RelayCommand<string> SetEditorPaletteCommand { get; }

    public event EventHandler? NewWindowRequested;

    private void RequestNewWindow()
    {
        NewWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    public DocumentTabViewModel CreateNewTab()
    {
        var tab = new DocumentTabViewModel
        {
            FileName = $"Untitled {_untitledCounter++}",
            LineEnding = LineEndingMode.Crlf,
            EncodingName = "UTF-8",
            Content = string.Empty,
            IsModified = false
        };

        Tabs.Add(tab);
        SelectedTab = tab;
        UpdateCommandsState();
        return tab;
    }

    public async Task OpenFileAsync()
    {
        if (_fileDialogService is null)
        {
            return;
        }

        var path = await _fileDialogService.PickOpenFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        // Check if file is already open
        var existing = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            SelectedTab = existing;
            return;
        }

        var doc = await _documentFileService.OpenAsync(path);

        // If current tab is pristine, load into it
        if (SelectedTab is not null &&
            string.IsNullOrEmpty(SelectedTab.FilePath) &&
            string.IsNullOrEmpty(SelectedTab.Content) &&
            !SelectedTab.IsModified)
        {
            SelectedTab.LoadFromDocument(doc);
        }
        else
        {
            var tab = new DocumentTabViewModel(doc);
            Tabs.Add(tab);
            SelectedTab = tab;
            UpdateCommandsState();
        }
    }

    public async Task<bool> SaveCurrentFileAsync()
    {
        if (SelectedTab is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(SelectedTab.FilePath))
        {
            return await SaveCurrentFileAsAsync();
        }

        var doc = SelectedTab.ToDomainModel();

        // 1. Safe write
        await _documentFileService.SaveAsync(doc, SelectedTab.FilePath);

        // 2. Backup creation & retention pruning
        try
        {
            await _backupService.CreateBackupAsync(SelectedTab.FilePath);
            await _backupService.PruneBackupsAsync(SelectedTab.FilePath, maxBackups: 5);
        }
        catch { }

        // 3. Mark tab clean and update fingerprint
        SelectedTab.MarkSaved(doc.FilePath!, doc.FileFingerprint);

        // 4. Remove recovery snapshot on clean save
        try
        {
            await _recoveryService.DeleteSnapshotAsync(SelectedTab.Id);
        }
        catch { }

        return true;
    }

    public async Task<bool> SaveCurrentFileAsAsync()
    {
        if (SelectedTab is null || _fileDialogService is null)
        {
            return false;
        }

        bool preferMarkdown = SelectedTab.IsMarkdown || SelectedTab.HasMarkdownFormatting();
        var suggestedName = string.IsNullOrEmpty(SelectedTab.FileName) ? "Untitled" : SelectedTab.FileName;
        var destination = await _fileDialogService.PickSaveFileAsync(suggestedName, preferMarkdown);

        if (string.IsNullOrWhiteSpace(destination))
        {
            return false;
        }

        if (destination.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            destination.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
        {
            SelectedTab.Kind = DocumentKind.Markdown;
        }
        else
        {
            SelectedTab.Kind = DocumentKind.PlainText;
        }

        var doc = SelectedTab.ToDomainModel();
        await _documentFileService.SaveAsync(doc, destination);

        try
        {
            await _backupService.CreateBackupAsync(destination);
            await _backupService.PruneBackupsAsync(destination, maxBackups: 5);
        }
        catch { }

        SelectedTab.MarkSaved(doc.FilePath!, doc.FileFingerprint);

        try
        {
            await _recoveryService.DeleteSnapshotAsync(SelectedTab.Id);
        }
        catch { }

        return true;
    }

    public async Task CloseTabAsync(DocumentTabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab is null || !Tabs.Contains(tab))
        {
            return;
        }

        if (tab.IsModified && _fileDialogService is not null)
        {
            var choice = await _fileDialogService.PromptUnsavedChangesAsync(tab.FileName);
            if (choice == UnsavedChangesResult.Cancel)
            {
                return;
            }

            if (choice == UnsavedChangesResult.Save)
            {
                SelectedTab = tab;
                var saved = await SaveCurrentFileAsync();
                if (!saved)
                {
                    return;
                }
            }
        }

        // Clean up recovery snapshot
        try
        {
            await _recoveryService.DeleteSnapshotAsync(tab.Id);
        }
        catch { }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        UpdateCommandsState();

        if (Tabs.Count == 0)
        {
            CreateNewTab();
        }
        else if (SelectedTab == tab)
        {
            var nextIndex = Math.Min(index, Tabs.Count - 1);
            SelectedTab = Tabs[nextIndex];
        }
    }

    public async Task CloseOtherTabsAsync(DocumentTabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab is null) return;

        var otherTabs = Tabs.Where(t => t != tab).ToList();
        foreach (var other in otherTabs)
        {
            await CloseTabAsync(other);
        }
    }

    public async Task CloseTabsToTheRightAsync(DocumentTabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab is null) return;

        int index = Tabs.IndexOf(tab);
        if (index < 0 || index >= Tabs.Count - 1) return;

        var rightTabs = Tabs.Skip(index + 1).ToList();
        foreach (var right in rightTabs)
        {
            await CloseTabAsync(right);
        }
    }

    private void CopyFilePath(DocumentTabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (!string.IsNullOrEmpty(tab?.FilePath))
        {
            var package = new DataPackage();
            package.SetText(tab.FilePath);
            Clipboard.SetContent(package);
        }
    }

    private void OpenContainingFolder(DocumentTabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (!string.IsNullOrEmpty(tab?.FilePath) && File.Exists(tab.FilePath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{tab.FilePath}\"");
            }
            catch
            {
                string? dir = Path.GetDirectoryName(tab.FilePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", dir);
                }
            }
        }
    }

    public async Task SaveSessionAsync()
    {
        var session = new SessionState
        {
            SelectedTabIndex = SelectedTab is not null ? Tabs.IndexOf(SelectedTab) : 0,
            SavedAtUtc = DateTime.UtcNow
        };

        foreach (var tab in Tabs)
        {
            session.Tabs.Add(new SessionTabState
            {
                Id = tab.Id,
                FilePath = tab.FilePath,
                Title = tab.FileName,
                Content = tab.Content,
                IsModified = tab.IsModified,
                LineEnding = tab.LineEnding,
                EncodingName = tab.EncodingName,
                CaretLine = tab.CaretLine,
                CaretColumn = tab.CaretColumn,
                ZoomLevel = tab.ZoomLevel
            });
        }

        await _sessionService.SaveSessionAsync(session);
    }

    public async Task<bool> RestoreSessionAsync()
    {
        var session = await _sessionService.LoadSessionAsync();
        if (session is null || session.Tabs.Count == 0)
        {
            return false;
        }

        Tabs.Clear();
        foreach (var tabState in session.Tabs)
        {
            var tab = new DocumentTabViewModel
            {
                FilePath = tabState.FilePath,
                FileName = tabState.Title,
                Content = tabState.Content,
                IsModified = tabState.IsModified,
                LineEnding = tabState.LineEnding,
                EncodingName = tabState.EncodingName,
                CaretLine = tabState.CaretLine,
                CaretColumn = tabState.CaretColumn,
                ZoomLevel = tabState.ZoomLevel
            };
            Tabs.Add(tab);
        }

        int selectedIdx = Math.Clamp(session.SelectedTabIndex, 0, Tabs.Count - 1);
        SelectedTab = Tabs[selectedIdx];
        UpdateCommandsState();
        return true;
    }

    public async Task CheckForCrashRecoveryAsync()
    {
        try
        {
            var snapshots = await _recoveryService.LoadAllSnapshotsAsync();
            if (snapshots.Count == 0 || _fileDialogService is null)
            {
                return;
            }

            bool shouldRecover = await _fileDialogService.PromptCrashRecoveryAsync(snapshots.Count);
            if (shouldRecover)
            {
                foreach (var snapshot in snapshots)
                {
                    var tab = new DocumentTabViewModel
                    {
                        FilePath = snapshot.OriginalFilePath,
                        FileName = snapshot.Title,
                        Content = snapshot.Content,
                        IsModified = true,
                        LineEnding = snapshot.LineEnding,
                        EncodingName = snapshot.EncodingWebName,
                        Kind = snapshot.Kind
                    };
                    Tabs.Add(tab);
                    SelectedTab = tab;
                }
                UpdateCommandsState();
            }
            else
            {
                await _recoveryService.ClearAllSnapshotsAsync();
            }
        }
        catch { }
    }

    public async Task CheckExternalChangesForSelectedTabAsync()
    {
        if (SelectedTab is null || string.IsNullOrEmpty(SelectedTab.FilePath) || !File.Exists(SelectedTab.FilePath))
        {
            return;
        }

        var doc = SelectedTab.ToDomainModel();
        if (_documentFileService.CheckForExternalChanges(doc))
        {
            if (!SelectedTab.IsModified)
            {
                // Silently reload if no unsaved local changes
                var updated = await _documentFileService.OpenAsync(SelectedTab.FilePath);
                SelectedTab.LoadFromDocument(updated);
            }
            else if (_fileDialogService is not null)
            {
                var choice = await _fileDialogService.PromptExternalChangeConflictAsync(SelectedTab.FileName);
                if (choice == ExternalChangeChoice.Reload)
                {
                    var updated = await _documentFileService.OpenAsync(SelectedTab.FilePath);
                    SelectedTab.LoadFromDocument(updated);
                }
                else
                {
                    // Keep local: update fingerprint to current file state to avoid repeated prompts
                    var fileInfo = new FileInfo(SelectedTab.FilePath);
                    SelectedTab.FileFingerprint = $"{fileInfo.LastWriteTimeUtc.Ticks}_{fileInfo.Length}";
                }
            }
        }
    }

    private async Task OnProtectionTimerTickAsync()
    {
        // 1. Take recovery snapshots for all dirty tabs
        foreach (var tab in Tabs)
        {
            if (tab.IsModified && !string.IsNullOrEmpty(tab.Content))
            {
                try
                {
                    await _recoveryService.SaveSnapshotAsync(tab.ToDomainModel());
                }
                catch { }
            }
        }

        // 2. Autosave if enabled
        if (IsAutosaveEnabled)
        {
            foreach (var tab in Tabs)
            {
                if (tab.IsModified && !string.IsNullOrEmpty(tab.FilePath))
                {
                    try
                    {
                        var doc = tab.ToDomainModel();
                        await _documentFileService.SaveAsync(doc, tab.FilePath);
                        tab.MarkSaved(doc.FilePath!, doc.FileFingerprint);
                        await _recoveryService.DeleteSnapshotAsync(tab.Id);
                    }
                    catch { }
                }
            }
        }
    }

    public void SelectNextTab()
    {
        if (Tabs.Count <= 1 || SelectedTab is null)
        {
            return;
        }

        var currentIndex = Tabs.IndexOf(SelectedTab);
        var nextIndex = (currentIndex + 1) % Tabs.Count;
        SelectedTab = Tabs[nextIndex];
    }

    public void SelectPreviousTab()
    {
        if (Tabs.Count <= 1 || SelectedTab is null)
        {
            return;
        }

        var currentIndex = Tabs.IndexOf(SelectedTab);
        var prevIndex = (currentIndex - 1 + Tabs.Count) % Tabs.Count;
        SelectedTab = Tabs[prevIndex];
    }

    private void UpdateCommandsState()
    {
        NextTabCommand.RaiseCanExecuteChanged();
        PreviousTabCommand.RaiseCanExecuteChanged();
        CloseOtherTabsCommand.RaiseCanExecuteChanged();
        CloseTabsToTheRightCommand.RaiseCanExecuteChanged();
    }

    private void ZoomIn()
    {
        if (SelectedTab is not null)
        {
            SelectedTab.ZoomLevel += 10;
        }
    }

    private void ZoomOut()
    {
        if (SelectedTab is not null)
        {
            SelectedTab.ZoomLevel -= 10;
        }
    }

    private void ResetZoom()
    {
        if (SelectedTab is not null)
        {
            SelectedTab.ZoomLevel = 100;
        }
    }

    private void ToggleWordWrap()
    {
        if (SelectedTab is not null)
        {
            SelectedTab.IsWordWrapEnabled = !SelectedTab.IsWordWrapEnabled;
        }
    }

    private void ToggleStatusBar()
    {
        IsStatusBarVisible = !IsStatusBarVisible;
    }

    private void ToggleAutosave()
    {
        IsAutosaveEnabled = !IsAutosaveEnabled;
    }

    private void TogglePreview()
    {
        ViewMode = ViewMode switch
        {
            MarkdownViewMode.EditorOnly => MarkdownViewMode.SplitView,
            MarkdownViewMode.SplitView => MarkdownViewMode.PreviewOnly,
            _ => MarkdownViewMode.EditorOnly
        };
    }

    private void SetViewMode(string? mode)
    {
        ViewMode = mode switch
        {
            "Split" => MarkdownViewMode.SplitView,
            "Preview" => MarkdownViewMode.PreviewOnly,
            _ => MarkdownViewMode.EditorOnly
        };
    }

    private void SetTheme(string? theme)
    {
        AppTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (theme == "Light" && CurrentEditorTheme.Appearance == "dark")
        {
            _themeService.SetEditorTheme("Default Light");
        }
        else if (theme == "Dark" && CurrentEditorTheme.Appearance == "light")
        {
            _themeService.SetEditorTheme("Default Dark");
        }
    }

    private void SetLineEnding(string? mode)
    {
        if (SelectedTab is null || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        SelectedTab.LineEnding = mode.ToUpperInvariant() switch
        {
            "CRLF" => LineEndingMode.Crlf,
            "LF" => LineEndingMode.Lf,
            "CR" => LineEndingMode.Cr,
            _ => LineEndingMode.Crlf
        };
        SelectedTab.IsModified = true;
    }

    private void SetEncoding(string? encoding)
    {
        if (SelectedTab is null || string.IsNullOrWhiteSpace(encoding))
        {
            return;
        }

        SelectedTab.EncodingName = encoding;
        SelectedTab.IsModified = true;
    }
}
