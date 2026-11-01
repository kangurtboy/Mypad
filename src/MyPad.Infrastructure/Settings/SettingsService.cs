using System.Text.Json;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;

namespace MyPad.Infrastructure.Settings;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsService(string? customSettingsFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customSettingsFilePath))
        {
            _settingsFilePath = customSettingsFilePath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _settingsFilePath = Path.Combine(appData, "MyPad", "settings", "settings.json");
        }
    }

    public AppSettings CurrentSettings => _currentSettings;

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                using var stream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                    return _currentSettings;
                }
            }
        }
        catch
        {
            // Fallback to default settings on error
        }

        _currentSettings = new AppSettings();
        return _currentSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));

        string? directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempFilePath = _settingsFilePath + ".tmp";
        using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct);
            await stream.FlushAsync(ct);
        }

        File.Move(tempFilePath, _settingsFilePath, true);
    }
}
