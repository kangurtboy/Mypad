using MyPad.Core.Enums;

namespace MyPad.Core.Models;

/// <summary>
/// Represents the persisted state of an entire editor session.
/// </summary>
public class SessionState
{
    public int SelectedTabIndex { get; set; }
    public List<SessionTabState> Tabs { get; set; } = new();
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the persisted state of an individual tab in the session.
/// </summary>
public class SessionTabState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? FilePath { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = string.Empty;
    public bool IsModified { get; set; }
    public LineEndingMode LineEnding { get; set; } = LineEndingMode.Crlf;
    public string EncodingName { get; set; } = "UTF-8";
    public int CaretLine { get; set; } = 1;
    public int CaretColumn { get; set; } = 1;
    public int ZoomLevel { get; set; } = 100;
}
