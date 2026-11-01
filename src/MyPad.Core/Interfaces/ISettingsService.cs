using MyPad.Core.Models;

namespace MyPad.Core.Interfaces;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }

    Task<AppSettings> LoadSettingsAsync(CancellationToken ct = default);

    Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default);
}
