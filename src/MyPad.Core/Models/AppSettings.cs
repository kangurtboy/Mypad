using MyPad.Core.Enums;

namespace MyPad.Core.Models;

public class AppSettings
{
    // Appearance
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    public string EditorThemeName { get; set; } = "Default Dark";

    // Editor
    public string FontFamily { get; set; } = "Consolas";

    public double FontSize { get; set; } = 14.0;

    public bool WordWrap { get; set; } = true;

    public bool ShowLineNumbers { get; set; } = true;

    public int TabWidth { get; set; } = 4;

    public bool InsertSpacesForTabs { get; set; } = true;

    // Markdown
    public bool MarkdownLivePreview { get; set; } = true;

    public bool MarkdownSyncScroll { get; set; } = true;

    // Tabs & Session
    public bool RestoreSessionOnStartup { get; set; } = true;

    // Autosave
    public bool DirectFileAutosaveEnabled { get; set; } = false;

    public int AutosaveIntervalSeconds { get; set; } = 30;

    // Backups
    public bool BackupsEnabled { get; set; } = true;

    public int BackupRetentionCount { get; set; } = 5;

    public string? CustomBackupDirectory { get; set; }

    // Files
    public string DefaultEncodingWebName { get; set; } = "utf-8";

    public LineEndingMode DefaultLineEnding { get; set; } = LineEndingMode.Crlf;
}
