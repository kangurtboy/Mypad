# Markdown Design

## Parser
Use Markdig with a pinned stable package version. Configure only required extensions; document the exact pipeline.

## Supported syntax
- CommonMark core.
- GitHub Flavored Markdown features selected for MyPad: tables, task lists, strikethrough, autolinks, fenced code.
- Raw HTML policy must be explicitly configured and tested.

## UX modes
1. Editor only.
2. Split editor + preview (default for Markdown when preview is enabled).
3. Preview only.

Split view starts with editor on the left and preview on the right. The splitter is resizable.

## Live update behavior
- Debounce rendering after text changes.
- Cancel obsolete parse/render work.
- Ensure results are applied in document-version order.
- Avoid reparsing on every individual keystroke if a short debounce can coalesce edits.
- Keep typing responsive when rendering large Markdown files.

## Security
Markdown is untrusted content.
- Disable JavaScript in the preview surface.
- Disable automatic remote image/resource loads.
- Restrict navigation and external links; open links only through explicit user action and an approved external-launch path.
- Sanitize HTML or disable raw HTML.
- Do not allow Markdown content to access arbitrary local files.
- Test script tags, event handlers, `javascript:` URLs, data URLs, malformed HTML, and path traversal attempts.

## Images and links
Local images may be supported through a narrowly scoped, user-approved resource policy. Do not grant broad filesystem access to the preview surface.
External images remain blocked by default.

## Scroll synchronization
Optional setting. Use approximate source-to-preview mapping; do not promise exact mapping for complex nested Markdown.
