using MyPad.Core.Interfaces;

namespace MyPad.Infrastructure.Storage;

public class BackupService : IBackupService
{
    private readonly string _backupDirectory;

    public BackupService(string? customBackupDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(customBackupDirectory))
        {
            _backupDirectory = customBackupDirectory;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _backupDirectory = Path.Combine(appData, "MyPad", "backups");
        }
    }

    public async Task CreateBackupAsync(string sourceFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            return;
        }

        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }

        string fileName = Path.GetFileName(sourceFilePath);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string uniqueTag = Guid.NewGuid().ToString("N")[..6];
        string backupFileName = $"{fileName}.{timestamp}_{uniqueTag}.bak";
        string destinationPath = Path.Combine(_backupDirectory, backupFileName);

        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        await sourceStream.CopyToAsync(destStream, ct);
        await destStream.FlushAsync(ct);
    }

    public Task PruneBackupsAsync(string sourceFilePath, int maxBackups, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !Directory.Exists(_backupDirectory) || maxBackups <= 0)
        {
            return Task.CompletedTask;
        }

        string fileName = Path.GetFileName(sourceFilePath);
        string searchPattern = $"{fileName}.*.bak";

        try
        {
            var directoryInfo = new DirectoryInfo(_backupDirectory);
            var backupFiles = directoryInfo.GetFiles(searchPattern)
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (backupFiles.Count > maxBackups)
            {
                var filesToDelete = backupFiles.Skip(maxBackups);
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore individual deletion errors
                    }
                }
            }
        }
        catch
        {
            // Ignore directory scanning errors
        }

        return Task.CompletedTask;
    }
}
