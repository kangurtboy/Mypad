using MyPad.Core.Models;

namespace MyPad.Core.Interfaces;

/// <summary>
/// Service managing editor palettes, application theme synchronization, and theme import/export.
/// </summary>
public interface IThemeService
{
    IReadOnlyList<EditorTheme> GetAvailableEditorThemes();
    EditorTheme CurrentEditorTheme { get; }
    void SetEditorTheme(string themeName);
    EditorTheme ImportTheme(string jsonContent);
    string ExportTheme(EditorTheme theme);
    bool ValidateTheme(string jsonContent, out string? errorMessage);
    event EventHandler<EditorTheme>? ThemeChanged;
}
