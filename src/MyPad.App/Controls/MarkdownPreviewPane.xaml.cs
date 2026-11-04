using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using MyPad.App.Services;
using Windows.System;

namespace MyPad.App.Controls;

public sealed partial class MarkdownPreviewPane : UserControl
{
    private readonly DispatcherTimer _debounceTimer;
    private string _pendingMarkdown = string.Empty;
    private bool _isWebViewInitialized;
    private ElementTheme _currentTheme = ElementTheme.Default;

    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownPreviewPane),
            new PropertyMetadata(string.Empty, OnMarkdownTextChanged));

    public MarkdownPreviewPane()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _debounceTimer.Tick += (s, e) =>
        {
            _debounceTimer.Stop();
            RenderPendingContent();
        };
    }

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    public ElementTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                RenderPendingContent();
            }
        }
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownPreviewPane pane)
        {
            pane.QueueRender(e.NewValue as string ?? string.Empty);
        }
    }

    private async void OnWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isWebViewInitialized)
        {
            try
            {
                LoadingRing.IsActive = true;
                await PreviewWebView.EnsureCoreWebView2Async();
                ConfigureWebViewSecurity();
                _isWebViewInitialized = true;
                RenderPendingContent();
            }
            catch
            {
                // WebView2 runtime unavailable or error
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }
    }

    private void ConfigureWebViewSecurity()
    {
        if (PreviewWebView.CoreWebView2 is null) return;

        var settings = PreviewWebView.CoreWebView2.Settings;
        settings.IsScriptEnabled = false; // Security: disable script execution
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsWebMessageEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.IsZoomControlEnabled = true;

        PreviewWebView.NavigationStarting += OnNavigationStarting;
        PreviewWebView.CoreWebView2.NewWindowRequested += (s, args) =>
        {
            args.Handled = true;
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
            {
                _ = Launcher.LaunchUriAsync(uri);
            }
        };
    }

    private void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        // Cancel navigation if user clicked an external link and launch in system browser
        if (args.Uri != null && !args.Uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
        {
            args.Cancel = true;
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
            {
                _ = Launcher.LaunchUriAsync(uri);
            }
        }
    }

    public void QueueRender(string markdown)
    {
        _pendingMarkdown = markdown ?? string.Empty;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void RenderPendingContent()
    {
        if (!_isWebViewInitialized || PreviewWebView.CoreWebView2 is null)
        {
            return;
        }

        string rawHtmlBody = AppServices.MarkdownRenderer.RenderToHtml(_pendingMarkdown);
        string fullDocument = GenerateHtmlDocument(rawHtmlBody);

        PreviewWebView.NavigateToString(fullDocument);
    }

    private string GenerateHtmlDocument(string htmlBody)
    {
        bool isDark = _currentTheme switch
        {
            ElementTheme.Dark => true,
            ElementTheme.Light => false,
            _ => Application.Current.RequestedTheme == ApplicationTheme.Dark
        };

        string bgColor = isDark ? "#1f1f1f" : "#ffffff";
        string textColor = isDark ? "#e1e4e8" : "#24292e";
        string codeBg = isDark ? "#2d333b" : "#f6f8fa";
        string borderColor = isDark ? "#373e47" : "#e1e4e8";
        string linkColor = isDark ? "#58a6ff" : "#0366d6";
        string quoteColor = isDark ? "#8b949e" : "#6a737d";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <meta http-equiv=""Content-Security-Policy"" content=""default-src 'none'; style-src 'unsafe-inline'; img-src data: https: http:;"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
            font-size: 14px;
            line-height: 1.6;
            color: {textColor};
            background-color: {bgColor};
            padding: 16px 24px;
            word-wrap: break-word;
            margin: 0;
        }}
        h1, h2, h3, h4, h5, h6 {{
            line-height: 1.25;
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
        }}
        h1 {{ font-size: 2em; border-bottom: 1px solid {borderColor}; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid {borderColor}; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        a {{ color: {linkColor}; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
        code {{
            background-color: {codeBg};
            padding: 0.2em 0.4em;
            border-radius: 4px;
            font-family: 'Cascadia Code', Consolas, monospace;
            font-size: 85%;
        }}
        pre {{
            background-color: {codeBg};
            padding: 16px;
            border-radius: 6px;
            overflow: auto;
            border: 1px solid {borderColor};
        }}
        pre code {{ background-color: transparent; padding: 0; }}
        blockquote {{
            margin: 0 0 16px 0;
            padding: 0 1em;
            color: {quoteColor};
            border-left: 0.25em solid {borderColor};
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
        }}
        table th, table td {{
            padding: 6px 13px;
            border: 1px solid {borderColor};
        }}
        table tr:nth-child(2n) {{
            background-color: {codeBg};
        }}
        hr {{
            border: 0;
            height: 1px;
            background-color: {borderColor};
            margin: 24px 0;
        }}
        ul, ol {{ padding-left: 2em; margin-bottom: 16px; }}
        li + li {{ margin-top: 0.25em; }}
        input[type=""checkbox""] {{ margin-right: 0.5em; }}
    </style>
</head>
<body>
    {htmlBody}
</body>
</html>";
    }
}
