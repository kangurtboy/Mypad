using System.Text.Json;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;

namespace MyPad.Infrastructure.Storage;

public class RecoveryService : IRecoveryService
{
    private readonly string _recoveryDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public RecoveryService(string? customRecoveryDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(customRecoveryDirectory))
        {
            _recoveryDirectory = customRecoveryDirectory;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _recoveryDirectory = Path.Combine(appData, "MyPad", "recovery");
        }
    }

    public async Task SaveSnapshotAsync(TextDocument document, CancellationToken ct = default)
    {
        if (!Directory.Exists(_recoveryDirectory))
        {
            Directory.CreateDirectory(_recoveryDirectory);
        }

        var snapshot = new RecoverySnapshot
        {
            DocumentId = document.Id,
            OriginalFilePath = document.FilePath,
            Title = document.Title,
            Content = document.Content,
            EncodingWebName = document.Encoding.WebName,
            HasBom = document.HasBom,
            LineEnding = document.LineEnding,
            Kind = document.Kind,
            CursorPosition = document.CursorPosition,
            SelectionLength = document.SelectionLength,
            VerticalScrollOffset = document.VerticalScrollOffset,
            SnapshotTimestampUtc = DateTime.UtcNow,
            OriginalFileFingerprint = document.FileFingerprint
        };

        string snapshotPath = Path.Combine(_recoveryDirectory, $"{document.Id}.snapshot.json");
        string tempPath = snapshotPath + ".tmp";

        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, ct);
            await stream.FlushAsync(ct);
        }

        File.Move(tempPath, snapshotPath, true);
    }

    public async Task<IReadOnlyList<RecoverySnapshot>> LoadAllSnapshotsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_recoveryDirectory))
        {
            return Array.Empty<RecoverySnapshot>();
        }

        var snapshots = new List<RecoverySnapshot>();
        string[] files = Directory.GetFiles(_recoveryDirectory, "*.snapshot.json");

        foreach (string file in files)
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var snapshot = await JsonSerializer.DeserializeAsync<RecoverySnapshot>(stream, JsonOptions, ct);
                if (snapshot != null)
                {
                    snapshots.Add(snapshot);
                }
            }
            catch
            {
                // Continue reading remaining snapshots if one fails
            }
        }

        return snapshots;
    }

    public Task DeleteSnapshotAsync(Guid documentId, CancellationToken ct = default)
    {
        string snapshotPath = Path.Combine(_recoveryDirectory, $"{documentId}.snapshot.json");
        if (File.Exists(snapshotPath))
        {
            File.Delete(snapshotPath);
        }

        return Task.CompletedTask;
    }

    public Task ClearAllSnapshotsAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(_recoveryDirectory))
        {
            foreach (string file in Directory.GetFiles(_recoveryDirectory, "*.snapshot.json"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore deletion failures during clear
                }
            }
        }

        return Task.CompletedTask;
    }
}
