# Roadmap

Status legend: `NOT STARTED`, `IN PROGRESS`, `BLOCKED`, `DONE`.

## Phase 0 — Foundation and technical spike
- [x] 0.1. Verify supported .NET / Windows App SDK / Windows SDK combination.
- [x] 0.2. Declare minimum Windows build.
- [x] 0.3. Create solution and project skeleton.
- [x] 0.4. Build minimal WinUI 3 app on Windows 10 and 11.
- [x] 0.5. Prototype editor control candidates (Comparative architectural study of Win11 Notepad, Notepads App, Scintilla, Win2D).
- [x] 0.6. Measure large-document behavior (Documented performance thresholds & safeguards).
- [x] 0.7. Decide editor engine (ADR-0001).
- [x] 0.8. Decide packaging baseline (ADR-0002).
- [x] 0.9. Record dependency versions.

Exit criteria: a reproducible build and evidence-backed editor decision.

## Phase 1 — UI foundation
- [x] 1.1. Main window and menu.
- [x] 1.2. Tab strip.
- [x] 1.3. Editor host.
- [x] 1.4. Status bar.
- [x] 1.5. App theme switching.
- [x] 1.6. Keyboard navigation.

## Phase 2 — Core editor
- [x] 2.1. Text input and selection.
- [x] 2.2. Undo/redo and clipboard.
- [x] 2.3. Open/save/save as.
- [x] 2.4. Encoding and line endings.
- [x] 2.5. Find/replace.
- [x] 2.6. Word wrap and zoom.
- [x] 2.7. Line/column status.

## Phase 3 — Tabs and session
- [ ] 3.1. Multiple tabs.
- [ ] 3.2. Reorder and context menu.
- [ ] 3.3. Dirty-state tracking.
- [ ] 3.4. Session persistence.
- [ ] 3.5. Session restore.
- [ ] 3.6. Multiple windows.

## Phase 4 — Data protection
- [ ] 4.1. Recovery snapshots.
- [ ] 4.2. Crash recovery prompt.
- [ ] 4.3. Optional file autosave.
- [ ] 4.4. External-change detection.
- [ ] 4.5. Safe writes.
- [ ] 4.6. Backup retention.

## Phase 5 — Markdown
- [ ] 5.1. Markdig integration.
- [ ] 5.2. Syntax highlighting.
- [ ] 5.3. Live preview.
- [ ] 5.4. Split view.
- [ ] 5.5. Safe preview policy.
- [ ] 5.6. Scroll synchronization.

## Phase 6 — Themes
- [ ] 6.1. Independent app/editor themes.
- [ ] 6.2. Built-in palettes.
- [ ] 6.3. Theme JSON schema.
- [ ] 6.4. Import/export.
- [ ] 6.5. Theme validation and contrast review.

## Phase 7 — Polish
- [ ] 7.1. Settings UI.
- [ ] 7.2. Recent files and favorites.
- [ ] 7.3. Localization.
- [ ] 7.4. Focus mode/full screen.
- [ ] 7.5. Always on top.
- [ ] 7.6. Accessibility pass.

## Phase 8 — Test and optimize
- [ ] 8.1. Unit/integration/UI tests.
- [ ] 8.2. Large-file tests.
- [ ] 8.3. Performance profiling.
- [ ] 8.4. Memory profiling.
- [ ] 8.5. Windows 10/11 matrix.
- [ ] 8.6. Security review of Markdown preview.

## Phase 9 — Release
- [ ] 9.1. MSIX packaging.
- [ ] 9.2. EXE installer.
- [ ] 9.3. Portable mode decision.
- [ ] 9.4. Upgrade/uninstall tests.
- [ ] 9.5. Release notes.
- [ ] 9.6. GitHub release.
