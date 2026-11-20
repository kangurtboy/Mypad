namespace MyPad.Core.Enums;

/// <summary>
/// Specifies the formatting interaction mode in the editor.
/// </summary>
public enum EditorFormattingMode
{
    /// <summary>
    /// Explicit Markdown mode: formatting inserts markup characters like **bold**, *italic*, # heading.
    /// </summary>
    MarkdownSyntax,

    /// <summary>
    /// Visual WYSIWYG mode: formatting directly alters visual styles without showing markup characters in text.
    /// </summary>
    VisualWysiwyg
}
