# CaptureRunner

`CaptureRunner` is a Windows-only `.NET 8` console application for `AnalyticsCreator` UI automation, validation, and screenshot generation.

The current production lane is:

1. start or attach to `AnalyticsCreator`
2. accept the saved-password login if needed
3. select and verify the `Northwind` repository
4. traverse the reachable UI with UI Automation
5. capture native PNG screenshots with `600 DPI` metadata
6. write machine-readable evidence and human-readable maps

`--map-ui` is the primary automation mode. It forces the native PNG capture backend and fails closed when bootstrap or validation evidence is incomplete. Legacy plan execution and Snagit-assisted capture still exist, but they are no longer the primary path.

## Current verified baseline

Latest fresh cold-start verification:

- startup evidence: [Output/bfs-production-fresh3/startup-state.json](Output/bfs-production-fresh3/startup-state.json)
- UI map JSON: [Output/bfs-production-fresh3/ui-map.json](Output/bfs-production-fresh3/ui-map.json)
- UI map Markdown: [Output/bfs-production-fresh3/UiMap.md](Output/bfs-production-fresh3/UiMap.md)
- coverage summary: [Output/bfs-production-fresh3/coverage-summary.json](Output/bfs-production-fresh3/coverage-summary.json)
- rejected routes: [Output/bfs-production-fresh3/rejected-routes.json](Output/bfs-production-fresh3/rejected-routes.json)
- remaining queued routes: [Output/bfs-production-fresh3/remaining-queued-routes.json](Output/bfs-production-fresh3/remaining-queued-routes.json)
- promotion summary: [Output/bfs-production-fresh3/route-promotion-summary.json](Output/bfs-production-fresh3/route-promotion-summary.json)

Result:

- `32` discovered screens
- `32` accepted screens
- `0` review
- `0` rejected
- `0` blocked
- `accepted_ratio = 1.0`

## Modes

### 1. Bootstrap-only smoke test

Use this to prove startup, login handling, repository selection, and repository verification before any traversal work.

Outputs:

- `startup-state.json`

### 2. UI mapping and screenshot generation

Use this for automatic discovery and screenshot production.

Outputs:

- `ui-map.json`
- `UiMap.md`
- `coverage-summary.json`
- `rejected-routes.json`
- `remaining-queued-routes.json`
- `route-promotion-summary.json`
- per-screen evidence bundles under the requested screenshot root, bucketed by readiness such as `accepted`

### 3. Legacy authored-plan execution

Use this when you need to run a curated `*-plan.json` file against a fixed set of screens. This path still supports both native PNG capture and optional Snagit-assisted capture.

### 4. Discovery-only snapshot

Use this to dump the current shell or dialog state before writing plans or route recipes.

## Documentation

- [AuthoringGuide.md](AuthoringGuide.md) - authoring rules for plans, bootstrap profiles, route recipes, and route promotion
- [CurrentStatus.md](CurrentStatus.md) - current handoff note and latest verified run
- [QuickStart_EN.md](QuickStart_EN.md) - common commands for smoke, mapper, and larger runs
- [NavigationMap.md](NavigationMap.md) - curated workbook-oriented navigation reference
- [ScreenshotIntegrationPlan.md](ScreenshotIntegrationPlan.md) - current capture architecture and evidence contract
- [Profiles/northwind-bootstrap.json](Profiles/northwind-bootstrap.json) - default bootstrap profile for saved-password `Northwind`
- [Profiles/high-value-route-recipes.json](Profiles/high-value-route-recipes.json) - seeded multi-step routes for high-value dialogs and wizards

## Project layout

```text
CaptureRunner/
  Models/
  Output/
  Profiles/
  Runner/
  Services/
  Program.cs
```

## Build

From Windows:

```powershell
dotnet build .\CaptureRunner\CaptureRunner.csproj
```

From this WSL workspace against Windows `dotnet`:

```bash
cmd.exe /c dotnet build E:\DDD\GitHub\gizmosql\CaptureRunner\CaptureRunner.csproj
```

## Common commands

### Bootstrap-only smoke

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --bootstrap-only `
  --bootstrap-profile .\CaptureRunner\Profiles\northwind-bootstrap.json `
  --repository-name Northwind `
  --startup-state-output .\CaptureRunner\Output\startup-state.json `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --keep-open
```

### Fresh UI mapper run

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --map-ui `
  --bootstrap-profile .\CaptureRunner\Profiles\northwind-bootstrap.json `
  --repository-name Northwind `
  --route-recipes .\CaptureRunner\Profiles\high-value-route-recipes.json `
  --ui-map-output .\CaptureRunner\Output\bfs-production-fresh3\ui-map.json `
  --ui-map-markdown-output .\CaptureRunner\Output\bfs-production-fresh3\UiMap.md `
  --startup-state-output .\CaptureRunner\Output\bfs-production-fresh3\startup-state.json `
  --screenshot-output-root .\CaptureRunner\Output\screenshots-bfs-production-fresh3 `
  --map-max-screens 100 `
  --map-max-depth 2 `
  --map-branching-factor 50 `
  --map-traversal-timeout-ms 600000 `
  --accepted-threshold 0.98 `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --keep-open
```

### Larger all-screens run

Use this after the smoke and fresh production run are clean. The route-promotion rule remains the same: only promote routes that appear in `rejected-routes.json` or `remaining-queued-routes.json`.

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --map-ui `
  --bootstrap-profile .\CaptureRunner\Profiles\northwind-bootstrap.json `
  --repository-name Northwind `
  --route-recipes .\CaptureRunner\Profiles\high-value-route-recipes.json `
  --ui-map-output .\CaptureRunner\Output\bfs-production-large\ui-map.json `
  --ui-map-markdown-output .\CaptureRunner\Output\bfs-production-large\UiMap.md `
  --startup-state-output .\CaptureRunner\Output\bfs-production-large\startup-state.json `
  --screenshot-output-root .\CaptureRunner\Output\screenshots-bfs-production-large `
  --map-max-screens 200 `
  --map-max-depth 2 `
  --map-branching-factor 75 `
  --map-traversal-timeout-ms 900000 `
  --accepted-threshold 0.98 `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --keep-open
```

### Authored plan run with native PNG capture

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\sample-plan.json `
  --output .\CaptureRunner\Output\report.json `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --capture-backend native `
  --native-capture-area client `
  --png-dpi 600 `
  --capture-output-dir .\CaptureRunner\Output\captures-native `
  --keep-open
```

## Runtime rules

- Run in an interactive Windows desktop session. UIA scanning is not reliable from a locked or disconnected session.
- Keep exactly one `AnalyticsCreator.exe` instance running.
- Treat `Northwind` bootstrap verification as mandatory for automated mapping and screenshot generation.
- `--map-ui` always uses native PNG capture and defaults `--png-dpi` to `600`.
- The mapper resets to the verified shell between queued routes and explicitly closes dialogs, wizards, and detail surfaces after capture.
- Do not promote a new route because it "looks right". Promote it only when it appears in `rejected-routes.json` or `remaining-queued-routes.json` from a fresh run.
- Snagit remains optional for legacy plan-driven runs. It is not the default path for production mapping.
