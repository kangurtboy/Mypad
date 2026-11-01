# ADR-0002: Packaging Strategy

- Status: Accepted
- Date: 2026-11-01

## Context
MyPad must provide both a conventional standalone EXE / installer and MSIX packaging for Windows 10 and Windows 11.

## Decision
Configure `MyPad.App` with `<WindowsPackageType>None</WindowsPackageType>` as the core development and standalone deployment baseline:
1. **Unpackaged (Standard EXE):**
   - Direct execution via `MyPad.App.exe`.
   - Allows portable mode (writing configuration locally or to `%LocalAppData%`).
   - Standard Windows installer (Inno Setup / WiX).
2. **Packaged (MSIX):**
   - Supported via Windows App SDK single-project MSIX or Windows Application Packaging Project for Store distribution.

Platform-specific storage and path resolution are abstracted behind `ISettingsService` and `IRecoveryService` in `MyPad.Core`, ensuring zero hardcoded dependencies on packaging models.

## Consequences
- Fast local F5 debug cycle without requiring MSIX developer mode certificates.
- Seamless compatibility with both portable and packaged distributions.
