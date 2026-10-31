# Roadmap

Status legend: `NOT STARTED`, `IN PROGRESS`, `BLOCKED`, `DONE`.

## Phase 0 — Foundation and technical spike
- [ ] Verify supported .NET / Windows App SDK / Windows SDK combination.
- [ ] Declare minimum Windows build.
- [ ] Create solution and project skeleton.
- [ ] Build minimal WinUI 3 app on Windows 10 and 11.
- [ ] Prototype editor control candidates.
- [ ] Measure large-document behavior.
- [ ] Decide editor engine (ADR-0001).
- [ ] Decide packaging baseline (ADR-0002).
- [ ] Record dependency versions.

Exit criteria: a reproducible build and evidence-backed editor decision.

## Phase 1 — UI foundation
- [ ] Main window and menu.
- [ ] Tab strip.
- [ ] Editor host.
- [ ] Status bar.
- [ ] App theme switching.
- [ ] Keyboard navigation.

## Phase 2 — Core editor
- [ ] Text input and selection.
- [ ] Undo/redo and clipboard.
- [ ] Open/save/save as.
- [ ] Encoding and line endings.
- [ ] Find/replace.
- [ ] Word wrap and zoom.
- [ ] Line/column status.

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
