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

    public MainWindow()
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

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1050, 680));

        // Seamless integrated title bar (only AppTitleBar acts as the window drag/caption area)
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ApplyTitleBarTheme(ViewModel.AppTheme);
        UpdateWindowTitle();

        // Restore previous session and check for crash recovery
        _ = InitializeSessionAndRecoveryAsync();

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
            if (e.PropertyName == nameof(MainViewModel.AppTheme))
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

    private async Task InitializeSessionAndRecoveryAsync()
    {
        await ViewModel.RestoreSessionAsync();
        await ViewModel.CheckForCrashRecoveryAsync();
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
