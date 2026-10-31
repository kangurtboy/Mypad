# ADR-0002: Packaging Strategy

- Status: Proposed
- Date: 2026-10-31

## Context
MyPad must provide both MSIX and a conventional EXE installer. WinUI 3 packaging and unpackaged deployment have different runtime/dependency considerations.

## Decision
Support both distribution forms, but defer exact packaging configuration until the selected Windows App SDK and deployment model are validated.

## Required validation
- Packaged MSIX install/upgrade/uninstall.
- Unpackaged EXE install and launch.
- Runtime/framework dependency handling.
- File associations.
- Windows 10 and Windows 11 compatibility.
- User data preservation on uninstall.

## Consequences
Avoid packaging-specific assumptions in Core. Keep app data locations and installer integration behind appropriate platform services.
