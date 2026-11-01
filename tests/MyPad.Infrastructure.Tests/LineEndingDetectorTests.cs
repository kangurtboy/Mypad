using MyPad.Core.Enums;
using MyPad.Infrastructure.FileSystem;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class LineEndingDetectorTests
{
    private readonly LineEndingDetector _detector = new();

    [TestMethod]
    public void Detect_Crlf_ShouldDetectCrlf()
    {
        string text = "Line 1\r\nLine 2\r\nLine 3";
        var result = _detector.Detect(text);
        Assert.AreEqual(LineEndingMode.Crlf, result);
    }

    [TestMethod]
    public void Detect_Lf_ShouldDetectLf()
    {
        string text = "Line 1\nLine 2\nLine 3";
        var result = _detector.Detect(text);
        Assert.AreEqual(LineEndingMode.Lf, result);
    }

    [TestMethod]
    public void Detect_Mixed_ShouldDetectMixed()
    {
        string text = "Line 1\r\nLine 2\nLine 3";
        var result = _detector.Detect(text);
        Assert.AreEqual(LineEndingMode.Mixed, result);
    }

    [TestMethod]
    public void NormalizeLineEndings_ToLf_ShouldConvertAllToLf()
    {
        string input = "A\r\nB\rC\nD";
        string normalized = _detector.NormalizeLineEndings(input, LineEndingMode.Lf);
        Assert.AreEqual("A\nB\nC\nD", normalized);
    }

    [TestMethod]
    public void NormalizeLineEndings_ToCrlf_ShouldConvertAllToCrlf()
    {
        string input = "A\r\nB\rC\nD";
        string normalized = _detector.NormalizeLineEndings(input, LineEndingMode.Crlf);
        Assert.AreEqual("A\r\nB\r\nC\r\nD", normalized);
    }
}
