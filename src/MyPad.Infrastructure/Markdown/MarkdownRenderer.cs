using Markdig;
using MyPad.Core.Interfaces;

namespace MyPad.Infrastructure.Markdown;

public class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .UseEmphasisExtras()
            .UseGridTables()
            .UsePipeTables()
            .UseTaskLists()
            .DisableHtml() // Security: disable raw HTML to prevent script injection
            .Build();
    }

    public string RenderToHtml(string markdownContent)
    {
        if (string.IsNullOrEmpty(markdownContent))
        {
            return string.Empty;
        }

        return Markdig.Markdown.ToHtml(markdownContent, _pipeline);
    }
}
