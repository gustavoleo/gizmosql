# CaptureRunner AGENTS

This file scopes future agent work inside `/mnt/e/DDD/GitHub/gizmosql/CaptureRunner`.

## Purpose

`CaptureRunner` is a Windows-only WPF automation capability scanner plus a screenshot capture harness layered on top of validated navigation.

It is not yet a full production screenshot engine.

## Current Truth

- The scanner baseline is stable enough to produce curated automation reports.
- Snagit integration works when Snagit writes `.snagx` packages into:
  - `C:\Users\lainoborgo\Documents\Snagit`
- `CaptureRunner` extracts the primary PNG from those packages.
- One-row Snagit proof is successful:
  - [wave1-snagit-poc-report-snagx.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-snagit-poc-report-snagx.json)
- First-5 Wave 1 batch is only partially successful:
  - [wave1-first5-snagit-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-first5-snagit-report.json)

## Working Assumptions

- There should be exactly one `AnalyticsCreator.exe` instance.
- `SnagitCapture.exe` should already be installed at:
  - `C:\Program Files\TechSmith\Snagit\SnagitCapture.exe`
- The active repeat-last-capture path depends on Snagit UI state.
- The exported Snagit preset file is safe to edit:
  - `C:\Users\lainoborgo\Documents\SnaggitPresets.snagpresets`
- The live Snagit runtime config is not safe to patch casually:
  - `C:\Users\lainoborgo\AppData\Local\TechSmith\Snagit\25\Presets7.xml`

## Immediate Next Priority

Do not spend more time on Snagit transport unless it regresses.

The next priority is navigation-model work for list-family screenshots:

- `1.6.6 Connectors`
- `1.6.3 Layers`
- `1.6.17 Packages`
- `1.6.18 Parameters`

The current `tree_path` model is too weak for those routes.

## Recommended Implementation Direction

Add explicit post-navigation actions to the plan schema.

Examples:

- invoke a secondary item such as `List Parameters`
- click a named button after selecting a tree node
- invoke a menu or list command after a primary route

Likely edit points:

- [InputPlan.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/InputPlan.cs)
- [NavigationEngine.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/NavigationEngine.cs)
- [TreeService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/TreeService.cs)
- [Validator.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/Validator.cs)

## Development Rules

- Keep partial-report behavior intact.
- Keep one-screen-per-worker isolation intact.
- Do not remove `.snagx` support from [SnagitCaptureService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/SnagitCaptureService.cs).
- Prefer plan/schema changes over hardcoded one-off screen logic.
- Preserve the current operator notice and mouse parking behavior unless they become blockers.

## Resume Docs

Start with:

- [CurrentStatus.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/CurrentStatus.md)
- [README.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/README.md)
- [AuthoringGuide.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/AuthoringGuide.md)
- [ScreenshotIntegrationPlan.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/ScreenshotIntegrationPlan.md)
