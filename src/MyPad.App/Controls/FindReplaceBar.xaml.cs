using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MyPad.App.Controls;

public sealed partial class FindReplaceBar : UserControl
{
    public static readonly DependencyProperty TargetEditorProperty =
        DependencyProperty.Register(
            nameof(TargetEditor),
            typeof(EditorHost),
            typeof(FindReplaceBar),
            new PropertyMetadata(null));

    public FindReplaceBar()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;
    }

    public EditorHost? TargetEditor
    {
        get => (EditorHost?)GetValue(TargetEditorProperty);
        set => SetValue(TargetEditorProperty, value);
    }

    public void ShowFind()
    {
        Visibility = Visibility.Visible;
        ReplaceRow.Visibility = Visibility.Collapsed;
        ToggleReplaceButton.IsChecked = false;
        FindTextBox.Focus(FocusState.Programmatic);
        FindTextBox.SelectAll();
        UpdateMatchCount();
    }

    public void ShowReplace()
    {
        Visibility = Visibility.Visible;
        ReplaceRow.Visibility = Visibility.Visible;
        ToggleReplaceButton.IsChecked = true;
        FindTextBox.Focus(FocusState.Programmatic);
        FindTextBox.SelectAll();
        UpdateMatchCount();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        TargetEditor?.Focus(FocusState.Programmatic);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnToggleReplaceClick(object sender, RoutedEventArgs e)
    {
        ReplaceRow.Visibility = ToggleReplaceButton.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnMatchCaseClick(object sender, RoutedEventArgs e)
    {
        UpdateMatchCount();
    }

    private void OnFindTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMatchCount();
    }

    private void OnFindKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var isShift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

            if (isShift)
            {
                FindPrevious();
            }
            else
            {
                FindNext();
            }
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnReplaceKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ReplaceCurrent();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnFindNextClick(object sender, RoutedEventArgs e) => FindNext();
    private void OnFindPrevClick(object sender, RoutedEventArgs e) => FindPrevious();
    private void OnReplaceClick(object sender, RoutedEventArgs e) => ReplaceCurrent();
    private void OnReplaceAllClick(object sender, RoutedEventArgs e) => ReplaceAll();

    private FindOptions CurrentFindOptions =>
        MatchCaseButton.IsChecked == true ? FindOptions.Case : FindOptions.None;

    public bool FindNext()
    {
        if (TargetEditor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            return false;
        }

        var doc = TargetEditor.EditBoxControl.Document;
        var query = FindTextBox.Text;
        var options = CurrentFindOptions;

        doc.GetText(TextGetOptions.None, out var fullText);
        int docLength = fullText.Length;
        int startPos = doc.Selection.EndPosition;

        // Search from current caret to end of document
        var range = doc.GetRange(startPos, docLength);
        int matchLen = range.FindText(query, docLength - startPos, options);

        if (matchLen > 0)
        {
            doc.Selection.SetRange(range.StartPosition, range.EndPosition);
            range.ScrollIntoView(PointOptions.None);
            UpdateMatchCount();
            return true;
        }

        // Wrap around: search from start of document to current position
        if (startPos > 0)
        {
            range = doc.GetRange(0, startPos);
            matchLen = range.FindText(query, startPos, options);
            if (matchLen > 0)
            {
                doc.Selection.SetRange(range.StartPosition, range.EndPosition);
                range.ScrollIntoView(PointOptions.None);
                UpdateMatchCount();
                return true;
            }
        }

        UpdateMatchCount();
        return false;
    }

    public bool FindPrevious()
    {
        if (TargetEditor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            return false;
        }

        var doc = TargetEditor.EditBoxControl.Document;
        var query = FindTextBox.Text;
        var comparison = MatchCaseButton.IsChecked == true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        doc.GetText(TextGetOptions.None, out var text);
        int currentStart = Math.Clamp(doc.Selection.StartPosition, 0, text.Length);

        // Search backward before current selection
        int idx = -1;
        if (currentStart > 0)
        {
            idx = text.LastIndexOf(query, currentStart - 1, comparison);
        }

        // Wrap around backward from end
        if (idx < 0)
        {
            idx = text.LastIndexOf(query, comparison);
        }

        if (idx >= 0)
        {
            var range = doc.GetRange(idx, idx + query.Length);
            doc.Selection.SetRange(range.StartPosition, range.EndPosition);
            range.ScrollIntoView(PointOptions.None);
            UpdateMatchCount();
            return true;
        }

        UpdateMatchCount();
        return false;
    }

    public void ReplaceCurrent()
    {
        if (TargetEditor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            return;
        }

        var doc = TargetEditor.EditBoxControl.Document;
        var query = FindTextBox.Text;
        var comparison = MatchCaseButton.IsChecked == true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        doc.Selection.GetText(TextGetOptions.None, out var selectedText);
        if (string.Equals(selectedText, query, comparison))
        {
            doc.Selection.SetText(TextSetOptions.None, ReplaceTextBox.Text ?? string.Empty);
        }

        FindNext();
    }

    public void ReplaceAll()
    {
        if (TargetEditor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            return;
        }

        var doc = TargetEditor.EditBoxControl.Document;
        var query = FindTextBox.Text;
        var replacement = ReplaceTextBox.Text ?? string.Empty;
        var comparison = MatchCaseButton.IsChecked == true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        doc.GetText(TextGetOptions.None, out var fullText);
        if (string.IsNullOrEmpty(fullText))
        {
            return;
        }

        int count = 0;
        int idx = 0;
        while ((idx = fullText.IndexOf(query, idx, comparison)) >= 0)
        {
            count++;
            idx += query.Length;
        }

        if (count == 0)
        {
            MatchCountText.Text = "0 replaced";
            return;
        }

        doc.BeginUndoGroup();
        try
        {
            int searchStart = 0;
            var range = doc.GetRange(0, 0);

            while (true)
            {
                doc.GetText(TextGetOptions.None, out var currentText);
                int found = currentText.IndexOf(query, searchStart, comparison);
                if (found < 0)
                {
                    break;
                }

                range.SetRange(found, found + query.Length);
                range.SetText(TextSetOptions.None, replacement);
                searchStart = found + replacement.Length;
            }
        }
        finally
        {
            doc.EndUndoGroup();
        }

        MatchCountText.Text = $"{count} replaced";
    }

    private void UpdateMatchCount()
    {
        if (TargetEditor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            MatchCountText.Text = string.Empty;
            return;
        }

        var query = FindTextBox.Text;
        var comparison = MatchCaseButton.IsChecked == true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        TargetEditor.EditBoxControl.Document.GetText(TextGetOptions.None, out var text);
        if (string.IsNullOrEmpty(text))
        {
            MatchCountText.Text = "No results";
            return;
        }

        int count = 0;
        int idx = 0;
        while ((idx = text.IndexOf(query, idx, comparison)) >= 0)
        {
            count++;
            idx += query.Length;
        }

        MatchCountText.Text = count > 0 ? $"{count} matches" : "No results";
    }
}
