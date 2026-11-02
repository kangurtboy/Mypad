using MyPad.Core.Enums;
using MyPad.Core.Models;
using MyPad.Infrastructure.Session;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class SessionServiceTests
{
    private string _testDir = null!;
    private string _sessionFile = null!;
    private SessionService _sessionService = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MyPad_Test_Session_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _sessionFile = Path.Combine(_testDir, "session.json");
        _sessionService = new SessionService(_sessionFile);
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
    public async Task SaveAndLoadSession_PreservesTabsAndSelection()
    {
        var session = new SessionState
        {
            SelectedTabIndex = 1,
            Tabs = new List<SessionTabState>
            {
                new()
                {
                    Title = "Doc1.txt",
                    FilePath = @"C:\docs\Doc1.txt",
                    Content = "Hello from Doc 1",
                    CaretLine = 2,
                    CaretColumn = 5,
                    ZoomLevel = 110,
                    LineEnding = LineEndingMode.Lf,
                    EncodingName = "UTF-8",
                    IsModified = false
                },
                new()
                {
                    Title = "Untitled 2",
                    FilePath = null,
                    Content = "Unsaved draft",
                    CaretLine = 1,
                    CaretColumn = 1,
                    ZoomLevel = 100,
                    LineEnding = LineEndingMode.Crlf,
                    EncodingName = "UTF-8",
                    IsModified = true
                }
            }
        };

        await _sessionService.SaveSessionAsync(session);

        var loaded = await _sessionService.LoadSessionAsync();
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded.SelectedTabIndex);
        Assert.AreEqual(2, loaded.Tabs.Count);

        Assert.AreEqual("Doc1.txt", loaded.Tabs[0].Title);
        Assert.AreEqual(@"C:\docs\Doc1.txt", loaded.Tabs[0].FilePath);
        Assert.AreEqual("Hello from Doc 1", loaded.Tabs[0].Content);
        Assert.AreEqual(LineEndingMode.Lf, loaded.Tabs[0].LineEnding);
        Assert.AreEqual(2, loaded.Tabs[0].CaretLine);
        Assert.AreEqual(5, loaded.Tabs[0].CaretColumn);

        Assert.AreEqual("Untitled 2", loaded.Tabs[1].Title);
        Assert.IsNull(loaded.Tabs[1].FilePath);
        Assert.IsTrue(loaded.Tabs[1].IsModified);
    }

    [TestMethod]
    public async Task LoadSession_NonExistentFile_ReturnsNull()
    {
        var loaded = await _sessionService.LoadSessionAsync();
        Assert.IsNull(loaded);
    }

    [TestMethod]
    public async Task LoadSession_CorruptFile_ReturnsNull()
    {
        await File.WriteAllTextAsync(_sessionFile, "INVALID JSON {{{ corrupt");
        var loaded = await _sessionService.LoadSessionAsync();
        Assert.IsNull(loaded);
    }

    [TestMethod]
    public async Task ClearSession_RemovesFile()
    {
        await _sessionService.SaveSessionAsync(new SessionState());
        Assert.IsTrue(File.Exists(_sessionFile));

        await _sessionService.ClearSessionAsync();
        Assert.IsFalse(File.Exists(_sessionFile));
    }
}
