# ADR-0001: Editor Engine Selection

- Status: Proposed
- Date: 2026-10-31

## Context
MyPad requires a native-feeling editor with plain-text fidelity, undo/redo, selection, IME input, line numbers, current-line highlighting, Markdown syntax highlighting, and acceptable performance for files up to the initial target size.

## Options
1. WinUI TextBox.
2. WinUI RichEditBox.
3. Custom text surface/control.
4. Third-party editor component.

## Decision
Deferred until Phase 0 technical spike. The product requirement is a custom MyPad editor component, but the underlying text engine must be selected using measured evidence.

## Required evidence
See `docs/EDITOR_ENGINE.md`.

## Consequences
No production editor implementation should depend on an untested assumption. Update this ADR with measurements, selected option, known limitations, and approval before Phase 2.
