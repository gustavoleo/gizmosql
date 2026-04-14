# CaptureRunner Current Status

This file is the handoff note for the next development session.

## Current Baseline

- Core scanner baseline is still the curated master pair:
  - [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json)
  - [analyticscreator-master-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-master-report.json)
- Snagit integration is now real, not theoretical.
- `CaptureRunner` can watch Snagit `.snagx` output, extract the primary PNG, and save a managed screenshot file.

## Confirmed Working

### One-row Snagit proof

- Plan:
  - [wave1-snagit-poc-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-snagit-poc-plan.json)
- Report:
  - [wave1-snagit-poc-report-snagx.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-snagit-poc-report-snagx.json)
- Managed screenshot:
  - [1.6.18-parameters.png](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures/1.6.18-parameters.png)

Result:

- `1.6.18 Parameters` reached `full / high`
- Snagit package output was detected in `C:\Users\lainoborgo\Documents\Snagit`
- the primary PNG was extracted successfully into the managed output folder

### First-5 Wave 1 batch

- Plan:
  - [wave1-first5-snagit-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-first5-snagit-plan.json)
- Report:
  - [wave1-first5-snagit-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-first5-snagit-report.json)
- Managed screenshot:
  - [1.6.23-scripts.png](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures-wave1-first5/1.6.23-scripts.png)

Result:

- `1.6.23 Scripts` succeeded end to end as `full / high`
- `1.6.6 Connectors` timed out
- `1.6.3 Layers` timed out
- `1.6.17 Packages` timed out
- `1.6.18 Parameters` timed out in the batch plan even though the one-row proof works

## What This Means

- Snagit is no longer the blocker.
- The remaining blocker is navigation fidelity for Wave 1 list screens.
- The current `tree_path` model is strong enough for some screens, but not enough for the first batch of list-family routes.

## Important Runtime Facts

- Use the Snagit watch folder:
  - `C:\Users\lainoborgo\Documents\Snagit`
- Your working preset is:
  - `ac_refguide_4k_png`
- Current trigger path is:
  - `Ctrl+Shift+Alt+5`
  - this relies on `Repeat last capture`
- `CaptureRunner` now supports `.snagx` packages automatically.

## Do Not Repeat

- Do not patch Snagit live runtime state in:
  - `C:\Users\lainoborgo\AppData\Local\TechSmith\Snagit\25\Presets7.xml`
- That caused Snagit to show `Unable to capture`.
- Editing the exported preset file in Documents is safe.
- The active capture preset still needs to be managed through Snagit UI unless we later discover a supported runtime format.

## Next Development Step

Implement a stronger navigation/action model for list screenshots.

Recommended shape:

- keep `tree_path` for the primary route
- add one explicit post-navigation action list for things like:
  - `List Parameters`
  - `List object scripts`
  - secondary menu/button invocations
  - context actions after selecting the family node

That work should start in:

- [InputPlan.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/InputPlan.cs)
- [NavigationEngine.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/NavigationEngine.cs)
- [Validator.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/Validator.cs)
- [TreeService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/TreeService.cs)

## Good Resume Point For Tomorrow

1. Extend the plan schema with an explicit action step after tree navigation.
2. Re-author the first-5 Wave 1 plan using those actions.
3. Re-run the same first-5 batch with Snagit enabled.
4. Promote any rows that pass into the production capture lane.
