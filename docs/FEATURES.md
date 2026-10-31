# Feature Catalogue

This document defines feature scope and acceptance behavior.

## P0 — Essential editor
- New/open/save/save as/save all.
- Plain text editing.
- Undo/redo.
- Clipboard.
- Find/replace.
- Word wrap.
- Encoding and line endings.
- Tabs and dirty-state indicator.
- Close confirmation.
- Basic status bar.

## P0 — Data integrity
- Session recovery.
- Safe save.
- External modification warning.
- Backup support.
- Permission and disk-full error handling.

## P1 — Markdown
- CommonMark plus selected GFM extensions.
- Source editor and live split preview.
- Debounced rendering.
- Safe HTML policy.
- Optional scroll synchronization.

## P1 — Themes
- System/Light/Dark application theme.
- Independent editor theme.
- Built-in palettes.
- Custom theme JSON import/export.

## P1 — Usability
- Recent files.
- Favorites.
- Go to line.
- Word/character counts.
- Font and tab-width settings.
- Keyboard shortcuts.

## P2 — Advanced
- Multiple windows.
- Always on top.
- Focus mode/full screen.
- Portable mode.
- Configurable keyboard shortcuts.
- Context-menu integration.

## Acceptance examples
- Opening a UTF-8 LF file and saving without edits must not unexpectedly change its encoding or line endings.
- Closing a dirty tab must offer Save / Don't Save / Cancel.
- A crash after a recovery snapshot must allow the user to restore unsaved text.
- A Markdown document must update its preview after edits without blocking typing.
- Switching Windows theme in System mode must update app chrome and select the configured matching editor palette.
- Invalid custom theme JSON must be rejected with a useful error and must not corrupt current settings.
