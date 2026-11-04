using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyPad.App.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace MyPad.App.Controls;

public sealed partial class EditorHost : UserControl
{
    private const double BaseFontSize = 14.0;
    private bool _isUpdatingInternally;
    private readonly DispatcherTimer _highlightTimer;

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

        _highlightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _highlightTimer.Tick += (s, e) =>
        {
            _highlightTimer.Stop();
            HighlightMarkdownTokens();
        };
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
                oldVm.PropertyChanged -= host.OnVmPropertyChanged;
            }

            if (e.NewValue is DocumentTabViewModel newVm)
            {
                newVm.PropertyChanged += host.OnVmPropertyChanged;
                host.LoadDocumentFromViewModel(newVm);
            }
        }
    }

    private void OnEditBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (TabViewModel is not null)
        {
            LoadDocumentFromViewModel(TabViewModel);
        }
    }

    private void LoadDocumentFromViewModel(DocumentTabViewModel vm)
    {
        _isUpdatingInternally = true;
        try
        {
            EditBox.Document.SetText(TextSetOptions.None, vm.Content ?? string.Empty);
            EditBox.TextWrapping = vm.IsWordWrapEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ApplyZoom(vm.ZoomLevel);
            UpdateCaretPosition();
            _highlightTimer.Stop();
            _highlightTimer.Start();
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

    private void OnTextChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingInternally || TabViewModel is null)
        {
            return;
        }

        EditBox.Document.GetText(TextGetOptions.None, out var text);
        if (text.EndsWith("\r", StringComparison.Ordinal))
        {
            text = text[..^1];
        }

        if (string.Equals(text, TabViewModel.Content, StringComparison.Ordinal))
        {
            return;
        }

        _isUpdatingInternally = true;
        try
        {
            TabViewModel.Content = text;
            TabViewModel.IsModified = true;
        }
        finally
        {
            _isUpdatingInternally = false;
        }

        UpdateCaretPosition();
        _highlightTimer.Stop();
        _highlightTimer.Start();
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
        // Enforce plain-text paste on Ctrl+V
        if (e.Key == VirtualKey.V)
        {
            var isCtrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

            if (isCtrl)
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

    private void HighlightMarkdownTokens()
    {
        if (TabViewModel is null || !TabViewModel.IsMarkdown || TabViewModel.Content.Length > 1_000_000)
        {
            return;
        }

        var doc = EditBox.Document;
        doc.BeginUndoGroup();
        try
        {
            doc.GetText(TextGetOptions.None, out var text);
            if (string.IsNullOrEmpty(text)) return;

            // Reset formatting on the full document to default
            var fullRange = doc.GetRange(0, text.Length);
            fullRange.CharacterFormat.Bold = FormatEffect.Off;
            fullRange.CharacterFormat.Italic = FormatEffect.Off;

            // Highlight Markdown Headers (# Header)
            var headerMatches = Regex.Matches(text, @"^(#{1,6}\s+.*)$", RegexOptions.Multiline);
            foreach (Match m in headerMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Bold = FormatEffect.On;
            }

            // Highlight Bold (**text**)
            var boldMatches = Regex.Matches(text, @"\*\*(.+?)\*\*");
            foreach (Match m in boldMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.Bold = FormatEffect.On;
            }

            // Highlight Inline Code (`code`)
            var codeMatches = Regex.Matches(text, @"`([^`]+?)`");
            foreach (Match m in codeMatches)
            {
                var r = doc.GetRange(m.Index, m.Index + m.Length);
                r.CharacterFormat.ForegroundColor = Windows.UI.Color.FromArgb(255, 209, 154, 102);
            }
        }
        catch { }
        finally
        {
            doc.EndUndoGroup();
        }
    }
}
