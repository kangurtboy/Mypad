# Roadmap

Status legend: `NOT STARTED`, `IN PROGRESS`, `BLOCKED`, `DONE`.

## Phase 0 — Foundation and technical spike
- [x] Verify supported .NET / Windows App SDK / Windows SDK combination.
- [x] Declare minimum Windows build.
- [x] Create solution and project skeleton.
- [x] Build minimal WinUI 3 app on Windows 10 and 11.
- [x] Prototype editor control candidates (Comparative architectural study of Win11 Notepad, Notepads App, Scintilla, Win2D).
- [x] Measure large-document behavior (Documented performance thresholds & safeguards).
- [x] Decide editor engine (ADR-0001).
- [x] Decide packaging baseline (ADR-0002).
- [x] Record dependency versions.

Exit criteria: a reproducible build and evidence-backed editor decision.

## Phase 1 — UI foundation
- [x] Main window and menu.
- [x] Tab strip.
- [x] Editor host.
- [x] Status bar.
- [x] App theme switching.
- [x] Keyboard navigation.

## Phase 2 — Core editor
- [x] Text input and selection.
- [x] Undo/redo and clipboard.
- [x] Open/save/save as.
- [x] Encoding and line endings.
- [x] Find/replace.
- [x] Word wrap and zoom.
- [x] Line/column status.

## Phase 3 — Tabs and session
- [ ] Multiple tabs.
- [ ] Reorder and context menu.
- [ ] Dirty-state tracking.
- [ ] Session persistence.
- [ ] Session restore.
- [ ] Multiple windows.

## Phase 4 — Data protection
- [ ] Recovery snapshots.
- [ ] Crash recovery prompt.
- [ ] Optional file autosave.
- [ ] External-change detection.
- [ ] Safe writes.
- [ ] Backup retention.

## Phase 5 — Markdown
- [ ] Markdig integration.
- [ ] Syntax highlighting.
- [ ] Live preview.
- [ ] Split view.
- [ ] Safe preview policy.
- [ ] Scroll synchronization.

## Phase 6 — Themes
- [ ] Independent app/editor themes.
- [ ] Built-in palettes.
- [ ] Theme JSON schema.
- [ ] Import/export.
- [ ] Theme validation and contrast review.

## Phase 7 — Polish
- [ ] Settings UI.
- [ ] Recent files and favorites.
- [ ] Localization.
- [ ] Focus mode/full screen.
- [ ] Always on top.
- [ ] Accessibility pass.

## Phase 8 — Test and optimize
- [ ] Unit/integration/UI tests.
- [ ] Large-file tests.
- [ ] Performance profiling.
- [ ] Memory profiling.
- [ ] Windows 10/11 matrix.
- [ ] Security review of Markdown preview.

## Phase 9 — Release
- [ ] MSIX packaging.
- [ ] EXE installer.
- [ ] Portable mode decision.
- [ ] Upgrade/uninstall tests.
- [ ] Release notes.
- [ ] GitHub release.
