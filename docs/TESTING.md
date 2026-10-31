# Testing Strategy

## Test layers

### Unit tests
- Encoding detection and round trips.
- Line-ending detection and preservation.
- Document dirty-state transitions.
- Theme schema validation.
- Settings serialization.
- Backup retention.
- Recovery snapshot serialization.
- Markdown pipeline configuration and sanitization rules.

### Integration tests
- Open/edit/save workflows.
- External modification detection.
- Safe-write failure recovery.
- Session restore.
- Crash recovery.
- Markdown preview update ordering.
- Settings migration between schema versions.

### UI tests
- New/open/save/close.
- Dirty tab confirmation.
- Tab switching/reordering.
- Find/replace.
- Theme switching.
- Markdown split view.
- Keyboard navigation and focus.

## Test files
Include fixtures for:
- UTF-8 with and without BOM.
- UTF-16 LE/BE.
- CRLF and LF.
- Mixed line endings.
- Empty file.
- Very long line.
- Unicode and combining characters.
- Large text files.
- Markdown with tables, task lists, code blocks, links, images, raw HTML, and hostile payloads.

## Performance tests
Test 1 KB, 100 KB, 1 MB, 5 MB, 10 MB documents. Record:
- Startup time.
- Open/save time.
- Memory footprint.
- Typing latency.
- Scroll responsiveness.
- Preview update latency.

Use repeatable hardware/software conditions. Performance goals are targets, not guarantees.

## Release gate
- Clean build.
- All required tests pass.
- No known critical/high data-loss or security bugs.
- Windows 10 and Windows 11 smoke tests.
- Both installer types tested.
- Upgrade and uninstall tested.
- User data preserved after uninstall.
