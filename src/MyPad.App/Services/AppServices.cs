using MyPad.Core.Interfaces;
using MyPad.Infrastructure.Encoding;
using MyPad.Infrastructure.FileSystem;
using MyPad.Infrastructure.Markdown;
using MyPad.Infrastructure.Settings;
using MyPad.Infrastructure.Storage;

namespace MyPad.App.Services;

/// <summary>
/// Simple composition root and service registry for the application.
/// </summary>
public static class AppServices
{
    private static readonly Lazy<IEncodingDetector> _encodingDetector =
        new(() => new EncodingDetector());

    private static readonly Lazy<ILineEndingDetector> _lineEndingDetector =
        new(() => new LineEndingDetector());

    private static readonly Lazy<IDocumentFileService> _documentFileService =
        new(() => new DocumentFileService(_encodingDetector.Value, _lineEndingDetector.Value));

    private static readonly Lazy<ISettingsService> _settingsService =
        new(() => new SettingsService());

    private static readonly Lazy<IRecoveryService> _recoveryService =
        new(() => new RecoveryService());

    private static readonly Lazy<IMarkdownRenderer> _markdownRenderer =
        new(() => new MarkdownRenderer());

    private static readonly Lazy<ISessionService> _sessionService =
        new(() => new MyPad.Infrastructure.Session.SessionService());

    private static readonly Lazy<IBackupService> _backupService =
        new(() => new BackupService());

    public static IEncodingDetector EncodingDetector => _encodingDetector.Value;
    public static ILineEndingDetector LineEndingDetector => _lineEndingDetector.Value;
    public static IDocumentFileService DocumentFileService => _documentFileService.Value;
    public static ISettingsService SettingsService => _settingsService.Value;
    public static IRecoveryService RecoveryService => _recoveryService.Value;
    public static IMarkdownRenderer MarkdownRenderer => _markdownRenderer.Value;
    public static ISessionService SessionService => _sessionService.Value;
    public static IBackupService BackupService => _backupService.Value;
}
