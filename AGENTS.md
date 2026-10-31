# AGENTS.md — MyPad

## Mission
Build MyPad, a lightweight, native Windows text editor inspired by Windows Notepad, with reliable plain-text editing, tabs, recovery, Markdown live preview, and independently configurable application/editor themes.

## Non-negotiable product principles
1. Preserve user data above all else.
2. Prefer native Windows UI and predictable behavior.
3. Keep the application responsive; never perform expensive file I/O, parsing, or rendering on the UI thread.
4. Work offline by default. No accounts, ads, cloud sync, hidden telemetry, or document uploads.
5. Keep the default experience simple. Advanced features must not clutter the primary editor.
6. Maintain Windows 10 and Windows 11 compatibility within the explicitly validated support matrix.
7. Do not claim a feature is complete until it is implemented, tested, and documented.

## Before every task
1. Read this file and the relevant documents under `docs/`.
2. Inspect the existing solution and current Git state. Do not overwrite user changes.
3. Identify the acceptance criteria and affected modules.
4. For architectural choices, write down alternatives, trade-offs, and the reason for the decision in `docs/decisions/`.
5. Prefer small, reviewable changes over broad rewrites.

## Development workflow
For each task:
1. Summarize the intended change and implementation plan.
2. Implement the smallest coherent increment.
3. Add or update automated tests.
4. Build the solution and run relevant tests.
5. Manually verify UI behavior when automation cannot cover it.
6. Update documentation, roadmap, and changelog where applicable.
7. Inspect `git diff`; ensure no secrets, local files, generated binaries, or user documents are included.
8. Commit only when explicitly requested or when the user has authorized autonomous commits.

## Engineering rules
- Use C# with nullable reference types enabled.
- Use async APIs for file I/O and other potentially slow operations.
- Avoid `.Result`, `.Wait()`, and synchronous blocking on UI paths.
- Keep business logic out of XAML code-behind wherever practical.
- Use MVVM and dependency injection consistently; do not introduce abstractions without a clear need.
- Keep Core independent of WinUI and Windows UI types.
- Keep file encoding and line-ending behavior explicit and testable.
- Never silently discard text, overwrite external edits, or swallow exceptions.
- Do not store document contents in logs.
- Treat all Markdown and file contents as untrusted input.
- Markdown preview must not execute scripts or load remote resources automatically.
- Avoid unnecessary NuGet dependencies. Record each new dependency and its purpose.
- Do not invent APIs. Verify framework/API availability against the project's target SDK.
- Do not use preview/beta packages in production unless explicitly approved.

## Editor-engine gate
The editor control is an unresolved technical decision until Phase 0 is complete.
- Prototype candidate native controls (including RichEditBox and TextBox where appropriate).
- Verify plain-text fidelity, undo/redo, selection, IME/input behavior, large-file responsiveness, line numbering/highlighting feasibility, encoding round-trips, and Markdown highlighting.
- Do not assume RichEditBox is suitable merely because it is available.
- Do not store rich-text markup in user text files.
- Record the decision and test evidence in `docs/decisions/ADR-0001-editor-engine.md`.
- If native controls cannot meet acceptance criteria, evaluate a custom control strategy before introducing a third-party editor engine.

## Data safety requirements
- Maintain a recovery journal or equivalent crash-recovery mechanism.
- Distinguish session recovery from saving to the original file.
- Detect external file modifications before writing.
- Use safe write/replace patterns and preserve recoverable data on failure.
- Make backup retention configurable.
- Test interrupted writes and conflict scenarios.
- Ask before destructive actions.

## Compatibility gate
Before choosing .NET, Windows App SDK, packaging, or APIs:
- Verify current supported versions and minimum Windows build from official Microsoft documentation.
- Declare the minimum supported Windows version in one central project configuration.
- Test the actual packaged and unpackaged deployment paths.
- Document any Windows 10 limitations and fallbacks.
- Do not promise Windows 10 1809 support until verified against the chosen SDK/runtime.

## Definition of done
A task is done only when:
- Acceptance criteria are met.
- Build succeeds for the affected configuration.
- Relevant tests pass.
- No known data-loss or security regression is introduced.
- Docs and roadmap are updated.
- Remaining limitations are clearly stated.
