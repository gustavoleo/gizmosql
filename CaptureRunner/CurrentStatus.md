# CaptureRunner Current Status

This is the handoff note for the next session.

## Current baseline

`CaptureRunner` now has a working production lane for `AnalyticsCreator`:

- bootstrap to saved-password login
- select and verify the `Northwind` repository
- run bounded BFS UI traversal with reset-to-shell between routes
- capture native PNG screenshots with `600 DPI` metadata
- close dialogs, wizards, and detail surfaces after capture
- write runtime map, coverage, rejection, queue, and promotion artifacts

`Snagit` remains optional for legacy plan-driven runs. It is no longer the primary screenshot path.

## Latest verified fresh run

Cold-start run:

- startup state: [Output/bfs-production-fresh3/startup-state.json](Output/bfs-production-fresh3/startup-state.json)
- UI map JSON: [Output/bfs-production-fresh3/ui-map.json](Output/bfs-production-fresh3/ui-map.json)
- UI map Markdown: [Output/bfs-production-fresh3/UiMap.md](Output/bfs-production-fresh3/UiMap.md)
- coverage summary: [Output/bfs-production-fresh3/coverage-summary.json](Output/bfs-production-fresh3/coverage-summary.json)
- rejected routes: [Output/bfs-production-fresh3/rejected-routes.json](Output/bfs-production-fresh3/rejected-routes.json)
- remaining queued routes: [Output/bfs-production-fresh3/remaining-queued-routes.json](Output/bfs-production-fresh3/remaining-queued-routes.json)
- promotion summary: [Output/bfs-production-fresh3/route-promotion-summary.json](Output/bfs-production-fresh3/route-promotion-summary.json)
- screenshots root: `CaptureRunner/Output/screenshots-bfs-production-fresh3`

Result:

- `32` discovered
- `32` accepted
- `0` review
- `0` rejected
- `0` blocked
- `accepted_ratio = 1.0`
- `promotion_source = none`

## What is now working

- startup bootstrap recognizes the saved-password login path
- repository selection uses `cmbName` and confirms `Northwind`
- mapper traversal is UIA-first and resets to shell between queued routes
- deeper BFS replay preserves alternate routes instead of losing them
- dialog and wizard routes are closed explicitly after capture
- mapper emits:
  - `ui-map.json`
  - `UiMap.md`
  - `coverage-summary.json`
  - `rejected-routes.json`
  - `remaining-queued-routes.json`
  - `route-promotion-summary.json`
- route promotion is now evidence-driven instead of inferred

## Current promotion rule

On every fresh run:

1. inspect `rejected-routes.json`
2. if empty, inspect `remaining-queued-routes.json`
3. use `route-promotion-summary.json` only as a grouped helper
4. promote only the routes that appear there

Do not add new recipe families when both artifacts are empty.

## Main remaining limits

- The mapper currently covers the safe reachable screen set proven by UIA traversal from the verified shell. It does not claim seeded-data, destructive, or manual-only surfaces unless evidence exists.
- Some context/detail pages may still need seeded data or object selection state that the current safe traversal does not synthesize.
- Any new route that leaves a modal or editor surface open must be treated as a bug until the cleanup path is explicit.

## Next clean run

The next larger test should keep the same bootstrap and promotion gate, but use a broader frontier:

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

Keep the promotion rule unchanged after that run.
