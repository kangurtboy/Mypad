using System.Text.Json;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;

namespace MyPad.Infrastructure.Session;

public class SessionService : ISessionService
{
    private readonly string _sessionFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SessionService(string? customSessionFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customSessionFilePath))
        {
            _sessionFilePath = customSessionFilePath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _sessionFilePath = Path.Combine(appData, "MyPad", "session", "session.json");
        }
    }

    public async Task SaveSessionAsync(SessionState session, CancellationToken ct = default)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        string? dir = Path.GetDirectoryName(_sessionFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tempPath = $"{_sessionFilePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await JsonSerializer.SerializeAsync(stream, session, JsonOptions, ct);
                await stream.FlushAsync(ct);
            }

            if (File.Exists(_sessionFilePath))
            {
                File.Replace(tempPath, _sessionFilePath, null);
            }
            else
            {
                File.Move(tempPath, _sessionFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    public async Task<SessionState?> LoadSessionAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_sessionFilePath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(_sessionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            return await JsonSerializer.DeserializeAsync<SessionState>(stream, JsonOptions, ct);
        }
        catch
        {
            // Corrupt or unreadable session file
            return null;
        }
    }

    public Task ClearSessionAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }
        catch
        {
            // Ignore deletion failures
        }

        return Task.CompletedTask;
    }
}
