# Build and Release

## Phase 0 prerequisite
Do not hard-code a .NET or Windows App SDK version based on assumptions. Verify the current stable supported combination and minimum Windows version using official Microsoft documentation, then record the chosen versions in the project and this document.

## Build configurations
- Debug
- Release

## Build requirements
- Reproducible restore.
- Nullable reference types enabled.
- Warnings reviewed; do not suppress broadly.
- Unit tests run in CI.
- Build output excluded from Git.

## Packaging
### MSIX
Validate package identity, signing, dependencies, file associations, install/upgrade/uninstall, and data preservation.

### EXE
Use a maintained installer technology. The installer must detect prerequisites, offer shortcuts and optional file associations, support silent install only if documented, and uninstall without deleting user documents.

## Release checklist
- [ ] Version updated.
- [ ] Changelog updated.
- [ ] Release build succeeds.
- [ ] Tests pass.
- [ ] Windows 10 smoke test passes.
- [ ] Windows 11 smoke test passes.
- [ ] MSIX installs and launches.
- [ ] EXE installs and launches.
- [ ] Upgrade path tested.
- [ ] Uninstall preserves user data.
- [ ] Checksums generated.
- [ ] Release notes published.
