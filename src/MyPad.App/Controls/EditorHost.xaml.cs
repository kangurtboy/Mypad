using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MyPad.App.Services;
using MyPad.App.ViewModels;
using MyPad.Core.Enums;
using MyPad.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace MyPad.App.Controls;

public sealed partial class EditorHost : UserControl
{
    private const double BaseFontSize = 14.0;
    private bool _isUpdatingInternally;
    private readonly DispatcherTimer _highlightTimer;

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(
            nameof(Theme),
            typeof(EditorTheme),
            typeof(EditorHost),
            new PropertyMetadata(null, OnThemeChanged));

    public static readonly DependencyProperty TabViewModelProperty =
        DependencyProperty.Register(
            nameof(TabViewModel),
            typeof(DocumentTabViewModel),
            typeof(EditorHost),
            new PropertyMetadata(null, OnTabViewModelChanged));

    public EditorHost()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (DataContext is DocumentTabViewModel vm)
            {
                TabViewModel = vm;
            }
        };

        EditBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnEditBoxKeyDown), true);
        EditBox.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);

        Theme = AppServices.ThemeService.CurrentEditorTheme;
        ApplyTheme(Theme);
        AppServices.ThemeService.ThemeChanged += (s, theme) =>
        {
            Theme = theme;
            ApplyTheme(theme);
        };

        _highlightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _highlightTimer.Tick += (s, e) =>
        {
            _highlightTimer.Stop();
            HighlightMarkdownTokens();
        };
    }

    public EditorTheme? Theme
    {
        get => (EditorTheme?)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EditorHost host && e.NewValue is EditorTheme theme)
        {
            host.ApplyTheme(theme);
        }
    }

    public void ApplyTheme(EditorTheme? theme)
    {
        if (theme is null) return;

        try
        {
            bool isDark = !string.Equals(theme.Appearance, "light", StringComparison.OrdinalIgnoreCase);

            // Sync RequestedTheme with theme appearance so native insertion caret (IBeam) is high-contrast
            EditBox.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
            RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

            var fgColor = HexToColor(theme.Colors.Foreground);
            var fgBrush = new SolidColorBrush(fgColor);
            var bgColor = HexToColor(theme.Colors.Background);
            var bgBrush = new SolidColorBrush(bgColor);
            var selBrush = new SolidColorBrush(HexToColor(theme.Colors.Selection));

            // Cursor/caret color matching the theme
            var cursorColor = !string.IsNullOrWhiteSpace(theme.Colors.Cursor)
                ? HexToColor(theme.Colors.Cursor)
                : (isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black);
            var cursorBrush = new SolidColorBrush(cursorColor);

            // Solid opaque background ensures DirectWrite DestinationInvert generates
            // a crisp black caret on light themes and crisp white caret on dark themes!
            EditBox.Background = bgBrush;
            EditBox.Foreground = fgBrush;
            EditBox.SelectionHighlightColor = selBrush;

            RootGrid.Background = bgBrush;

            // Lock all background brushes in EditBox resources to theme background
            EditBox.Resources["TextControlBackground"] = bgBrush;
            EditBox.Resources["TextControlBackgroundPointerOver"] = bgBrush;
            EditBox.Resources["TextControlBackgroundFocused"] = bgBrush;
            EditBox.Resources["TextControlBackgroundDisabled"] = bgBrush;

            // Completely eliminate hover/focus color shift: lock all foreground brushes to the theme foreground!
            EditBox.Resources["TextControlForeground"] = fgBrush;
            EditBox.Resources["TextControlForegroundPointerOver"] = fgBrush;
            EditBox.Resources["TextControlForegroundFocused"] = fgBrush;

            // Set caret / IBeam brushes for clear visibility in both light and dark themes
            EditBox.Resources["TextControlIBeamColor"] = cursorBrush;
            EditBox.Resources["TextControlIBeamColorFocused"] = cursorBrush;
            EditBox.Resources["TextControlIBeamColorPointerOver"] = cursorBrush;

            // Recolor all existing text in the document to the theme foreground
            var doc = EditBox.Document;
            doc.GetText(TextGetOptions.None, out var text);
            if (!string.IsNullOrEmpty(text))
            {
                var fullRange = doc.GetRange(0, text.Length);
                fullRange.CharacterFormat.ForegroundColor = fgColor;
            }
            doc.Selection.CharacterFormat.ForegroundColor = fgColor;

            HighlightMarkdownTokens();
        }
        catch { }
    }

    public static Windows.UI.Color HexToColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Microsoft.UI.Colors.Transparent;
        if (hex.StartsWith("#")) hex = hex[1..];
        byte a = 255;
        int idx = 0;
        if (hex.Length == 8)
        {
            a = Convert.ToByte(hex[..2], 16);
            idx = 2;
        }
        byte r = Convert.ToByte(hex.Substring(idx, 2), 16);
        byte g = Convert.ToByte(hex.Substring(idx + 2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(idx + 4, 2), 16);
        return Windows.UI.Color.FromArgb(a, r, g, b);
    }

    public DocumentTabViewModel? TabViewModel
    {
        get => (DocumentTabViewModel?)GetValue(TabViewModelProperty);
        set => SetValue(TabViewModelProperty, value);
    }

    private static void OnTabViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EditorHost host)
        {
            if (e.OldValue is DocumentTabViewModel oldVm)
            {
                host.SaveDocumentToViewModel(oldVm);
                oldVm.PropertyChanged -= host.OnVmPropertyChanged;
            }

            if (e.NewValue is DocumentTabViewModel newVm)
            {
                newVm.PropertyChanged += host.OnVmPropertyChanged;
                host.LoadDocumentFromViewModel(newVm);
                host.DispatcherQueue.TryEnqueue(() => host.FocusEditor());
            }
        }
    }

    public void SaveDocumentToViewModel(DocumentTabViewModel? vm)
    {
        if (vm is null) return;

        if (vm.IsVisualWysiwyg)
        {
            EditBox.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
            vm.RtfContent = rtf;
            var markdown = ExportRichEditToMarkdown();
            if (!string.Equals(vm.Content, markdown, StringComparison.Ordinal))
            {
                vm.Content = markdown;
            }
        }
        else
        {
            EditBox.Document.GetText(TextGetOptions.None, out var text);
            if (text.EndsWith("\r", StringComparison.Ordinal))
            {
                text = text[..^1];
            }
            vm.RtfContent = null;
            if (!string.Equals(vm.Content, text, StringComparison.Ordinal))
            {
                vm.Content = text;
            }
        }
    }

    public void FocusEditor(bool placeCaretAtEnd = false)
    {
        EditBox.Focus(FocusState.Programmatic);
        if (placeCaretAtEnd)
        {
            EditBox.Document.GetText(TextGetOptions.None, out var text);
            int pos = text.Length;
            if (text.EndsWith("\r", StringComparison.Ordinal))
            {
                pos--;
            }
            pos = Math.Max(0, pos);
            EditBox.Document.Selection.SetRange(pos, pos);
        }
    }

    private void OnEditBoxLoaded(object sender, RoutedEventArgs e)
    {
        EditBox.SelectionFlyout = null;
        if (TabViewModel is not null)
        {
            LoadDocumentFromViewModel(TabViewModel);
        }
        DispatcherQueue.TryEnqueue(() => FocusEditor());
    }

    private void LoadDocumentFromViewModel(DocumentTabViewModel vm)
    {
        _isUpdatingInternally = true;
        try
        {
            if (vm.IsVisualWysiwyg && !string.IsNullOrEmpty(vm.RtfContent))
            {
                EditBox.Document.SetText(TextSetOptions.FormatRtf, vm.RtfContent);
            }
            else
            {
                EditBox.Document.SetText(TextSetOptions.None, vm.Content ?? string.Empty);
                if (vm.IsVisualWysiwyg && !string.IsNullOrEmpty(vm.Content))
                {
                    RenderMarkdownToVisualWysiwyg();
                    EditBox.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
                    vm.RtfContent = rtf;
                }
            }

            EditBox.TextWrapping = vm.IsWordWrapEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ApplyZoom(vm.ZoomLevel);
            ApplyTheme(Theme ?? AppServices.ThemeService.CurrentEditorTheme);
            UpdateCaretPosition();

            if (!vm.IsVisualWysiwyg)
            {
                _highlightTimer.Stop();
                _highlightTimer.Start();
            }
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    public RichEditBox EditBoxControl => EditBox;

    public void Undo() => EditBox.Document.Undo();
    public void Redo() => EditBox.Document.Redo();
    public void Cut() => EditBox.Document.Selection.Cut();
    public void Copy() => EditBox.Document.Selection.Copy();
    public void Paste() => EditBox.Document.Selection.Paste(0);
    public void SelectAll() => EditBox.Document.Selection.SetRange(0, int.MaxValue);

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not DocumentTabViewModel vm || _isUpdatingInternally)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(DocumentTabViewModel.IsWordWrapEnabled):
                EditBox.TextWrapping = vm.IsWordWrapEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;
                break;
            case nameof(DocumentTabViewModel.ZoomLevel):
                ApplyZoom(vm.ZoomLevel);
                break;
            case nameof(DocumentTabViewModel.FormattingMode):
                if (vm.IsVisualWysiwyg)
                {
                    RenderMarkdownToVisualWysiwyg();
                    EditBox.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
                    vm.RtfContent = rtf;
                }
                else
                {
                    var md = ExportRichEditToMarkdown();
                    vm.Content = md;
                    EditBox.Document.SetText(TextSetOptions.None, md);
                    HighlightMarkdownTokens();
                }
                break;
            case nameof(DocumentTabViewModel.Content):
                _isUpdatingInternally = true;
                try
                {
                    EditBox.Document.SetText(TextSetOptions.None, vm.Content ?? string.Empty);
                    UpdateCaretPosition();
                }
                finally
                {
                    _isUpdatingInternally = false;
                }
                break;
        }
    }

    private void ApplyZoom(int zoomLevel)
    {
        var scale = Math.Clamp(zoomLevel, 10, 500) / 100.0;
        EditBox.FontSize = Math.Max(6.0, BaseFontSize * scale);
    }

    private static string NormalizeToCr(string? str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        return str.Replace("\r\n", "\r").Replace("\n", "\r");
    }

    private void OnTextChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingInternally || TabViewModel is null)
        {
            return;
        }

        SyncContentToViewModel();

        UpdateCaretPosition();
        if (!IsVisualWysiwyg)
        {
            _highlightTimer.Stop();
            _highlightTimer.Start();
        }
    }

    public void SyncContentToViewModel()
    {
        if (TabViewModel is null) return;

        _isUpdatingInternally = true;
        try
        {
            if (IsVisualWysiwyg)
            {
                EditBox.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
                TabViewModel.RtfContent = rtf;
                var markdown = ExportRichEditToMarkdown();
                if (!string.Equals(TabViewModel.Content, markdown, StringComparison.Ordinal))
                {
                    TabViewModel.Content = markdown;
                    TabViewModel.IsModified = true;
                }
            }
            else
            {
                EditBox.Document.GetText(TextGetOptions.None, out var text);
                if (text.EndsWith("\r", StringComparison.Ordinal))
                {
                    text = text[..^1];
                }
                TabViewModel.RtfContent = null;
                if (!string.Equals(TabViewModel.Content, text, StringComparison.Ordinal))
                {
                    TabViewModel.Content = text;
                    TabViewModel.IsModified = true;
                }
            }
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingInternally)
        {
            return;
        }

        UpdateCaretPosition();
    }

    private void UpdateCaretPosition()
    {
        if (TabViewModel is null)
        {
            return;
        }

        var selection = EditBox.Document.Selection;
        int caretPos = selection.StartPosition;

        EditBox.Document.GetText(TextGetOptions.None, out var fullText);
        int pos = Math.Clamp(caretPos, 0, fullText.Length);

        int line = 1;
        int col = 1;

        for (int i = 0; i < pos; i++)
        {
            char c = fullText[i];
            if (c == '\r' || c == '\n')
            {
                line++;
                col = 1;
                if (c == '\r' && i + 1 < pos && fullText[i + 1] == '\n')
                {
                    i++;
                }
            }
            else
            {
                col++;
            }
        }

        TabViewModel.CaretLine = line;
        TabViewModel.CaretColumn = col;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var isCtrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        if (isCtrl && TabViewModel is not null)
        {
            var pointerPoint = e.GetCurrentPoint(EditBox);
            int mouseDelta = pointerPoint.Properties.MouseWheelDelta;

            if (mouseDelta > 0)
            {
                TabViewModel.ZoomLevel += 10;
                e.Handled = true;
            }
            else if (mouseDelta < 0)
            {
                TabViewModel.ZoomLevel -= 10;
                e.Handled = true;
            }
        }
    }

    private async void OnEditBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var isCtrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        if (isCtrl)
        {
            // Ctrl+B: Bold
            if (e.Key == VirtualKey.B)
            {
                e.Handled = true;
                InsertBold();
                return;
            }

            // Ctrl+I: Italic
            if (e.Key == VirtualKey.I)
            {
                e.Handled = true;
                InsertItalic();
                return;
            }

            // Ctrl+U: Underline
            if (e.Key == VirtualKey.U)
            {
                e.Handled = true;
                InsertUnderline();
                return;
            }

            // Ctrl+K: Markdown Link ([text](https://))
            if (e.Key == VirtualKey.K)
            {
                e.Handled = true;
                InsertLink();
                return;
            }

            // Enforce plain-text paste on Ctrl+V
            if (e.Key == VirtualKey.V)
            {
                var package = Clipboard.GetContent();
                if (package.Contains(StandardDataFormats.Text))
                {
                    e.Handled = true;
                    var plainText = await package.GetTextAsync();
                    if (!string.IsNullOrEmpty(plainText))
                    {
                        EditBox.Document.Selection.SetText(TextSetOptions.None, plainText);
                    }
                }
            }
        }
    }

    private void ToggleMarkdownWrap(string marker)
    {
        var selection = EditBox.Document.Selection;
        var selectedText = selection.Text;

        if (string.IsNullOrEmpty(selectedText))
        {
            // No selection: insert marker + marker (e.g. ****) and position caret inside (**|**)
            int start = selection.StartPosition;
            selection.SetText(TextSetOptions.None, marker + marker);
            selection.SetRange(start + marker.Length, start + marker.Length);
        }
        else
        {
            // If already wrapped in marker, unwrap it (toggle off)
            if (selectedText.StartsWith(marker, StringComparison.Ordinal) &&
                selectedText.EndsWith(marker, StringComparison.Ordinal) &&
                selectedText.Length >= marker.Length * 2)
            {
                var unwrapped = selectedText.Substring(marker.Length, selectedText.Length - marker.Length * 2);
                int start = selection.StartPosition;
                selection.SetText(TextSetOptions.None, unwrapped);
                selection.SetRange(start, start + unwrapped.Length);
            }
            else
            {
                // Wrap selected text: **selected**
                int start = selection.StartPosition;
                var wrapped = marker + selectedText + marker;
                selection.SetText(TextSetOptions.None, wrapped);
                selection.SetRange(start, start + wrapped.Length);
            }
        }

        HighlightMarkdownTokens();
    }

    private void InsertMarkdownLink()
    {
        var selection = EditBox.Document.Selection;
        var selectedText = selection.Text;

        if (string.IsNullOrEmpty(selectedText))
        {
            int start = selection.StartPosition;
            selection.SetText(TextSetOptions.None, "[link text](https://)");
            selection.SetRange(start + 1, start + 10);
        }
        else
        {
            int start = selection.StartPosition;
            var wrapped = $"[{selectedText}](https://)";
            selection.SetText(TextSetOptions.None, wrapped);
            selection.SetRange(start + wrapped.Length - 9, start + wrapped.Length - 1);
        }

        HighlightMarkdownTokens();
    }

    public void ToggleLinePrefix(string prefix)
    {
        var doc = EditBox.Document;
        var selection = doc.Selection;
        int selStart = selection.StartPosition;
        int selEnd = selection.EndPosition;

        var lineRange = selection.GetClone();
        lineRange.Expand(TextRangeUnit.Line);
        int lineStart = lineRange.StartPosition;

        lineRange.GetText(TextGetOptions.None, out var lineText);
        if (lineText == null) return;

        // Check if line already has any Markdown prefix:
        // Prefixes: "#{1,6} ", "> ", "- [ ] ", "- [x] ", "[-*+] ", "\d+\. "
        var prefixMatch = Regex.Match(lineText, @"^(#{1,6}\s+|>\s*|- \[[ xX]\]\s*|[-*+]\s+|\d+\.\s+)");

        doc.BeginUndoGroup();
        try
        {
            if (prefixMatch.Success)
            {
                var existingPrefix = prefixMatch.Value;
                if (string.Equals(existingPrefix, prefix, StringComparison.Ordinal))
                {
                    // Exact same prefix: remove it (toggle off)
                    var removeRange = doc.GetRange(lineStart, lineStart + existingPrefix.Length);
                    removeRange.SetText(TextSetOptions.None, string.Empty);
                    int offset = existingPrefix.Length;
                    doc.Selection.SetRange(Math.Max(lineStart, selStart - offset), Math.Max(lineStart, selEnd - offset));
                }
                else
                {
                    // Different prefix (e.g. user had "# " and clicked "## "): replace it cleanly
                    var replaceRange = doc.GetRange(lineStart, lineStart + existingPrefix.Length);
                    replaceRange.SetText(TextSetOptions.None, prefix);
                    int diff = prefix.Length - existingPrefix.Length;
                    doc.Selection.SetRange(Math.Max(lineStart, selStart + diff), Math.Max(lineStart, selEnd + diff));
                }
            }
            else
            {
                // No existing prefix: insert prefix at lineStart
                var insertRange = doc.GetRange(lineStart, lineStart);
                insertRange.SetText(TextSetOptions.None, prefix);
                doc.Selection.SetRange(selStart + prefix.Length, selEnd + prefix.Length);
            }
        }
        finally
        {
            doc.EndUndoGroup();
        }

        HighlightMarkdownTokens();
    }

    public void InsertCodeBlock()
    {
        var doc = EditBox.Document;
        var selection = doc.Selection;
        var text = selection.Text;

        if (string.IsNullOrEmpty(text))
        {
            int start = selection.StartPosition;
            selection.SetText(TextSetOptions.None, "```\n\n```");
            selection.SetRange(start + 4, start + 4);
        }
        else
        {
            int start = selection.StartPosition;
            var block = "```\n" + text + "\n```";
            selection.SetText(TextSetOptions.None, block);
            selection.SetRange(start, start + block.Length);
        }

        HighlightMarkdownTokens();
    }

    private bool IsVisualWysiwyg => TabViewModel is null || TabViewModel.IsVisualWysiwyg;

    // Context menu & formatting public methods
    public void InsertBold()
    {
        if (IsVisualWysiwyg)
            ToggleVisualBold();
        else
            ToggleMarkdownWrap("**");
    }

    public void InsertItalic()
    {
        if (IsVisualWysiwyg)
            ToggleVisualItalic();
        else
            ToggleMarkdownWrap("*");
    }

    public void InsertUnderline()
    {
        var selection = EditBox.Document.Selection;
        var current = selection.CharacterFormat.Underline;
        selection.CharacterFormat.Underline = (current == UnderlineType.Single) ? UnderlineType.None : UnderlineType.Single;
        SyncContentToViewModel();
    }

    public void InsertStrikethrough()
    {
        if (IsVisualWysiwyg)
        {
            var selection = EditBox.Document.Selection;
            var current = selection.CharacterFormat.Strikethrough;
            selection.CharacterFormat.Strikethrough = (current == FormatEffect.On) ? FormatEffect.Off : FormatEffect.On;
            SyncContentToViewModel();
        }
        else
        {
            ToggleMarkdownWrap("~~");
        }
    }

    public void InsertInlineCode()
    {
        if (IsVisualWysiwyg)
        {
            var selection = EditBox.Document.Selection;
            selection.CharacterFormat.Name = "Consolas";
            if (Theme != null)
            {
                selection.CharacterFormat.ForegroundColor = HexToColor(Theme.Colors.String);
            }
            SyncContentToViewModel();
        }
        else
        {
            ToggleMarkdownWrap("`");
        }
    }

    public void InsertLink() => InsertMarkdownLink();

    public void InsertH1()
    {
        if (IsVisualWysiwyg)
            ApplyVisualHeading(1.6f);
        else
            ToggleLinePrefix("# ");
    }

    public void InsertH2()
    {
        if (IsVisualWysiwyg)
            ApplyVisualHeading(1.3f);
        else
            ToggleLinePrefix("## ");
    }

    public void InsertH3()
    {
        if (IsVisualWysiwyg)
            ApplyVisualHeading(1.15f);
        else
            ToggleLinePrefix("### ");
    }

    public void InsertQuote()
    {
        if (IsVisualWysiwyg)
            ApplyVisualQuote();
        else
            ToggleLinePrefix("> ");
    }

    public void InsertBulletList()
    {
        if (IsVisualWysiwyg)
            ToggleVisualList(MarkerType.Bullet);
        else
            ToggleLinePrefix("- ");
    }

    public void InsertNumberedList()
    {
        if (IsVisualWysiwyg)
            ToggleVisualList(MarkerType.Arabic);
        else
            ToggleLinePrefix("1. ");
    }

    public void InsertTaskList() => ToggleLinePrefix("- [ ] ");

    private void ToggleVisualBold()
    {
        var selection = EditBox.Document.Selection;
        var current = selection.CharacterFormat.Bold;
        selection.CharacterFormat.Bold = (current == FormatEffect.On) ? FormatEffect.Off : FormatEffect.On;
        SyncContentToViewModel();
    }

    private void ToggleVisualItalic()
    {
        var selection = EditBox.Document.Selection;
        var current = selection.CharacterFormat.Italic;
        selection.CharacterFormat.Italic = (current == FormatEffect.On) ? FormatEffect.Off : FormatEffect.On;
        SyncContentToViewModel();
    }

    private void ApplyVisualHeading(float scale)
    {
        var doc = EditBox.Document;
        var selection = doc.Selection;
        var lineRange = selection.GetClone();
        lineRange.Expand(TextRangeUnit.Line);

        doc.BeginUndoGroup();
        try
        {
            float targetSize = (float)(BaseFontSize * scale);
            if (Math.Abs(lineRange.CharacterFormat.Size - targetSize) < 0.5f)
            {
                lineRange.CharacterFormat.Size = (float)BaseFontSize;
                lineRange.CharacterFormat.Bold = FormatEffect.Off;
                if (Theme != null)
                {
                    var baseFg = HexToColor(Theme.Colors.Foreground);
                    lineRange.CharacterFormat.ForegroundColor = baseFg;
                }
            }
            else
            {
                lineRange.CharacterFormat.Size = targetSize;
                lineRange.CharacterFormat.Bold = FormatEffect.On;
                if (Theme != null)
                {
                    lineRange.CharacterFormat.ForegroundColor = HexToColor(Theme.Colors.Heading);
                }
            }
        }
        finally
        {
            doc.EndUndoGroup();
        }

        SyncContentToViewModel();
    }

    private void ToggleVisualList(MarkerType marker)
    {
        var selection = EditBox.Document.Selection;
        var current = selection.ParagraphFormat.ListType;
        selection.ParagraphFormat.ListType = (current == marker) ? MarkerType.None : marker;
        SyncContentToViewModel();
    }

    private void ApplyVisualQuote()
    {
        var selection = EditBox.Document.Selection;
        selection.CharacterFormat.Italic = FormatEffect.On;
        if (Theme != null)
        {
            selection.CharacterFormat.ForegroundColor = HexToColor(Theme.Colors.Comment);
        }
        SyncContentToViewModel();
    }

    // Context Menu Event Handlers
    private void OnCutContextClick(object sender, RoutedEventArgs e) => Cut();
    private void OnCopyContextClick(object sender, RoutedEventArgs e) => Copy();
    private async void OnPasteContextClick(object sender, RoutedEventArgs e)
    {
        var package = Clipboard.GetContent();
        if (package.Contains(StandardDataFormats.Text))
        {
            var plainText = await package.GetTextAsync();
            if (!string.IsNullOrEmpty(plainText))
            {
                EditBox.Document.Selection.SetText(TextSetOptions.None, plainText);
            }
        }
    }
    private void OnSelectAllContextClick(object sender, RoutedEventArgs e) => SelectAll();

    private void OnBoldClick(object sender, RoutedEventArgs e) => InsertBold();
    private void OnItalicClick(object sender, RoutedEventArgs e) => InsertItalic();
    private void OnUnderlineClick(object sender, RoutedEventArgs e) => InsertUnderline();
    private void OnStrikethroughClick(object sender, RoutedEventArgs e) => InsertStrikethrough();
    private void OnInlineCodeClick(object sender, RoutedEventArgs e) => InsertInlineCode();
    private void OnLinkClick(object sender, RoutedEventArgs e) => InsertLink();

    private void OnH1Click(object sender, RoutedEventArgs e) => InsertH1();
    private void OnH2Click(object sender, RoutedEventArgs e) => InsertH2();
    private void OnH3Click(object sender, RoutedEventArgs e) => InsertH3();
    private void OnQuoteClick(object sender, RoutedEventArgs e) => InsertQuote();
    private void OnBulletListClick(object sender, RoutedEventArgs e) => InsertBulletList();
    private void OnNumberedListClick(object sender, RoutedEventArgs e) => InsertNumberedList();
    private void OnTaskListClick(object sender, RoutedEventArgs e) => InsertTaskList();
    private void OnCodeBlockClick(object sender, RoutedEventArgs e) => InsertCodeBlock();

    private void HighlightMarkdownTokens()
    {
        if (TabViewModel is null || IsVisualWysiwyg)
        {
            return;
        }

        var doc = EditBox.Document;
        doc.GetText(TextGetOptions.None, out var text);
        if (string.IsNullOrEmpty(text) || text.Length > 1_000_000) return;

        bool isDark = Theme is null || !string.Equals(Theme.Appearance, "light", StringComparison.OrdinalIgnoreCase);
        var baseFg = Theme is not null ? HexToColor(Theme.Colors.Foreground) : (isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black);

        // Check if markdown by file extension or token presence in actual text
        bool isMd = TabViewModel.Kind == DocumentKind.Markdown ||
                    TabViewModel.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                    TabViewModel.FileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(text, @"(?<=^|[\r\n])(#{1,6}[ \t]+|[-*+][ \t]+|\d+\.[ \t]+|>|```)|(\*\*|__)[^\r\n]+?\2|`[^`\r\n]+`|~~[^~\r\n]+?~~");

        if (!isMd)
        {
            var fullPlainRange = doc.GetRange(0, text.Length);
            fullPlainRange.CharacterFormat.ForegroundColor = baseFg;
            fullPlainRange.CharacterFormat.Bold = FormatEffect.Off;
            fullPlainRange.CharacterFormat.Italic = FormatEffect.Off;
            fullPlainRange.CharacterFormat.Strikethrough = FormatEffect.Off;
            fullPlainRange.CharacterFormat.Underline = UnderlineType.None;
            return;
        }

        var headingColor = Theme is not null ? HexToColor(Theme.Colors.Heading) : Windows.UI.Color.FromArgb(255, 78, 201, 176);
        var boldColor = Theme is not null ? HexToColor(Theme.Colors.Keyword) : Windows.UI.Color.FromArgb(255, 86, 156, 214);
        var codeColor = Theme is not null ? HexToColor(Theme.Colors.String) : Windows.UI.Color.FromArgb(255, 209, 154, 102);
        var commentColor = Theme is not null ? HexToColor(Theme.Colors.Comment) : Windows.UI.Color.FromArgb(255, 106, 153, 85);
        var linkColor = Theme is not null ? HexToColor(Theme.Colors.Link) : Windows.UI.Color.FromArgb(255, 55, 148, 255);

        doc.BeginUndoGroup();
        try
        {
            // Reset formatting on the full document to default base foreground
            var fullRange = doc.GetRange(0, text.Length);
            fullRange.CharacterFormat.Bold = FormatEffect.Off;
            fullRange.CharacterFormat.Italic = FormatEffect.Off;
            fullRange.CharacterFormat.Strikethrough = FormatEffect.Off;
            fullRange.CharacterFormat.Underline = UnderlineType.None;
            fullRange.CharacterFormat.ForegroundColor = baseFg;

            // Highlight Markdown Headers (# Header) - handles start of string and CR/LF
            var headerMatches = Regex.Matches(text, @"(?<=^|[\r\n])(#{1,6}[ \t]+[^\r\n]*)");
            foreach (Match m in headerMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Bold = FormatEffect.On;
                r.CharacterFormat.ForegroundColor = headingColor;
            }

            // Highlight Blockquotes (> Quote)
            var quoteMatches = Regex.Matches(text, @"(?<=^|[\r\n])(>[ \t]+[^\r\n]*)");
            foreach (Match m in quoteMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Italic = FormatEffect.On;
                r.CharacterFormat.ForegroundColor = commentColor;
            }

            // Highlight Lists (- item, * item, 1. item, - [ ] item)
            var listMatches = Regex.Matches(text, @"(?<=^|[\r\n])([ \t]*(- \[[ xX]\]|[-*+]|\d+\.)[ \t]+)");
            foreach (Match m in listMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.ForegroundColor = headingColor;
                r.CharacterFormat.Bold = FormatEffect.On;
            }

            // Highlight Code Blocks (``` ... ```)
            var codeBlockMatches = Regex.Matches(text, @"```[\s\S]*?```");
            foreach (Match m in codeBlockMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.ForegroundColor = codeColor;
            }

            // Highlight Bold (**text** or __text__)
            var boldMatches = Regex.Matches(text, @"(\*\*|__)([^\r\n]+?)\1");
            foreach (Match m in boldMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Bold = FormatEffect.On;
                r.CharacterFormat.ForegroundColor = boldColor;
            }

            // Highlight Italic (*text* or _text_)
            var italicMatches = Regex.Matches(text, @"(?<!\*)\*([^*\r\n]+?)\*(?!\*)|(?<!_)_([^_\r\n]+?)_(?!_)");
            foreach (Match m in italicMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Italic = FormatEffect.On;
            }

            // Highlight Strikethrough (~~text~~)
            var strikeMatches = Regex.Matches(text, @"~~([^~\r\n]+?)~~");
            foreach (Match m in strikeMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Strikethrough = FormatEffect.On;
            }

            // Highlight Inline Code (`code`)
            var codeMatches = Regex.Matches(text, @"`([^`\r\n]+?)`");
            foreach (Match m in codeMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.ForegroundColor = codeColor;
            }

            // Highlight Links ([text](https://...))
            var linkMatches = Regex.Matches(text, @"\[([^\]\r\n]+)\]\(([^)\r\n]+)\)");
            foreach (Match m in linkMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.ForegroundColor = linkColor;
                r.CharacterFormat.Underline = UnderlineType.Single;
            }
        }
        catch { }
        finally
        {
            doc.EndUndoGroup();
        }
    }

    public string ExportRichEditToMarkdown()
    {
        var doc = EditBox.Document;
        doc.GetText(TextGetOptions.None, out var fullText);
        if (string.IsNullOrEmpty(fullText))
        {
            return string.Empty;
        }

        if (fullText.EndsWith("\r", StringComparison.Ordinal))
        {
            fullText = fullText[..^1];
        }

        var sb = new StringBuilder();
        int charIndex = 0;
        int textLength = fullText.Length;

        while (charIndex < textLength)
        {
            int lineEnd = fullText.IndexOfAny(new[] { '\r', '\n' }, charIndex);
            if (lineEnd == -1)
            {
                lineEnd = textLength;
            }

            int lineLen = lineEnd - charIndex;
            if (lineLen == 0)
            {
                sb.AppendLine();
            }
            else
            {
                var lineRange = doc.GetRange(charIndex, lineEnd);
                var prefix = string.Empty;

                if (lineRange.ParagraphFormat.ListType == MarkerType.Bullet)
                {
                    prefix = "- ";
                }
                else if (lineRange.ParagraphFormat.ListType == MarkerType.Arabic)
                {
                    prefix = "1. ";
                }
                else
                {
                    float size = lineRange.CharacterFormat.Size;
                    if (size >= 21.0f)
                    {
                        prefix = "# ";
                    }
                    else if (size >= 17.5f)
                    {
                        prefix = "## ";
                    }
                    else if (size >= 15.5f)
                    {
                        prefix = "### ";
                    }
                }

                sb.Append(prefix);

                bool isHeading = !string.IsNullOrEmpty(prefix) && prefix.StartsWith("#", StringComparison.Ordinal);
                bool hasInlineStyles = lineRange.CharacterFormat.Bold != FormatEffect.Off ||
                                       lineRange.CharacterFormat.Italic != FormatEffect.Off ||
                                       lineRange.CharacterFormat.Strikethrough != FormatEffect.Off ||
                                       string.Equals(lineRange.CharacterFormat.Name, "Consolas", StringComparison.OrdinalIgnoreCase);

                if (!hasInlineStyles || isHeading)
                {
                    sb.Append(fullText.Substring(charIndex, lineLen));
                }
                else
                {
                    bool inBold = false;
                    bool inItalic = false;
                    bool inStrike = false;
                    bool inCode = false;

                    for (int i = charIndex; i < lineEnd; i++)
                    {
                        char c = fullText[i];
                        var r = doc.GetRange(i, i + 1);

                        bool isBold = r.CharacterFormat.Bold == FormatEffect.On;
                        bool isItalic = r.CharacterFormat.Italic == FormatEffect.On;
                        bool isStrike = r.CharacterFormat.Strikethrough == FormatEffect.On;
                        bool isCode = string.Equals(r.CharacterFormat.Name, "Consolas", StringComparison.OrdinalIgnoreCase);

                        if (inCode && !isCode) { sb.Append('`'); inCode = false; }
                        if (inStrike && !isStrike) { sb.Append("~~"); inStrike = false; }
                        if (inItalic && !isItalic) { sb.Append('*'); inItalic = false; }
                        if (inBold && !isBold) { sb.Append("**"); inBold = false; }

                        if (!inBold && isBold && c != ' ') { sb.Append("**"); inBold = true; }
                        if (!inItalic && isItalic && c != ' ') { sb.Append('*'); inItalic = true; }
                        if (!inStrike && isStrike && c != ' ') { sb.Append("~~"); inStrike = true; }
                        if (!inCode && isCode && c != ' ') { sb.Append('`'); inCode = true; }

                        sb.Append(c);
                    }

                    if (inCode) sb.Append('`');
                    if (inStrike) sb.Append("~~");
                    if (inItalic) sb.Append('*');
                    if (inBold) sb.Append("**");
                }

                sb.AppendLine();
            }

            if (lineEnd < textLength && fullText[lineEnd] == '\r') lineEnd++;
            if (lineEnd < textLength && fullText[lineEnd] == '\n') lineEnd++;
            charIndex = lineEnd;
        }

        var result = sb.ToString();
        if (result.EndsWith("\r\n", StringComparison.Ordinal))
        {
            result = result[..^2];
        }
        else if (result.EndsWith('\n') || result.EndsWith('\r'))
        {
            result = result[..^1];
        }

        return result;
    }

    public void RenderMarkdownToVisualWysiwyg()
    {
        var doc = EditBox.Document;
        doc.GetText(TextGetOptions.None, out var text);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        doc.BeginUndoGroup();
        try
        {
            var headingColor = Theme is not null ? HexToColor(Theme.Colors.Heading) : Windows.UI.Color.FromArgb(255, 78, 201, 176);
            var codeColor = Theme is not null ? HexToColor(Theme.Colors.String) : Windows.UI.Color.FromArgb(255, 209, 154, 102);

            // 1. Bold: (**text** or __text__)
            var boldMatches = Regex.Matches(text, @"(\*\*|__)([^\r\n]+?)\1");
            for (int i = boldMatches.Count - 1; i >= 0; i--)
            {
                var m = boldMatches[i];
                var inner = m.Groups[2].Value;
                var range = doc.GetRange(m.Index, m.Index + m.Length);
                range.SetText(TextSetOptions.None, inner);
                var formatted = doc.GetRange(m.Index, m.Index + inner.Length);
                formatted.CharacterFormat.Bold = FormatEffect.On;
            }

            doc.GetText(TextGetOptions.None, out text);

            // 2. Italic: (*text* or _text_)
            var italicMatches = Regex.Matches(text, @"(?<!\*)\*([^*\r\n]+?)\*(?!\*)|(?<!_)_([^_\r\n]+?)_(?!_)");
            for (int i = italicMatches.Count - 1; i >= 0; i--)
            {
                var m = italicMatches[i];
                var inner = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                var range = doc.GetRange(m.Index, m.Index + m.Length);
                range.SetText(TextSetOptions.None, inner);
                var formatted = doc.GetRange(m.Index, m.Index + inner.Length);
                formatted.CharacterFormat.Italic = FormatEffect.On;
            }

            doc.GetText(TextGetOptions.None, out text);

            // 3. Strikethrough: ~~text~~
            var strikeMatches = Regex.Matches(text, @"~~([^~\r\n]+?)~~");
            for (int i = strikeMatches.Count - 1; i >= 0; i--)
            {
                var m = strikeMatches[i];
                var inner = m.Groups[1].Value;
                var range = doc.GetRange(m.Index, m.Index + m.Length);
                range.SetText(TextSetOptions.None, inner);
                var formatted = doc.GetRange(m.Index, m.Index + inner.Length);
                formatted.CharacterFormat.Strikethrough = FormatEffect.On;
            }

            doc.GetText(TextGetOptions.None, out text);

            // 4. Inline code: `code`
            var codeMatches = Regex.Matches(text, @"`([^`\r\n]+?)`");
            for (int i = codeMatches.Count - 1; i >= 0; i--)
            {
                var m = codeMatches[i];
                var inner = m.Groups[1].Value;
                var range = doc.GetRange(m.Index, m.Index + m.Length);
                range.SetText(TextSetOptions.None, inner);
                var formatted = doc.GetRange(m.Index, m.Index + inner.Length);
                formatted.CharacterFormat.Name = "Consolas";
                formatted.CharacterFormat.ForegroundColor = codeColor;
            }

            doc.GetText(TextGetOptions.None, out text);

            // 5. Headings: (#{1,6}\s+)
            var headingMatches = Regex.Matches(text, @"(?<=^|[\r\n])(#{1,6}[ \t]+)([^\r\n]*)");
            for (int i = headingMatches.Count - 1; i >= 0; i--)
            {
                var m = headingMatches[i];
                int level = m.Groups[1].Value.Trim().Length;
                int prefixLen = m.Groups[1].Length;
                var prefixRange = doc.GetRange(m.Index, m.Index + prefixLen);
                prefixRange.SetText(TextSetOptions.None, string.Empty);

                var lineRange = doc.GetRange(m.Index, m.Index + m.Groups[2].Length);
                float size = level switch
                {
                    1 => (float)(BaseFontSize * 1.6),
                    2 => (float)(BaseFontSize * 1.3),
                    _ => (float)(BaseFontSize * 1.15)
                };
                lineRange.CharacterFormat.Size = size;
                lineRange.CharacterFormat.Bold = FormatEffect.On;
                lineRange.CharacterFormat.ForegroundColor = headingColor;
            }

            doc.GetText(TextGetOptions.None, out text);

            // 6. Bullet lists: ([-*+][ \t]+)
            var bulletMatches = Regex.Matches(text, @"(?<=^|[\r\n])([-*+][ \t]+)([^\r\n]*)");
            for (int i = bulletMatches.Count - 1; i >= 0; i--)
            {
                var m = bulletMatches[i];
                int prefixLen = m.Groups[1].Length;
                var prefixRange = doc.GetRange(m.Index, m.Index + prefixLen);
                prefixRange.SetText(TextSetOptions.None, string.Empty);

                var lineRange = doc.GetRange(m.Index, m.Index + m.Groups[2].Length);
                lineRange.ParagraphFormat.ListType = MarkerType.Bullet;
            }

            doc.GetText(TextGetOptions.None, out text);

            // 7. Numbered lists: (\d+\.[ \t]+)
            var numMatches = Regex.Matches(text, @"(?<=^|[\r\n])(\d+\.[ \t]+)([^\r\n]*)");
            for (int i = numMatches.Count - 1; i >= 0; i--)
            {
                var m = numMatches[i];
                int prefixLen = m.Groups[1].Length;
                var prefixRange = doc.GetRange(m.Index, m.Index + prefixLen);
                prefixRange.SetText(TextSetOptions.None, string.Empty);

                var lineRange = doc.GetRange(m.Index, m.Index + m.Groups[2].Length);
                lineRange.ParagraphFormat.ListType = MarkerType.Arabic;
            }
        }
        catch { }
        finally
        {
            doc.EndUndoGroup();
        }
    }
}
