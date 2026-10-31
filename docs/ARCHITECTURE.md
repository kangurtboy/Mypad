# Architecture

## 1. High-level structure

```text
MyPad.App (WinUI 3)
    |  Views, ViewModels, Controls, app composition
    v
MyPad.Core
    |  Domain models, interfaces, validation, editor contracts
    ^
    |
MyPad.Infrastructure
       File system, encoding, settings, recovery, backup, Markdown
```

### MyPad.App
Owns Windows UI and platform integration:
- Main window, menus, tab strip, status bar, settings views.
- Editor and preview controls.
- ViewModels and navigation.
- Application lifecycle and activation.
- Dependency injection composition root.

### MyPad.Core
Must not reference WinUI, XAML, or Windows UI controls.
- Document/session models.
- Settings/theme models.
- Interfaces for file, recovery, backup, and settings services.
- Validation and domain-level rules.

### MyPad.Infrastructure
Implements Core interfaces:
- File open/save and atomic-write strategy.
- Encoding and line-ending detection.
- Settings serialization.
- Session recovery storage.
- Backup management.
- Markdown parsing adapter.

## 2. Dependency direction
App may depend on Core and Infrastructure. Infrastructure depends on Core. Core depends on neither App nor Infrastructure.

## 3. Suggested services
- IDocumentFileService
- IEncodingDetector
- IRecoveryService
- IBackupService
- ISettingsService
- IRecentFilesService
- IMarkdownRenderer
- IThemeService
- IExternalChangeMonitor

Keep interfaces focused. Avoid a generic service locator.

## 4. Document model
A document should track:
- Stable in-session document ID.
- Optional file path.
- Display name.
- Text buffer abstraction.
- Encoding.
- Line-ending mode.
- Dirty state.
- Last known file metadata/fingerprint.
- Cursor and selection state.
- Scroll position.
- Document kind (plain text / Markdown / inferred text type).

Do not make a rich-text document object the canonical storage model. The canonical content is plain text.

## 5. UI threading
- All UI control access occurs on the UI dispatcher.
- File I/O and expensive parsing must not block UI.
- Use cancellation for obsolete preview/render operations.
- Avoid stale async results overwriting newer editor state.

## 6. Persistence
Separate:
1. User preferences.
2. Session metadata.
3. Recovery snapshots.
4. Backup copies.
5. Original user files.

Each store has a distinct lifecycle and retention policy.

## 7. Error handling
Use typed results or documented exceptions at service boundaries. Surface actionable user messages. Preserve recovery snapshots on failed saves.

## 8. Architecture decision records
Record important choices in `docs/decisions/`. Do not silently change editor engine, packaging model, or persistence format.
