# Theme System

## Independent layers
1. Application theme: System / Light / Dark.
2. Editor palette: independent light palette and dark palette.
3. Optional custom editor themes.

## System behavior
When application theme is System, observe Windows theme changes and update app resources. The editor chooses the user-configured palette corresponding to the active light/dark state.

When application theme is forced Light or Dark, use that state to select the matching editor palette unless the user has explicitly selected an independent editor mode. Document final precedence in implementation.

## Initial palette catalogue
Light:
- Default Light
- GitHub Light
- Solarized Light
- Catppuccin Latte
- Gruvbox Light

Dark:
- Default Dark
- One Dark
- Dracula
- Nord
- Catppuccin Mocha
- Gruvbox Dark
- Monokai

## Theme schema
Version the JSON schema. Required fields:
- schemaVersion
- name
- appearance (`light` or `dark`)
- colors (background, foreground, selection, cursor, currentLine, lineNumber, keyword, string, comment, heading, link)

Optional token colors may be added compatibly.

## Validation
- Validate hex colors.
- Reject missing required fields and unknown incompatible schema versions.
- Limit file size.
- Never execute content from theme files.
- Import should be transactional: invalid theme must not replace an existing theme.
- Export must produce stable, readable JSON.

## Accessibility
Check text/background contrast for editor text, selected text, line numbers, and syntax tokens. Do not rely on color alone to communicate document state.
