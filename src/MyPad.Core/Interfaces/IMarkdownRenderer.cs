namespace MyPad.Core.Interfaces;

public interface IMarkdownRenderer
{
    string RenderToHtml(string markdownContent);
}
