using Microsoft.UI.Xaml;

namespace MyPad.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += (s, e) =>
        {
            e.Handled = true;
            try
            {
                var logFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyPad", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logFile)!);
                System.IO.File.AppendAllText(logFile, $"[{DateTime.Now}] Unhandled XAML Exception: {e.Exception}\n");
            }
            catch { }
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var logFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyPad", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logFile)!);
                System.IO.File.AppendAllText(logFile, $"[{DateTime.Now}] AppDomain Exception: {e.ExceptionObject}\n");
            }
            catch { }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var filesToOpen = new List<string>();

        // 1. Check WinRT AppLifecycle activation arguments (e.g. File activation in WinUI 3)
        try
        {
            var appArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            if (appArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
            {
                if (appArgs.Data is Windows.ApplicationModel.Activation.IFileActivatedEventArgs fileArgs)
                {
                    foreach (var item in fileArgs.Files)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Path) && System.IO.File.Exists(item.Path))
                        {
                            var full = System.IO.Path.GetFullPath(item.Path);
                            if (!filesToOpen.Contains(full, StringComparer.OrdinalIgnoreCase))
                            {
                                filesToOpen.Add(full);
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // 2. Check standard command line arguments (e.g. Explorer "Open with" or shell command)
        var cmdArgs = Environment.GetCommandLineArgs();
        for (int i = 1; i < cmdArgs.Length; i++)
        {
            var candidate = cmdArgs[i].Trim('\"');
            if (!string.IsNullOrWhiteSpace(candidate) && System.IO.File.Exists(candidate))
            {
                var full = System.IO.Path.GetFullPath(candidate);
                if (!filesToOpen.Contains(full, StringComparer.OrdinalIgnoreCase))
                {
                    filesToOpen.Add(full);
                }
            }
        }

        _window = new Views.MainWindow.MainWindow(filesToOpen);
        _window.Activate();
    }
}
