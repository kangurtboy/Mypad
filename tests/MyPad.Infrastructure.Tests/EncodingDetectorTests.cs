using System.Text;
using MyPad.Infrastructure.Encoding;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class EncodingDetectorTests
{
    private readonly EncodingDetector _detector = new();

    [TestMethod]
    public void Detect_Utf8Bom_ShouldReturnUtf8WithBom()
    {
        byte[] bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // BOM + "Hello"
        var result = _detector.Detect(bytes, bytes.Length);

        Assert.IsTrue(result.HasBom);
        Assert.AreEqual(System.Text.Encoding.UTF8.WebName, result.Encoding.WebName);
    }

    [TestMethod]
    public void Detect_Utf16LeBom_ShouldReturnUnicodeLe()
    {
        byte[] bytes = new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 }; // BOM + "Hi"
        var result = _detector.Detect(bytes, bytes.Length);

        Assert.IsTrue(result.HasBom);
        Assert.AreEqual(System.Text.Encoding.Unicode.WebName, result.Encoding.WebName);
    }

    [TestMethod]
    public void Detect_Utf16BeBom_ShouldReturnUnicodeBe()
    {
        byte[] bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69 }; // BOM + "Hi"
        var result = _detector.Detect(bytes, bytes.Length);

        Assert.IsTrue(result.HasBom);
        Assert.AreEqual(System.Text.Encoding.BigEndianUnicode.WebName, result.Encoding.WebName);
    }

    [TestMethod]
    public void Detect_Utf8WithoutBom_ShouldReturnUtf8WithoutBom()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Привет, мир! MyPad testing plain text.");
        var result = _detector.Detect(bytes, bytes.Length);

        Assert.IsFalse(result.HasBom);
        Assert.AreEqual(System.Text.Encoding.UTF8.WebName, result.Encoding.WebName);
    }
}
