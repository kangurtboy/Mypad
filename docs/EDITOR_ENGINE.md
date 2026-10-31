# Editor Engine — Phase 0 Technical Spike

## Purpose
Select a reliable native editing foundation before committing to the full UI.

## Candidates
Evaluate at minimum:
- WinUI `TextBox` (multi-line).
- WinUI `RichEditBox`.
- A custom text surface built on lower-level text/input primitives, only if required.

Do not add a large third-party editor dependency without a documented evaluation.

## Required spike scenarios
1. Plain-text fidelity: no hidden formatting or altered characters.
2. Undo/redo correctness after syntax highlighting updates.
3. Selection, caret, keyboard navigation, clipboard.
4. IME and international keyboard input.
5. Unicode supplementary characters and combining marks.
6. CRLF/LF round-trip behavior.
7. UTF-8 and UTF-16 round trips.
8. 1 MB, 5 MB, and 10 MB documents.
9. Typing latency and scrolling performance.
10. Line number gutter feasibility.
11. Current-line highlight feasibility.
12. Markdown token highlighting without corrupting text.
13. High DPI and text scaling.
14. Accessibility and focus behavior.

## Measurement
Record:
- Cold and warm initialization.
- Memory after opening each test document.
- Time to open and save.
- Typing latency under a repeatable test.
- UI responsiveness while preview parsing runs.
- Known limitations.

## Decision
Document the winner and rationale in `decisions/ADR-0001-editor-engine.md`.

## Important implementation constraints
- Canonical document content remains plain text.
- Highlighting must be presentation-only.
- Do not rewrite the entire document formatting on every keystroke.
- Preserve undo/redo history.
- If the selected native control cannot meet requirements, propose a custom control architecture with a small prototype before production implementation.
