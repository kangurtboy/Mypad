using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using MyPad.App.Common;
using MyPad.Core.Enums;

namespace MyPad.App.ViewModels;

/// <summary>
/// Root ViewModel for MainWindow.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private DocumentTabViewModel? _selectedTab;
    private bool _isStatusBarVisible = true;
    private ElementTheme _appTheme = ElementTheme.Default;
    private int _untitledCounter = 1;

    public MainViewModel()
    {
        Tabs = new ObservableCollection<DocumentTabViewModel>();

        NewTabCommand = new RelayCommand(() => CreateNewTab());
        CloseTabCommand = new RelayCommand<DocumentTabViewModel>(CloseTab);
        ZoomInCommand = new RelayCommand(ZoomIn, () => SelectedTab is not null);
        ZoomOutCommand = new RelayCommand(ZoomOut, () => SelectedTab is not null);
        ResetZoomCommand = new RelayCommand(ResetZoom, () => SelectedTab is not null);
        ToggleWordWrapCommand = new RelayCommand(ToggleWordWrap, () => SelectedTab is not null);
        ToggleStatusBarCommand = new RelayCommand(ToggleStatusBar);
        SetThemeCommand = new RelayCommand<string>(SetTheme);
        NextTabCommand = new RelayCommand(SelectNextTab, () => Tabs.Count > 1);
        PreviousTabCommand = new RelayCommand(SelectPreviousTab, () => Tabs.Count > 1);

        // Create the initial tab
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
                ZoomInCommand.RaiseCanExecuteChanged();
                ZoomOutCommand.RaiseCanExecuteChanged();
                ResetZoomCommand.RaiseCanExecuteChanged();
                ToggleWordWrapCommand.RaiseCanExecuteChanged();
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
    public RelayCommand<DocumentTabViewModel> CloseTabCommand { get; }
    public RelayCommand ZoomInCommand { get; }
    public RelayCommand ZoomOutCommand { get; }
    public RelayCommand ResetZoomCommand { get; }
    public RelayCommand ToggleWordWrapCommand { get; }
    public RelayCommand ToggleStatusBarCommand { get; }
    public RelayCommand<string> SetThemeCommand { get; }
    public RelayCommand NextTabCommand { get; }
    public RelayCommand PreviousTabCommand { get; }

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

    public void CloseTab(DocumentTabViewModel? tab)
    {
        if (tab is null)
        {
            tab = SelectedTab;
        }

        if (tab is null || !Tabs.Contains(tab))
        {
            return;
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
}
