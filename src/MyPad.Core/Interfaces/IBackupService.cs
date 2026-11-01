namespace MyPad.Core.Interfaces;

public interface IBackupService
{
    Task CreateBackupAsync(string sourceFilePath, CancellationToken ct = default);

    Task PruneBackupsAsync(string sourceFilePath, int maxBackups, CancellationToken ct = default);
}
