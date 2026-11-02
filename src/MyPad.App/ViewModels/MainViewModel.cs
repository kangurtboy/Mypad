using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using MyPad.App.Common;
using MyPad.App.Services;
using MyPad.Core.Enums;
using MyPad.Core.Interfaces;

namespace MyPad.App.ViewModels;

/// <summary>
/// Root ViewModel for MainWindow managing tabs, file operations, and editor state.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IDocumentFileService _documentFileService;
    private readonly IFileDialogService? _fileDialogService;

    private DocumentTabViewModel? _selectedTab;
    private bool _isStatusBarVisible = true;
    private ElementTheme _appTheme = ElementTheme.Default;
    private int _untitledCounter = 1;

    public MainViewModel(IDocumentFileService? documentFileService = null, IFileDialogService? fileDialogService = null)
    {
        _documentFileService = documentFileService ?? AppServices.DocumentFileService;
        _fileDialogService = fileDialogService;

        Tabs = new ObservableCollection<DocumentTabViewModel>();

        NewTabCommand = new RelayCommand(() => CreateNewTab());
        OpenCommand = new AsyncRelayCommand(OpenFileAsync);
        SaveCommand = new AsyncRelayCommand(SaveCurrentFileAsync, () => SelectedTab is not null);
        SaveAsCommand = new AsyncRelayCommand(SaveCurrentFileAsAsync, () => SelectedTab is not null);
        CloseTabCommand = new AsyncRelayCommand<DocumentTabViewModel>(CloseTabAsync);

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

        // Create initial blank tab
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
    public AsyncRelayCommand OpenCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand SaveAsCommand { get; }
    public AsyncRelayCommand<DocumentTabViewModel> CloseTabCommand { get; }
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
        NextTabCommand.RaiseCanExecuteChanged();
        PreviousTabCommand.RaiseCanExecuteChanged();
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
            NextTabCommand.RaiseCanExecuteChanged();
            PreviousTabCommand.RaiseCanExecuteChanged();
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
                    return; // user canceled the save dialog
                }
            }
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        NextTabCommand.RaiseCanExecuteChanged();
        PreviousTabCommand.RaiseCanExecuteChanged();

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
