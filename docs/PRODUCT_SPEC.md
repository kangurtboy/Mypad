# MyPad — Product Specification

**Version:** 1.0  
**Status:** Approved baseline  
**Product:** Native Windows text editor

## 1. Vision
MyPad is a focused, fast, local-first text editor inspired by Windows Notepad. It retains the simplicity of a plain-text editor while adding dependable tabs and recovery, Markdown live preview, and deep theme customization.

## 2. Target users
- Windows users who need a fast everyday text editor.
- Markdown authors who prefer a lightweight desktop application.
- Users who want a local, offline tool without accounts or ads.

## 3. Product principles
1. Data integrity is more important than convenience.
2. Plain text remains the canonical editable document representation.
3. The default UI is minimal; advanced controls remain discoverable.
4. App chrome and editor palette are independently configurable.
5. Offline operation is the default.
6. Windows 10 support must be verified, not merely advertised.

## 4. Functional scope

### 4.1 Plain-text editing
- New, open, save, save as, save all, close.
- Unicode text and explicit encoding handling.
- UTF-8, UTF-8 BOM, UTF-16 LE/BE; detect common encodings conservatively.
- CRLF and LF support; preserve original line endings unless the user changes them.
- Undo/redo, cut/copy/paste, select all.
- Word wrap, zoom, configurable font and tab width.
- Go to line, line/column display, word and character counts.
- Find/replace, match case, whole word, regex.
- Drag-and-drop file opening.
- Warn before opening likely binary files.

### 4.2 Tabs and windows
- Multiple documents in tabs.
- Reorder tabs, close tab, duplicate tab, context menu.
- Unsaved-change indicator.
- New window and multiple independent windows.
- Restore prior session and cursor/scroll positions.
- Missing or moved files must be handled without crashing.

### 4.3 Data protection
- Session recovery snapshots.
- Optional direct-to-file autosave.
- Configurable backup retention.
- External modification detection.
- Safe writes using temporary files and replacement where supported.
- Recovery prompt after abnormal shutdown.
- No silent overwrite of externally modified files.

### 4.4 Markdown
- `.md`, `.markdown`, `.mdown`, `.mkd`.
- CommonMark baseline and selected GFM extensions.
- Headings, emphasis, strike-through, lists, task lists, links, images, blockquotes, tables, code blocks, inline code, thematic breaks, autolinks.
- Live split view: source editor and rendered preview.
- Debounced preview updates.
- Optional synchronized scrolling.
- Preview-only and editor-only modes.
- Safe rendering: no script execution, no automatic remote content loading, sanitize/disable unsafe HTML.

### 4.5 Themes
Application theme:
- System
- Light
- Dark

Editor theme is independent:
- Light palette selection
- Dark palette selection
- System-following pair selection

Initial palette catalogue:
- Default Light / Default Dark
- GitHub Light
- Solarized Light
- Catppuccin Latte / Mocha
- Gruvbox Light / Dark
- One Dark
- Dracula
- Nord
- Monokai

Theme names identify color-scheme inspirations; do not imply affiliation or official endorsement. Provide custom JSON theme import/export. Validate imported theme values and reject malformed files.

### 4.6 Settings
- Appearance
- Editor
- Markdown
- Tabs
- Autosave
- Backups
- Files
- Keyboard shortcuts
- Language
- About

Settings are local JSON. Apply changes immediately when safe.

### 4.7 File integration
- Recent files and favorites.
- File associations for text and Markdown extensions, user-controlled.
- Context-menu integration where packaging and Windows policy permit.
- Portable mode as a later milestone.
- Do not delete user files or settings during uninstall without explicit consent.

## 5. Non-functional requirements

### Performance
- Keep UI thread free from expensive work.
- Async file I/O and Markdown parsing where appropriate.
- Debounce live preview.
- Avoid repeated full-document formatting on every keystroke.
- Measure cold/warm startup, memory, typing latency, file open/save, preview update.
- Test 1 KB, 100 KB, 1 MB, 5 MB, and 10 MB documents.
- Large-file behavior may use documented feature degradation rather than freezing.

### Reliability
- No silent data loss.
- Recovery data must survive process crash.
- Handle permission errors, disk-full conditions, missing paths, and external changes.
- Automated tests for encoding round trips, line endings, safe writes, and recovery.

### Privacy/security
- No account, ads, cloud sync, document upload, or hidden telemetry.
- Do not log document contents.
- Treat files and Markdown as untrusted input.
- No script execution in preview.
- No automatic network requests from preview.

### Accessibility/usability
- Keyboard-first workflows.
- Visible focus states.
- High-contrast readable themes.
- Respect Windows text scaling where practical.
- Screen-reader labels for interactive controls.

## 6. Out of scope for initial release
- Full IDE features and language-server integration.
- Collaborative editing.
- Cloud synchronization.
- AI writing features.
- Plugin marketplace.
- Rich-text word processor functionality.
- Mobile versions.

## 7. Release acceptance
A release candidate must pass the checklist in `TESTING.md`, run on the declared Windows support matrix, and produce both MSIX and EXE distributions.
