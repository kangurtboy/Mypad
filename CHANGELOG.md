# Changelog

All notable changes to MyPad will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v0.1.0] - 2026-11-20

### Added
- **Visual WYSIWYG & Markdown Modes**: Seamless toggle between clean visual WYSIWYG (no symbols on screen) and standard Markdown syntax (`**bold**`, `# Heading`).
- **Bidirectional Markdown Converter**: Converts visual formatting to standard CommonMark on save, and parses Markdown files into clean visual styles upon opening.
- **In-Memory Tab State Preservation**: Tabs retain full formatting across tab switching without data loss.
- **Rich Context Menu Formatting**: Bold (`Ctrl+B`), Italic (`Ctrl+I`), Underline (`Ctrl+U`), Strikethrough, Headings (H1, H2, H3), Quotes, Lists, and Code blocks.
- **Markdown Live Preview**: Split view and Preview Only modes powered by safe WebView2 with script and remote network isolation.
- **Crash Recovery & Autosave**: Recovery snapshots in `%LocalAppData%\MyPad\recovery`, configurable autosave, and safe atomic file writes.
- **Theming**: Independent application themes (Light, Dark, System) and editor syntax palettes.
- **Windows Integration**:
  - Inno Setup 6 standalone Windows installer (`MyPad-Setup-x64.exe`).
  - Portable ZIP distribution (`MyPad-win-x64-portable.zip`).
  - Windows Explorer "Open with" and context menu ("Edit with MyPad").
  - `Win+R mypad` execution via App Paths.
  - Automated GitHub Actions release pipeline (`.github/workflows/release.yml`).
