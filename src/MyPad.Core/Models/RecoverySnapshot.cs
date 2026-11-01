using MyPad.Core.Enums;

namespace MyPad.Core.Models;

public class RecoverySnapshot
{
    public Guid DocumentId { get; set; }

    public string? OriginalFilePath { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string EncodingWebName { get; set; } = "utf-8";

    public bool HasBom { get; set; }

    public LineEndingMode LineEnding { get; set; } = LineEndingMode.Crlf;

    public DocumentKind Kind { get; set; } = DocumentKind.PlainText;

    public int CursorPosition { get; set; }

    public int SelectionLength { get; set; }

    public double VerticalScrollOffset { get; set; }

    public DateTime SnapshotTimestampUtc { get; set; } = DateTime.UtcNow;

    public string? OriginalFileFingerprint { get; set; }
}
