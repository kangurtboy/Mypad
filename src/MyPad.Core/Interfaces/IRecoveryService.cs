using MyPad.Core.Models;

namespace MyPad.Core.Interfaces;

public interface IRecoveryService
{
    Task SaveSnapshotAsync(TextDocument document, CancellationToken ct = default);

    Task<IReadOnlyList<RecoverySnapshot>> LoadAllSnapshotsAsync(CancellationToken ct = default);

    Task DeleteSnapshotAsync(Guid documentId, CancellationToken ct = default);

    Task ClearAllSnapshotsAsync(CancellationToken ct = default);
}
