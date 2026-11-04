# Implementation Status

**Current stage:** Phase 7 — Polish  
**Last updated:** 2026-11-04

This repository contains the architecture, core domain models, infrastructure services, and test suite. Phases 0 through 6 are completed. Polish, Settings UI, recent files, and accessibility (Phase 7) is next.

## Completed
- [x] Product name and scope established.
- [x] Core technical direction established.
- [x] Product specification drafted.
- [x] Architecture and agent instructions drafted.
- [x] Roadmap and decision records created.
- [x] Verify official Windows/.NET/Windows App SDK compatibility (.NET 8 LTS baseline on Windows 10 Build 19041+).
- [x] Create solution and project files (`MyPad.slnx`, `MyPad.Core`, `MyPad.Infrastructure`, `MyPad.App`, test suites).
- [x] Implement Core domain models, contracts, and Infrastructure services (atomic file writes, encoding and line-ending detection, safe Markdown rendering, session recovery storage).
- [x] Automated unit tests implemented and passing (35/35 tests pass).
- [x] Build and restore the WinUI 3 starter app (`MyPad.App` compiles to `MyPad.App.exe` with 0 warnings and 0 errors).
- [x] Complete editor-engine comparative architecture spike and trade-off analysis (Windows 11 Notepad RichEdit, Notepads App, Scintilla, Win2D Canvas).
- [x] Accept ADR-0001 (Editor Engine: WinUI RichEditBox with Plain-Text Guard) and ADR-0002 (Packaging: Unpackaged baseline with MSIX support).
- [x] Phase 1 UI Foundation:
  - Main window shell with Mica backdrop and Windows 10 fallback.
  - Native Fluent MenuBar (File, Edit, View, Theme, Help) with standard keyboard accelerators.
  - Multi-tab strip (`TabView`) with dynamic creation, dirty tracking, and tab closing.
  - Dedicated plain-text `EditorHost` wrapping `RichEditBox` with plain-text paste guard.
  - Dynamic status bar showing Ln/Col, zoom percentage, line endings (CRLF/LF/CR), and encoding.
  - App theme switching (System, Light, Dark).
  - Keyboard navigation for tabs (Ctrl+Tab, Ctrl+Shift+Tab) and editing operations.
- [x] Phase 2 Core Editor:
  - FileOpenPicker and FileSavePicker integrated with Window Native HWND interop.
  - Safe atomic file opening, saving, and Save As using `DocumentFileService`.
  - Unsaved changes confirmation dialog before closing dirty tabs.
  - Dynamic line endings switching (CRLF, LF, CR) and encoding switching (UTF-8, UTF-16, ANSI) with interactive dropdown flyouts in the status bar.
  - Full Find and Replace floating panel (`FindReplaceBar`) with Ctrl+F / Ctrl+H, next/prev matching, match case, replace, and atomic replace-all.
- [x] Phase 3 Tabs and Session:
  - Tab drag-and-drop reordering (`CanReorderTabs`, `CanDragTabs`).
  - Rich Tab context menu (Close, Close Others, Close to the Right, Copy Full Path, Open Containing Folder).
  - Multi-window support (`Ctrl+Shift+N` spawns independent `MainWindow` instances).
  - Full session persistence and restoration (`ISessionService`, `SessionService`, saving and loading open tabs, drafts, caret positions, and active tab index across app restarts).
- [x] Phase 4 Data Protection:
  - Recovery snapshots: periodic snapshot journal of modified documents every 10 seconds via `IRecoveryService`.
  - Crash recovery prompt: checks on startup for unexpected termination snapshots and prompts user to Recover or Discard.
  - Optional file autosave: toggleable auto-save for open files (`File -> Auto-Save`).
  - External-change detection: detects when open files are modified outside MyPad (silent reload for pristine tabs, conflict prompt for dirty tabs).
  - Safe writes: atomic replace with temp files and directory creation.
  - Backup retention: automated backups on save via `IBackupService` with configurable 5-version retention pruning.
- [x] Phase 5 Markdown:
  - Markdig pipeline with GitHub Flavored Markdown (tables, task lists, autolinks, emphasis extras) and disabled raw HTML.
  - Token syntax highlighting in `EditorHost` for Headers, Bold, and inline Code.
  - Live preview with debounced updates using `MarkdownPreviewPane`.
  - Resizable split view layout (`Editor Only`, `Split View`, `Preview Only`) toggleable via `Alt+P` or View menu.
  - Strict Content Security Policy (CSP) and blocked scripts on `WebView2`.
- [x] Phase 6 Themes:
  - Independent application theme and editor palette layers.
  - Built-in palette catalog (Default Dark, One Dark, Dracula, Nord, Catppuccin Mocha, Monokai, Gruvbox Dark, Default Light, GitHub Light, Solarized Light, Catppuccin Latte, Gruvbox Light).
  - Versioned Theme JSON Schema with validation (strict hex verification, 256 KB limit, no script execution).
  - Transactional import/export via `ThemeService`.
  - WCAG contrast ratio calculation.

## Pending
- [ ] Phase 7 — Polish (Settings UI, recent files, focus mode, accessibility).

Update this file honestly after each milestone.
