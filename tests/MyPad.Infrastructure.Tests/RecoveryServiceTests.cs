using MyPad.Core.Enums;
using MyPad.Core.Models;
using MyPad.Infrastructure.Storage;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class RecoveryServiceTests
{
    private string _testDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MyPad_Test_Recovery_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
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
    public async Task SaveAndLoadSnapshot_ShouldPersistAndRestoreSnapshot()
    {
        var service = new RecoveryService(_testDir);
        var doc = new TextDocument
        {
            Id = Guid.NewGuid(),
            Title = "Draft Document",
            Content = "Important unsaved work",
            Kind = DocumentKind.Markdown,
            LineEnding = LineEndingMode.Lf,
            CursorPosition = 10,
            SelectionLength = 5
        };

        await service.SaveSnapshotAsync(doc);

        var snapshots = await service.LoadAllSnapshotsAsync();
        Assert.HasCount(1, snapshots);
        Assert.AreEqual(doc.Id, snapshots[0].DocumentId);
        Assert.AreEqual(doc.Title, snapshots[0].Title);
        Assert.AreEqual(doc.Content, snapshots[0].Content);
        Assert.AreEqual(DocumentKind.Markdown, snapshots[0].Kind);
    }

    [TestMethod]
    public async Task DeleteSnapshot_ShouldRemoveFile()
    {
        var service = new RecoveryService(_testDir);
        var doc = new TextDocument
        {
            Id = Guid.NewGuid(),
            Title = "Temporary",
            Content = "Temp content"
        };

        await service.SaveSnapshotAsync(doc);
        var snapshotsBefore = await service.LoadAllSnapshotsAsync();
        Assert.HasCount(1, snapshotsBefore);

        await service.DeleteSnapshotAsync(doc.Id);
        var snapshotsAfter = await service.LoadAllSnapshotsAsync();
        Assert.IsEmpty(snapshotsAfter);
    }
}
