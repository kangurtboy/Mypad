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
/// Root ViewModel for MainWindow managing tabs, file operations, and editor state.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IDocumentFileService _documentFileService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ISessionService _sessionService;

    private DocumentTabViewModel? _selectedTab;
    private bool _isStatusBarVisible = true;
    private ElementTheme _appTheme = ElementTheme.Default;
    private int _untitledCounter = 1;

    public MainViewModel(
        IDocumentFileService? documentFileService = null,
        IFileDialogService? fileDialogService = null,
        ISessionService? sessionService = null)
    {
        _documentFileService = documentFileService ?? AppServices.DocumentFileService;
        _fileDialogService = fileDialogService;
        _sessionService = sessionService ?? AppServices.SessionService;

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
        SetThemeCommand = new RelayCommand<string>(SetTheme);
        NextTabCommand = new RelayCommand(SelectNextTab, () => Tabs.Count > 1);
        PreviousTabCommand = new RelayCommand(SelectPreviousTab, () => Tabs.Count > 1);
        SetLineEndingCommand = new RelayCommand<string>(SetLineEnding, _ => SelectedTab is not null);
        SetEncodingCommand = new RelayCommand<string>(SetEncoding, _ => SelectedTab is not null);
        NewWindowCommand = new RelayCommand(RequestNewWindow);

        // Initial blank tab (can be replaced by session restore)
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
            }
        }
    }

    public bool HasSelectedTab => SelectedTab is not null;

    public bool IsStatusBarVisible
    {
        get => _isStatusBarVisible;
        set => SetProperty(ref _isStatusBarVisible, value);
    }

    public ElementTheme AppTheme
    {
        get => _appTheme;
        set => SetProperty(ref _appTheme, value);
    }

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
    public RelayCommand<string> SetThemeCommand { get; }
    public RelayCommand NextTabCommand { get; }
    public RelayCommand PreviousTabCommand { get; }
    public RelayCommand<string> SetLineEndingCommand { get; }
    public RelayCommand<string> SetEncodingCommand { get; }

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
        await _documentFileService.SaveAsync(doc, SelectedTab.FilePath);
        SelectedTab.MarkSaved(SelectedTab.FilePath);
        return true;
    }

    public async Task<bool> SaveCurrentFileAsAsync()
    {
        if (SelectedTab is null || _fileDialogService is null)
        {
            return false;
        }

        var suggestedName = string.IsNullOrEmpty(SelectedTab.FileName) ? "Untitled.txt" : SelectedTab.FileName;
        var destination = await _fileDialogService.PickSaveFileAsync(suggestedName);

        if (string.IsNullOrWhiteSpace(destination))
        {
            return false;
        }

        var doc = SelectedTab.ToDomainModel();
        await _documentFileService.SaveAsync(doc, destination);
        SelectedTab.MarkSaved(destination);
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
                // Fallback: open folder without selection
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

    private void SetTheme(string? theme)
    {
        AppTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
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
