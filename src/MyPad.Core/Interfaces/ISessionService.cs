using MyPad.Core.Models;

namespace MyPad.Core.Interfaces;

/// <summary>
/// Service responsible for persisting and restoring workspace sessions.
/// </summary>
public interface ISessionService
{
    Task SaveSessionAsync(SessionState session, CancellationToken ct = default);
    Task<SessionState?> LoadSessionAsync(CancellationToken ct = default);
    Task ClearSessionAsync(CancellationToken ct = default);
}
