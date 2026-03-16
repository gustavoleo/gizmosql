# Windows Install Guide

## Recommended download

Use `GizmoSQL-Setup-x64.exe` as the default Windows installer.

This Burn bundle chains:

1. `GizmoSQL-Core-x64.msi`
2. `GizmoSQL-UI-x64.msi`
3. `GizmoSQL-PowerBI-Setup-x64.msi` when selected

The bundle keeps the individual MSI artifacts available for advanced/manual installs.

## What the installer does

`GizmoSQL-Core-x64.msi` installs:

- `gizmosql_server.exe`
- `gizmosql_client.exe`
- VC++ runtime DLLs required for DuckDB extensions
- `quickstart.html`
- `launch-demo-server.ps1`
- `gizmosql-shell.cmd`
- Start Menu shortcuts for Quickstart, Demo Server, and UI handoff
- the system PATH entry for `C:\Program Files\GizmoSQL\`

The sample database is installed by default to:

`C:\ProgramData\GizmoSQL\samples\gizmosql-demo.duckdb`

## Current packaging model

- Core MSI authoring lives in `installer/GizmoSQL.wxs`.
- The recommended Windows bundle authoring lives in `installer-bundle/GizmoSQL.Bundle.wxs`.
- Windows CI/signing lives in `.github/workflows/ci.yml`.
- The branch-only Windows packaging smoke test lives in `.github/workflows/test-msi.yml`.
- Signing uses `AzureSignTool` with Azure Key Vault-backed certificate material.
- The Burn bundle uses a custom WixStdBA theme so the options page exposes the installer choices directly and the success page can launch the selected post-install action.

## Package acquisition strategy

Phase 1 uses **fetch release assets in CI** for the external MSIs:

- `scripts/fetch-ui-msi.ps1`
- `scripts/fetch-powerbi-msi.ps1`

The GitHub Actions workflow expects repository variables:

- `WINDOWS_UI_MSI_URL`
- `WINDOWS_POWERBI_MSI_URL`

This keeps the UI and Power BI connector release flows independent while making the bundle the single recommended evaluator path.

## Install behavior

- Core is required.
- Sample Database is on by default.
- GizmoSQL UI is on by default.
- Power BI Connector is optional and recommended for Power BI users.
- The bundle success page defaults to `Open GizmoSQL UI` and can also launch the demo server or quickstart guide.
- The `Open GizmoSQL UI` handoff attempts to start the bundled demo server automatically before opening the UI, so the default path stays within the 8-step evaluator goal without modifying UI internals.

The first-run surface on Windows is the GizmoSQL UI plus the installed quickstart and demo-launcher assets.

## Signing flow

- `GizmoSQL-Core-x64.msi` is built, then signed directly.
- `GizmoSQL-Setup-x64-unsigned.exe` is built first.
- CI detaches the Burn engine with `wix burn detach`, signs the detached engine, reattaches it with `wix burn reattach`, then signs the final `GizmoSQL-Setup-x64.exe`.

That preserves the standard Burn signing model instead of treating the bundle like a plain EXE.

## Upgrade, repair, uninstall

- The Core MSI retains the existing per-machine install root and PATH behavior.
- `MajorUpgrade` remains in the Core MSI for upgrade coherence.
- Start Menu shortcuts are removed on uninstall.
- The sample database is installed at a stable machine-wide path and is removed when the owning Core MSI component is removed.
- The external UI and Power BI products retain their own MSI upgrade/uninstall behavior because the bundle reuses their published MSI packages instead of repackaging them.
