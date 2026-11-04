using System.Text;
using MyPad.App.Common;
using MyPad.Core.Enums;
using MyPad.Core.Models;

namespace MyPad.App.ViewModels;

/// <summary>
/// ViewModel representing an individual document tab.
/// </summary>
public sealed class DocumentTabViewModel : ObservableObject
{
    private string _content = string.Empty;
    private bool _isModified;
    private string? _filePath;
    private string _fileName = "Untitled";
    private LineEndingMode _lineEnding = LineEndingMode.Crlf;
    private string _encodingName = "UTF-8";
    private int _caretLine = 1;
    private int _caretColumn = 1;
    private int _zoomLevel = 100;
    private bool _isWordWrapEnabled = true;

    public DocumentTabViewModel(TextDocument? document = null)
    {
        if (document is not null)
        {
            Id = document.Id;
            _filePath = document.FilePath;
            _fileName = document.Title;
            _content = document.Content;
            _lineEnding = document.LineEnding;
            _encodingName = document.Encoding?.WebName?.ToUpperInvariant() ?? "UTF-8";
            _isModified = document.IsModified;
            Kind = document.Kind;
        }
        else
        {
            Id = Guid.NewGuid();
            Kind = DocumentKind.PlainText;
        }
    }

    public Guid Id { get; }
    public DocumentKind Kind { get; set; }
    public string? FileFingerprint { get; set; }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                FileName = string.IsNullOrWhiteSpace(value)
                    ? "Untitled"
                    : System.IO.Path.GetFileName(value);
                OnPropertyChanged(nameof(HeaderTitle));
            }
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (SetProperty(ref _fileName, value))
            {
                OnPropertyChanged(nameof(HeaderTitle));
            }
        }
    }

    public string HeaderTitle => IsModified ? $"{FileName} *" : FileName;

    public bool IsMarkdown =>
        Kind == DocumentKind.Markdown ||
        FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                IsModified = true;
            }
        }
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (SetProperty(ref _isModified, value))
            {
                OnPropertyChanged(nameof(HeaderTitle));
            }
        }
    }

    public LineEndingMode LineEnding
    {
        get => _lineEnding;
        set
        {
            if (SetProperty(ref _lineEnding, value))
            {
                OnPropertyChanged(nameof(LineEndingDisplayName));
            }
        }
    }

    public string LineEndingDisplayName => LineEnding switch
    {
        LineEndingMode.Crlf => "Windows (CRLF)",
        LineEndingMode.Lf => "Unix (LF)",
        LineEndingMode.Cr => "Macintosh (CR)",
        _ => "Windows (CRLF)"
    };

    public string EncodingName
    {
        get => _encodingName;
        set => SetProperty(ref _encodingName, value);
    }

    public int CaretLine
    {
        get => _caretLine;
        set
        {
            if (SetProperty(ref _caretLine, value))
            {
                OnPropertyChanged(nameof(PositionDisplayName));
            }
        }
    }

    public int CaretColumn
    {
        get => _caretColumn;
        set
        {
            if (SetProperty(ref _caretColumn, value))
            {
                OnPropertyChanged(nameof(PositionDisplayName));
            }
        }
    }

    public string PositionDisplayName => $"Ln {CaretLine}, Col {CaretColumn}";

    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            var clamped = Math.Clamp(value, 10, 500);
            if (SetProperty(ref _zoomLevel, clamped))
            {
                OnPropertyChanged(nameof(ZoomDisplayName));
            }
        }
    }

    public string ZoomDisplayName => $"{ZoomLevel}%";

    public bool IsWordWrapEnabled
    {
        get => _isWordWrapEnabled;
        set => SetProperty(ref _isWordWrapEnabled, value);
    }

    public void LoadFromDocument(TextDocument doc)
    {
        _filePath = doc.FilePath;
        _fileName = doc.Title;
        _content = doc.Content;
        _lineEnding = doc.LineEnding;
        _encodingName = doc.Encoding?.WebName?.ToUpperInvariant() ?? "UTF-8";
        _isModified = false;
        Kind = doc.Kind;
        FileFingerprint = doc.FileFingerprint;

        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(LineEnding));
        OnPropertyChanged(nameof(LineEndingDisplayName));
        OnPropertyChanged(nameof(EncodingName));
        OnPropertyChanged(nameof(IsModified));
    }

    public void MarkSaved(string savedPath, string? fingerprint = null)
    {
        _filePath = savedPath;
        _fileName = System.IO.Path.GetFileName(savedPath);
        _isModified = false;
        if (fingerprint is not null)
        {
            FileFingerprint = fingerprint;
        }

        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(IsModified));
    }

    public TextDocument ToDomainModel()
    {
        Encoding enc;
        try
        {
            enc = Encoding.GetEncoding(EncodingName);
        }
        catch
        {
            enc = new UTF8Encoding(false);
        }

        return new TextDocument
        {
            Id = Id,
            FilePath = FilePath,
            Title = FileName,
            Content = Content,
            LineEnding = LineEnding,
            Encoding = enc,
            IsModified = IsModified,
            Kind = Kind,
            FileFingerprint = FileFingerprint
        };
    }
}
