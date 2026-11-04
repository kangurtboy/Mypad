namespace MyPad.Core.Models;

/// <summary>
/// Domain model for an editor theme conforming to the theme JSON schema.
/// </summary>
public class EditorTheme
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Default Dark";
    public string Appearance { get; set; } = "dark"; // "light" or "dark"
    public EditorThemeColors Colors { get; set; } = new();
    public bool IsBuiltIn { get; set; }
}

public class EditorThemeColors
{
    public string Background { get; set; } = "#1e1e1e";
    public string Foreground { get; set; } = "#d4d4d4";
    public string Selection { get; set; } = "#264f78";
    public string Cursor { get; set; } = "#aeafad";
    public string CurrentLine { get; set; } = "#282828";
    public string LineNumber { get; set; } = "#858585";
    public string Keyword { get; set; } = "#569cd6";
    public string String { get; set; } = "#ce9178";
    public string Comment { get; set; } = "#6a9955";
    public string Heading { get; set; } = "#4ec9b0";
    public string Link { get; set; } = "#3794ff";
}
