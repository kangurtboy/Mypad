using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
        ViewModel = new MainViewModel();

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
                }
            }
        };
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
