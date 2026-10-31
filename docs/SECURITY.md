# Security and Privacy Requirements

## Privacy
- Local-first operation.
- No account or cloud requirement.
- No advertising.
- No hidden telemetry.
- No document content in logs.
- No network calls from document rendering by default.

## Markdown preview threat model
Markdown and embedded HTML are untrusted.
- Disable script execution.
- Disable automatic remote resource loading.
- Restrict navigation.
- Sanitize or disable raw HTML.
- Restrict local file access.
- Validate URL schemes before external launch.
- Test malicious HTML, event handlers, `javascript:` URLs, data URLs, malformed markup, and path traversal.

## File safety
- Treat file names and paths as untrusted.
- Handle symlinks/reparse points carefully where relevant.
- Detect external modification.
- Avoid unsafe overwrite.
- Preserve recovery snapshots on failed writes.
- Do not delete user content as part of cleanup.

## Dependencies
- Minimize dependencies.
- Pin versions.
- Review license and maintenance status.
- Update vulnerable packages.
