# UI Guidelines

## Visual direction
Modern Fluent Design, understated and familiar. Avoid visual clutter, excessive gradients, oversized cards, and permanent sidebars.

## Main window
- Top: app menu/title area and document tabs.
- Center: editor, optionally split with Markdown preview.
- Bottom: compact status bar.
- Settings: dedicated settings page/window.

## Menus
File, Edit, View, Format, Settings, Help.

## Appearance
- App theme: System / Light / Dark.
- Editor theme: separate light/dark selection.
- Follow Windows theme changes without restart when possible.
- Respect system text scaling and high contrast where supported.

## Interaction
- Keyboard shortcuts should be discoverable.
- Dirty state must be visible but subtle.
- Avoid modal dialogs unless data safety requires them.
- Use clear Save / Don't Save / Cancel wording.
- Keep focus behavior predictable after dialogs and tab changes.

## Accessibility
- Every actionable control has an accessible name.
- Keyboard focus is visible.
- Do not communicate state by color alone.
- Support keyboard navigation through menus, tabs, editor, and settings.
- Test with Windows Narrator where feasible.

## Responsive layout
At narrow window widths, collapse low-priority controls into menus. The editor remains the dominant surface.
