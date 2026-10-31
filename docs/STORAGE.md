# Storage, Autosave, Recovery, and Backups

## Storage categories
Keep preferences, session state, recovery snapshots, backups, and original files separate.

Suggested app data layout (exact Windows paths to be finalized):
```text
MyPad/
  settings/
    settings.json
    themes/
  sessions/
    session.json
  recovery/
    <document-id>.snapshot
  backups/
    ...
  logs/
```

## Session recovery
Persist:
- Session ID and timestamp.
- Open document list and tab order.
- Active document.
- File path when present.
- Unsaved text snapshot or recoverable delta.
- Cursor, selection, and scroll state.
- Encoding and line endings.

Recovery snapshots must be written safely and must not replace original user files.

## File autosave
Direct-to-file autosave is optional and off by default until user enables it. Session recovery can remain enabled independently.

Before writing:
1. Check the file still matches the last observed fingerprint (where possible).
2. If externally changed, pause and ask the user how to resolve.
3. Write to a temporary file in the same volume/directory where feasible.
4. Flush and replace using a safe platform-supported operation.
5. Preserve a recovery snapshot if any step fails.

## Encoding
Preserve detected encoding and BOM behavior. If detection is ambiguous, ask or use a documented safe default. Never silently replace undecodable bytes.

## Line endings
Detect CRLF, LF, and where relevant CR. Preserve existing style unless user explicitly changes it. Mixed line endings require a documented policy.

## Backups
Configurable enable/disable, retention count, and location. Avoid unbounded storage growth. Do not delete the only recoverable copy.

## Error cases to test
- Read-only file.
- Access denied.
- File deleted while open.
- File renamed or moved.
- Disk full.
- External edit while open.
- Process termination during save.
- Invalid encoding sequences.
- Backup directory unavailable.
