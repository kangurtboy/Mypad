using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MyPad.App.Services;
using MyPad.App.ViewModels;

namespace MyPad.App.Views.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InvertedBoolToVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public MainWindow() : this((IReadOnlyList<string>?)null)
    {
    }

    public MainWindow(string? initialFilePath)
        : this(string.IsNullOrWhiteSpace(initialFilePath) ? null : new[] { initialFilePath })
    {
    }

    public MainWindow(IReadOnlyList<string>? initialFilePaths)
    {
        InitializeComponent();

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var fileDialogService = new Services.FileDialogService(
            () => hwnd,
            () => (Content as FrameworkElement)?.XamlRoot);

        ViewModel = new MainViewModel(
            AppServices.DocumentFileService,
            fileDialogService,
            AppServices.SessionService,
            AppServices.RecoveryService,
            AppServices.BackupService);

        ViewModel.BeforeSave = () => ActiveEditor?.SaveDocumentToViewModel(ViewModel.SelectedTab);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1050, 680));

        // Seamless integrated title bar (only AppTitleBar acts as the window drag/caption area)
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ApplyTitleBarTheme(ViewModel.AppTheme);
        if (Content is FrameworkElement rootElem)
        {
            rootElem.RequestedTheme = ViewModel.AppTheme;
        }
        UpdateWindowTitle();

        try
        {
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "MyPad.ico");
            if (System.IO.File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
        catch { }

        // Restore previous session and check for crash recovery or open initial file(s)
        _ = InitializeSessionAndRecoveryAsync(initialFilePaths);

        DocTabView.SelectionChanged += (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() => ActiveEditor.FocusEditor());
        };

        // Monitor external file changes when window is focused
        Activated += async (s, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                await ViewModel.CheckExternalChangesForSelectedTabAsync();
            }
        };

        // Handle opening new window (Ctrl+Shift+N)
        ViewModel.NewWindowRequested += (s, e) =>
        {
            var newWindow = new MainWindow();
            newWindow.Activate();
        };

        // Close window when all tabs are closed
        ViewModel.CloseWindowRequested += (s, e) =>
        {
            Close();
        };

        // Initialize layout for view mode
        UpdateViewModeColumns(ViewModel.ViewMode);

        // Persist session state on close
        Closed += async (s, e) =>
        {
            try
            {
                await ViewModel.SaveSessionAsync();
            }
            catch
            {
                // Ignore session save errors on shutdown
            }
        };

        // Enable frosted glass Acrylic / Mica backdrop
        try
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        catch
        {
            try
            {
                SystemBackdrop = new MicaBackdrop();
            }
            catch
            {
                // Graceful fallback for older systems
            }
        }

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ViewMode))
            {
                UpdateViewModeColumns(ViewModel.ViewMode);
            }
            else if (e.PropertyName == nameof(MainViewModel.AppTheme))
            {
                if (Content is FrameworkElement root)
                {
                    root.RequestedTheme = ViewModel.AppTheme;
                    ApplyTitleBarTheme(ViewModel.AppTheme);
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedTab))
            {
                UpdateWindowTitle();
                if (ViewModel.SelectedTab is not null)
                {
                    ViewModel.SelectedTab.PropertyChanged += (st, se) =>
                    {
                        if (se.PropertyName == nameof(DocumentTabViewModel.FileName) ||
                            se.PropertyName == nameof(DocumentTabViewModel.HeaderTitle))
                        {
                            UpdateWindowTitle();
                        }
                    };
                }
            }
        };
    }

    private void UpdateViewModeColumns(MyPad.Core.Enums.MarkdownViewMode mode)
    {
        switch (mode)
        {
            case MyPad.Core.Enums.MarkdownViewMode.EditorOnly:
                EditorCol.Width = new GridLength(1, GridUnitType.Star);
                SplitterCol.Width = new GridLength(0);
                PreviewCol.Width = new GridLength(0);
                break;
            case MyPad.Core.Enums.MarkdownViewMode.PreviewOnly:
                EditorCol.Width = new GridLength(0);
                SplitterCol.Width = new GridLength(0);
                PreviewCol.Width = new GridLength(1, GridUnitType.Star);
                break;
            case MyPad.Core.Enums.MarkdownViewMode.SplitView:
                EditorCol.Width = new GridLength(1, GridUnitType.Star);
                SplitterCol.Width = GridLength.Auto;
                PreviewCol.Width = new GridLength(1, GridUnitType.Star);
                break;
        }
    }

    private async Task InitializeSessionAndRecoveryAsync(IReadOnlyList<string>? initialFilePaths)
    {
        try
        {
            if (initialFilePaths is { Count: > 0 })
            {
                ViewModel.Tabs.Clear();
                foreach (var path in initialFilePaths)
                {
                    if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                    {
                        try
                        {
                            var doc = await AppServices.DocumentFileService.OpenAsync(path);
                            var tab = new DocumentTabViewModel(doc);
                            ViewModel.Tabs.Add(tab);
                        }
                        catch { }
                    }
                }

                if (ViewModel.Tabs.Count > 0)
                {
                    ViewModel.SelectedTab = ViewModel.Tabs[0];
                    ViewModel.UpdateCommandsState();
                    UpdateWindowTitle();
                    ActiveEditor.FocusEditor();
                    return;
                }
            }

            await ViewModel.RestoreSessionAsync();
            await ViewModel.CheckForCrashRecoveryAsync();
            ActiveEditor.FocusEditor();
        }
        catch { }
    }

    private void UpdateWindowTitle()
    {
        var title = ViewModel.SelectedTab is not null
            ? $"{ViewModel.SelectedTab.FileName} - MyPad"
            : "MyPad";

        if (TitleTextBlock is not null)
        {
            TitleTextBlock.Text = title;
        }
        AppWindow.Title = title;
    }

    private void ApplyTitleBarTheme(ElementTheme theme)
    {
        if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

            bool isDark = theme switch
            {
                ElementTheme.Dark => true,
                ElementTheme.Light => false,
                _ => Application.Current.RequestedTheme == ApplicationTheme.Dark
            };

            if (!isDark)
            {
                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 0, 0, 0);
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 0, 0, 0);
            }
            else
            {
                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 255, 255, 255);
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 255, 255, 255);
            }
        }
    }

    private void OnAddTabButtonClick(TabView sender, object args)
    {
        ViewModel.NewTabCommand.Execute(null);
        DispatcherQueue.TryEnqueue(() => ActiveEditor.FocusEditor());
    }

    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is DocumentTabViewModel tab)
        {
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void OnTabContextCloseClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTabViewModel tab)
        {
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void OnTabContextCloseOthersClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTabViewModel tab)
        {
            ViewModel.CloseOtherTabsCommand.Execute(tab);
        }
    }

    private void OnTabContextCloseRightClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTabViewModel tab)
        {
            ViewModel.CloseTabsToTheRightCommand.Execute(tab);
        }
    }

    private void OnTabContextCopyPathClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTabViewModel tab)
        {
            ViewModel.CopyFilePathCommand.Execute(tab);
        }
    }

    private void OnTabContextOpenFolderClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTabViewModel tab)
        {
            ViewModel.OpenContainingFolderCommand.Execute(tab);
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        ActiveEditor.Undo();
    }

    private void OnRedoClick(object sender, RoutedEventArgs e)
    {
        ActiveEditor.Redo();
    }

    private void OnCutClick(object sender, RoutedEventArgs e)
    {
        ActiveEditor.Cut();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        ActiveEditor.Copy();
    }

    private void OnPasteClick(object sender, RoutedEventArgs e)
    {
        ActiveEditor.Paste();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ActiveEditor.SelectAll();
    }

    private void OnBoldMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertBold();
    private void OnItalicMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertItalic();
    private void OnUnderlineMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertUnderline();
    private void OnStrikethroughMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertStrikethrough();
    private void OnInlineCodeMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertInlineCode();
    private void OnLinkMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertLink();
    private void OnH1MenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertH1();
    private void OnH2MenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertH2();
    private void OnH3MenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertH3();
    private void OnQuoteMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertQuote();
    private void OnBulletListMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertBulletList();
    private void OnNumberedListMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertNumberedList();
    private void OnTaskListMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertTaskList();
    private void OnCodeBlockMenuClick(object sender, RoutedEventArgs e) => ActiveEditor.InsertCodeBlock();

    private void OnFindClick(object sender, RoutedEventArgs e)
    {
        FindBar.ShowFind();
    }

    private void OnReplaceMenuClick(object sender, RoutedEventArgs e)
    {
        FindBar.ShowReplace();
    }

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        if (Content is not FrameworkElement rootElement)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "About MyPad",
            Content = "MyPad is a lightweight, native Windows text editor inspired by Windows Notepad.\n\nVersion: 0.1.0\nFramework: .NET 8 LTS & Windows App SDK (WinUI 3)\nEngine: DirectWrite RichEdit Plain-Text",
            CloseButtonText = "OK",
            XamlRoot = rootElement.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
