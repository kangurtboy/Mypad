using System.Text;
using MyPad.Core.Enums;
using MyPad.Core.Models;

namespace MyPad.Core.Tests;

[TestClass]
public class TextDocumentTests
{
    [TestMethod]
    public void CreateNew_ShouldInitializeWithDefaultValues()
    {
        var doc = TextDocument.CreateNew("Notes.txt", DocumentKind.Markdown);

        Assert.AreNotEqual(Guid.Empty, doc.Id);
        Assert.AreEqual("Notes.txt", doc.Title);
        Assert.AreEqual(string.Empty, doc.Content);
        Assert.AreEqual(DocumentKind.Markdown, doc.Kind);
        Assert.AreEqual(LineEndingMode.Crlf, doc.LineEnding);
        Assert.IsFalse(doc.IsModified);
    }

    [TestMethod]
    public void DocumentProperties_ShouldRoundTripCorrectly()
    {
        var doc = new TextDocument
        {
            FilePath = @"C:\docs\readme.md",
            Title = "readme.md",
            Content = "# Hello World",
            LineEnding = LineEndingMode.Lf,
            Encoding = Encoding.UTF8,
            IsModified = true,
            Kind = DocumentKind.Markdown,
            CursorLine = 5,
            CursorColumn = 10,
            ZoomLevel = 120
        };

        Assert.AreEqual(@"C:\docs\readme.md", doc.FilePath);
        Assert.AreEqual("readme.md", doc.Title);
        Assert.AreEqual("# Hello World", doc.Content);
        Assert.AreEqual(LineEndingMode.Lf, doc.LineEnding);
        Assert.IsTrue(doc.IsModified);
        Assert.AreEqual(DocumentKind.Markdown, doc.Kind);
        Assert.AreEqual(5, doc.CursorLine);
        Assert.AreEqual(10, doc.CursorColumn);
        Assert.AreEqual(120, doc.ZoomLevel);
    }
}
