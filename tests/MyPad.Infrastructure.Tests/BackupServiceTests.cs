using MyPad.Infrastructure.Storage;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class BackupServiceTests
{
    private string _testDir = null!;
    private string _backupDir = null!;
    private BackupService _backupService = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MyPad_Test_Backup_" + Guid.NewGuid().ToString("N"));
        _backupDir = Path.Combine(_testDir, "backups");
        Directory.CreateDirectory(_testDir);
        _backupService = new BackupService(_backupDir);
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
    public async Task CreateBackupAsync_CreatesCopyInBackupDirectory()
    {
        string sourceFile = Path.Combine(_testDir, "document.txt");
        await File.WriteAllTextAsync(sourceFile, "Original content for backup");

        await _backupService.CreateBackupAsync(sourceFile);

        var backups = Directory.GetFiles(_backupDir, "document.txt.*.bak");
        Assert.AreEqual(1, backups.Length);

        string backupContent = await File.ReadAllTextAsync(backups[0]);
        Assert.AreEqual("Original content for backup", backupContent);
    }

    [TestMethod]
    public async Task PruneBackupsAsync_KeepsOnlyMaxCount()
    {
        string sourceFile = Path.Combine(_testDir, "report.md");
        await File.WriteAllTextAsync(sourceFile, "# Report");

        for (int i = 0; i < 5; i++)
        {
            await _backupService.CreateBackupAsync(sourceFile);
            await Task.Delay(20);
        }

        var backupsBefore = Directory.GetFiles(_backupDir, "report.md.*.bak");
        Assert.AreEqual(5, backupsBefore.Length);

        await _backupService.PruneBackupsAsync(sourceFile, maxBackups: 2);

        var backupsAfter = Directory.GetFiles(_backupDir, "report.md.*.bak");
        Assert.AreEqual(2, backupsAfter.Length);
    }
}
