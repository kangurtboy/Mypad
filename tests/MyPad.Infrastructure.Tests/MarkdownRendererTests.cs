using MyPad.Infrastructure.Markdown;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class MarkdownRendererTests
{
    private readonly MarkdownRenderer _renderer = new();

    [TestMethod]
    public void RenderToHtml_HeadingsAndBold_ShouldRenderCorrectHtml()
    {
        string md = "# Title\n\n**Bold text**";
        string html = _renderer.RenderToHtml(md);

        Assert.Contains("<h1>Title</h1>", html);
        Assert.Contains("<strong>Bold text</strong>", html);
    }

    [TestMethod]
    public void RenderToHtml_Tables_ShouldRenderTableTags()
    {
        string md = "| Col1 | Col2 |\n| --- | --- |\n| Val1 | Val2 |";
        string html = _renderer.RenderToHtml(md);

        Assert.Contains("<table>", html);
        Assert.Contains("<td>Val1</td>", html);
    }

    [TestMethod]
    public void RenderToHtml_RawScriptTags_ShouldBeDisabledForSecurity()
    {
        string md = "Safe text <script>alert('xss')</script>";
        string html = _renderer.RenderToHtml(md);

        // Raw HTML is disabled by security policy
        Assert.DoesNotContain("<script>", html);
    }
}
