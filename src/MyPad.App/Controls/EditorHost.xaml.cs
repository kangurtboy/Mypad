using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using MyPad.App.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace MyPad.App.Controls;

public sealed partial class EditorHost : UserControl
{
    private const double BaseFontSize = 14.0;
    private bool _isUpdatingInternally;
    private Storyboard? _caretStoryboard;
    private Storyboard? _blinkStoryboard;
    private DispatcherTimer? _blinkIdleTimer;
    private bool _isFirstPlacement = true;
    private bool _hasEditorFocus;

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

    private void OnEditBoxGotFocus(object sender, RoutedEventArgs e)
    {
        _hasEditorFocus = true;
        SmoothCaret.Opacity = 1;
        UpdateCaretPosition();
    }

    private void OnEditBoxLostFocus(object sender, RoutedEventArgs e)
    {
        _hasEditorFocus = false;
        _caretStoryboard?.Stop();
        _blinkStoryboard?.Stop();
        _blinkIdleTimer?.Stop();
        SmoothCaret.Opacity = 0;
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
        SmoothCaret.Height = Math.Max(14, EditBox.FontSize * 1.3);
    }

    private void OnTextChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingInternally || TabViewModel is null)
        {
            return;
        }

        EditBox.Document.GetText(TextGetOptions.None, out var text);
        // RichEdit appends a trailing '\r' to the text story; strip it if present when comparing
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

        // Smooth Caret Animation (Office 365 style)
        if (_hasEditorFocus && selection.Length == 0)
        {
            try
            {
                selection.GetPoint(
                    HorizontalCharacterAlignment.Left,
                    VerticalCharacterAlignment.Top,
                    PointOptions.ClientCoordinates,
                    out Windows.Foundation.Point pt);

                if (pt.X >= 0 && pt.Y >= 0 && !double.IsInfinity(pt.X) && !double.IsInfinity(pt.Y))
                {
                    AnimateCaretTo(pt.X, pt.Y);
                }
            }
            catch
            {
                SmoothCaret.Opacity = 0;
            }
        }
        else
        {
            SmoothCaret.Opacity = 0;
        }
    }

    private void AnimateCaretTo(double targetX, double targetY)
    {
        SmoothCaret.Height = Math.Max(14, EditBox.FontSize * 1.3);
        SmoothCaret.Opacity = 1;
        _blinkStoryboard?.Stop();

        if (_isFirstPlacement)
        {
            CaretTransform.X = targetX;
            CaretTransform.Y = targetY;
            _isFirstPlacement = false;
            ResetBlinkTimer();
            return;
        }

        _caretStoryboard?.Stop();

        var animX = new DoubleAnimation
        {
            To = targetX,
            Duration = TimeSpan.FromMilliseconds(80),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animX, CaretTransform);
        Storyboard.SetTargetProperty(animX, "X");

        var animY = new DoubleAnimation
        {
            To = targetY,
            Duration = TimeSpan.FromMilliseconds(80),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animY, CaretTransform);
        Storyboard.SetTargetProperty(animY, "Y");

        _caretStoryboard = new Storyboard();
        _caretStoryboard.Children.Add(animX);
        _caretStoryboard.Children.Add(animY);
        _caretStoryboard.Begin();

        ResetBlinkTimer();
    }

    private void ResetBlinkTimer()
    {
        _blinkIdleTimer?.Stop();
        _blinkIdleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _blinkIdleTimer.Tick += (s, e) =>
        {
            _blinkIdleTimer?.Stop();
            StartBlinkAnimation();
        };
        _blinkIdleTimer.Start();
    }

    private void StartBlinkAnimation()
    {
        if (!_hasEditorFocus) return;

        _blinkStoryboard?.Stop();
        var blinkAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(500),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(blinkAnim, SmoothCaret);
        Storyboard.SetTargetProperty(blinkAnim, "Opacity");

        _blinkStoryboard = new Storyboard();
        _blinkStoryboard.Children.Add(blinkAnim);
        _blinkStoryboard.Begin();
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
}
