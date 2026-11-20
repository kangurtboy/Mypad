# MyPad

**A lightweight native Windows text editor with Markdown live preview and customizable themes.**

MyPad aims to combine the simplicity of Windows Notepad with reliable tabs, crash recovery, Markdown authoring, and a flexible appearance system.

## Product goals
- Native Windows desktop experience using C# and WinUI 3.
- Windows 10 and Windows 11 support, subject to the validated SDK support matrix.
- Fast startup and low resource use.
- Plain-text-first editing with encoding and line-ending control.
- Tabs, session restore, crash recovery, optional file autosave, and backups.
- Markdown editing with live split preview.
- Separate application and editor themes.
- Offline-first, local-only operation.

## Installation

Download the latest version from the [**GitHub Releases**](https://github.com/kangurtboy/Mypad/releases) page:

1. **Installer (`MyPad-v0.1.0-Setup-x64.exe`)**:
   - Modern setup wizard with automatic desktop & start menu shortcuts.
   - Registers Windows Explorer context menu (*"Открыть в MyPad"* / *"Edit with MyPad"*).
   - Associates `.txt`, `.md`, `.markdown`, `.log` files.
   - Enables `Win+R mypad` quick-launch.
   - Clean uninstaller included.

2. **Portable ZIP (`MyPad-v0.1.0-win-x64-portable.zip`)**:
   - Zero-install standalone archive. Extract anywhere and launch `MyPad.App.exe`.

---

## Current status
**Planning / foundation stage.** This repository contains the product specification, agent instructions, architecture notes, decision records, and project scaffolding. The editor engine and Windows SDK baseline must pass the Phase 0 prototype before the production UI is built.

## Repository layout
```text
src/                 Application, Core, and Infrastructure projects
tests/               Unit, integration, and UI tests
docs/                Product and engineering documentation
installer/           MSIX and EXE installer projects
.github/             CI and issue templates
```

## Getting started
See:
- [`AGENTS.md`](AGENTS.md) — instructions for coding agents.
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — phased implementation plan.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture and module boundaries.
- [`docs/decisions/`](docs/decisions/) — architecture decision records.

The exact .NET SDK, Windows App SDK, Windows SDK, and minimum Windows build must be selected and recorded during Phase 0 after checking official compatibility documentation.

## Product decisions
- Product name: **MyPad**
- Markdown: editor + live preview (split view)
- Editor: custom MyPad editor component; underlying control/engine selected after a measured prototype
- Distribution: MSIX and conventional EXE installer
- Application theme: System / Light / Dark
- Editor theme: independently selectable light and dark palettes
- Storage: local-only

## Security and privacy
MyPad must not require an account, upload documents, include advertising SDKs, or enable hidden telemetry. Markdown preview treats content as untrusted input and must not execute scripts or automatically load remote resources.

## Building from source
Prerequisites: .NET 8.0 SDK, Windows App SDK, and Windows 10/11 (x64).

```powershell
# Clone repository
git clone https://github.com/kangurtboy/Mypad.git
cd Mypad

# Run automated tests
dotnet test

# Build release artifacts (Installer + Portable ZIP)
powershell -ExecutionPolicy Bypass -File .\tools\build-release.ps1

# Or 1-click install/update on local PC
powershell -ExecutionPolicy Bypass -File .\tools\deploy.ps1
```

## Contributing
Follow `AGENTS.md`. Keep changes small, tested, documented, and compatible with the supported Windows matrix.
