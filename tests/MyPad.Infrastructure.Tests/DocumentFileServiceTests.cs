using System.Text;
using MyPad.Core.Enums;
using MyPad.Core.Models;
using MyPad.Infrastructure.Encoding;
using MyPad.Infrastructure.FileSystem;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class DocumentFileServiceTests
{
    private string _testDir = null!;
    private DocumentFileService _fileService = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MyPad_Test_FileService_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _fileService = new DocumentFileService(new EncodingDetector(), new LineEndingDetector());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [TestMethod]
    public async Task OpenAndSave_PreservesContentAndLineEndings()
    {
        string filePath = Path.Combine(_testDir, "test_crlf.txt");
        string originalContent = "Line 1\r\nLine 2\r\nLine 3";
        await File.WriteAllTextAsync(filePath, originalContent, System.Text.Encoding.UTF8);

        var document = await _fileService.OpenAsync(filePath);
        Assert.AreEqual(LineEndingMode.Crlf, document.LineEnding);
        Assert.AreEqual(originalContent, document.Content);

        document.Content = "Line 1\r\nLine 2\r\nLine 3 modified";
        await _fileService.SaveAsync(document);

        string savedContent = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
        Assert.AreEqual(document.Content, savedContent);
    }

    [TestMethod]
    public async Task CheckForExternalChanges_DetectsExternalFileModifications()
    {
        string filePath = Path.Combine(_testDir, "external_test.txt");
        await File.WriteAllTextAsync(filePath, "Initial content", System.Text.Encoding.UTF8);

        var document = await _fileService.OpenAsync(filePath);
        Assert.IsFalse(_fileService.CheckForExternalChanges(document));

        // External modification simulation
        await Task.Delay(50);
        await File.WriteAllTextAsync(filePath, "Modified by another app", System.Text.Encoding.UTF8);

        Assert.IsTrue(_fileService.CheckForExternalChanges(document));
    }
}
