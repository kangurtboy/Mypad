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

        ViewModel = new MainViewModel(AppServices.DocumentFileService, fileDialogService);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1050, 680));

        // Seamless integrated title bar (only AppTitleBar acts as the window drag/caption area)
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ApplyTitleBarTheme(ViewModel.AppTheme);
        UpdateWindowTitle();

        // Enable Mica backdrop when supported
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            // Graceful fallback for Windows 10
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

            if (theme == ElementTheme.Light)
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
