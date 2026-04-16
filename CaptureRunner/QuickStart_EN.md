# CaptureRunner Quick Start

## Purpose

Use `CaptureRunner` to bootstrap `AnalyticsCreator`, verify `Northwind`, map reachable screens, and generate native PNG screenshots with evidence.

The main automation mode is `--map-ui`.

## Prerequisites

- Windows desktop session, not locked and not disconnected
- exactly one `AnalyticsCreator.exe`
- saved password available for the login flow
- `Northwind` repository available in the selector
- certified monitor/environment profile if you want strict preflight

## Build

```powershell
dotnet build .\CaptureRunner\CaptureRunner.csproj
```

## 1. Bootstrap smoke test

Run this first. It proves startup, login handling, repository selection, and repository verification without attempting traversal.

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

Expected result:

- `startup-state.json.repository_verified = true`
- shell title contains `[Northwind]`

## 2. Fresh mapper run

This is the normal automatic UI mapping and screenshot pass.

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --map-ui `
  --bootstrap-profile .\CaptureRunner\Profiles\northwind-bootstrap.json `
  --repository-name Northwind `
  --route-recipes .\CaptureRunner\Profiles\high-value-route-recipes.json `
  --ui-map-output .\CaptureRunner\Output\bfs-production-fresh\ui-map.json `
  --ui-map-markdown-output .\CaptureRunner\Output\bfs-production-fresh\UiMap.md `
  --startup-state-output .\CaptureRunner\Output\bfs-production-fresh\startup-state.json `
  --screenshot-output-root .\CaptureRunner\Output\screenshots-bfs-production-fresh `
  --map-max-screens 100 `
  --map-max-depth 2 `
  --map-branching-factor 50 `
  --map-traversal-timeout-ms 600000 `
  --accepted-threshold 0.98 `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --keep-open
```

Generated artifacts:

- `startup-state.json`
- `ui-map.json`
- `UiMap.md`
- `coverage-summary.json`
- `rejected-routes.json`
- `remaining-queued-routes.json`
- `route-promotion-summary.json`
- screenshot evidence under the requested screenshot root

## 3. Larger all-screens run

Run this after the fresh mapper run is clean and you want a broader frontier.

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

After the run:

1. inspect `rejected-routes.json`
2. if empty, inspect `remaining-queued-routes.json`
3. use `route-promotion-summary.json` to group only those real stalls

## 4. Legacy authored-plan run

Use this when you need a fixed row set instead of the automatic mapper.

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

## Rules

- `--map-ui` always uses the native PNG backend and defaults `--png-dpi` to `600`.
- The mapper fails closed when bootstrap cannot verify `Northwind`.
- Do not promote routes from memory. Promote them only from the rejection or queue artifacts of a fresh run.
- Keep Snagit as optional only. It is not the default capture path.
