using System.Text;
using MyPad.Core.Enums;

namespace MyPad.Core.Models;

public class TextDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string? FilePath { get; set; }

    public string Title { get; set; } = "Untitled";

    public string Content { get; set; } = string.Empty;

    public Encoding Encoding { get; set; } = new UTF8Encoding(false);

    public bool HasBom { get; set; }

    public LineEndingMode LineEnding { get; set; } = LineEndingMode.Crlf;

    public bool IsModified { get; set; }

    public DocumentKind Kind { get; set; } = DocumentKind.PlainText;

    public string? FileFingerprint { get; set; }

    public DateTime? LastSavedUtc { get; set; }

    public int CursorPosition { get; set; }

    public int SelectionLength { get; set; }

    public int CursorLine { get; set; } = 1;

    public int CursorColumn { get; set; } = 1;

    public double VerticalScrollOffset { get; set; }

    public static TextDocument CreateNew(string title = "Untitled", DocumentKind kind = DocumentKind.PlainText)
    {
        return new TextDocument
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = string.Empty,
            Kind = kind,
            LineEnding = LineEndingMode.Crlf,
            Encoding = new UTF8Encoding(false),
            IsModified = false
        };
    }
}
