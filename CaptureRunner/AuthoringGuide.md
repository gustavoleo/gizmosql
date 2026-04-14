# CaptureRunner Authoring Guide

This guide explains how to extend `CaptureRunner` safely: how it runs, how to discover new screens, how to add new plan rows, how to choose anchors, and how to interpret the results.

Start here if you want to add:

- a new screen
- a new left-navigation node
- a new editor tab
- a toolbar, menu, or ribbon route
- a keyboard shortcut fallback
- stronger validation anchors

## What CaptureRunner Does

`CaptureRunner` is a Windows-only scanner for WPF UI automation feasibility. It is not a production execution engine. For each planned screen it:

1. attaches to or launches the target executable
2. shows an operator notice on the resolved `operator_display` from the selected environment profile
3. parks the mouse on that same certified operator display during parent runs
4. opens the target screen using the declared navigation hints
5. scans the UI Automation tree twice
6. validates that the intended screen is really active
7. classifies the result as `full`, `partial`, or `none`
8. writes a JSON report

## Runtime Model

Key runtime behavior:

- Exactly one target application instance is allowed.
  If multiple `AnalyticsCreator.exe` processes exist, the scan fails fast.
- Parent scans run each screen in its own worker process.
  This prevents one hung screen from blocking the whole batch forever.
- The parent process rewrites the output JSON after every completed row.
  A timed-out row still leaves a usable partial report.
- The operator should keep the application open and avoid using the mouse or keyboard during the run.
- During active parent runs, the cursor is parked on the resolved certified operator display. If it drifts to another display, CaptureRunner moves it back to the center of that operator display.

Relevant code:

- [Program.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Program.cs)
- [WindowService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/WindowService.cs)
- [MouseParkingService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/MouseParkingService.cs)
- [OperatorNoticeService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/OperatorNoticeService.cs)

## Command Line Options

| option | meaning |
|---|---|
| `--exe <path>` | Required. Path to the target executable. |
| `--plan <file>` | Plan JSON to scan. |
| `--discover-output <file>` | Write discovery JSON for the current app window. |
| `--output <file>` | Final or partial report path. |
| `--startup-timeout-ms <n>` | Timeout for app startup and initial window discovery. |
| `--navigation-timeout-ms <n>` | Timeout for post-navigation window settling. |
| `--screen-timeout-ms <n>` | Hard timeout per screen worker. |
| `--capture-timeout-ms <n>` | How long to wait for Snagit output. |
| `--keep-open` | Leave the target app open after the run. |
| `--no-operator-notice` | Disable the operator message window. |
| `--operator-message "<text>"` | Override the default operator message. |
| `--snagit-hotkey "<keys>"` | Trigger Snagit capture through a hotkey. |
| `--snagit-exe <path>` | Path to `SnagitCapture.exe`. |
| `--snagit-watch-dir <folder>` | Folder to watch for new Snagit output. |
| `--capture-output-dir <folder>` | Folder where managed screenshots are written. |
| `--worker-mode` | Internal mode used by the parent process. Do not use this manually. |

Typical commands:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --discover-output .\CaptureRunner\Output\discovery.json
```

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\analyticscreator-master-plan.json `
  --screen-timeout-ms 120000 `
  --output .\CaptureRunner\Output\analyticscreator-master-report.json `
  --keep-open
```

## Recommended Workflow For New Screens

Use this flow every time:

1. Run discovery on the target app state.
2. Inspect `likely_screens`, `likely_modules`, and `interesting_controls`.
3. Decide the navigation route.
4. Choose the strongest validation anchors.
5. Create one new plan row.
6. Run that single row by itself.
7. Inspect the result JSON.
8. Promote the row into a curated plan only after it is stable.

If screenshot capture is enabled, only promote the row after:

9. the scan report shows `capture.success: true`
10. the managed output screenshot lands in the requested capture output folder

## Step 1: Run Discovery

Discovery mode gives you the raw UIA surface for the current app window.

Use:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --discover-output .\CaptureRunner\Output\discovery.json `
  --keep-open
```

Look at:

- `likely_screens`
- `likely_modules`
- `interesting_controls`
- `notes`

Prefer anchors that expose a real `automation_id`.

## Step 2: Choose The Right Navigation Strategy

`CaptureRunner` uses navigation in this order:

1. `tree_navigation`
2. `keyboard`
3. `focus_change`

This is controlled by [NavigationEngine.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/NavigationEngine.cs).

Important current limitation:

- the existing plan schema can express only `tree_path`, `shortcut`, and focus fallback
- it cannot yet express explicit post-navigation actions such as `List Parameters`
- this is the main blocker for the failing Wave 1 list-family routes

### Use `tree_path` when:

- the screen is reachable by tree node
- the route is visible as tabs, menu items, tree items, list items, or buttons
- selection state matters

Example:

```json
"tree_path": ["Sources", "Connectors"]
```

Current caveat:

- some reference-guide list routes need one more action after the family node is selected
- if a screen requires `tree_path` plus a secondary command, the current model is not enough yet
- that work should be implemented in the next schema/navigation revision rather than hardcoded per row

### Use `shortcut` when:

- the app has a reliable accelerator
- the screen is easier to reach by keyboard than by tree selection

Supported modifiers:

- `Alt`
- `Ctrl`
- `Shift`

Supported special keys include:

- `Enter`
- `Esc`
- `Tab`
- `Up`
- `Down`
- `Left`
- `Right`
- function keys such as `F5`

Example:

```json
"shortcut": ["Alt+2"]
```

### Use focus fallback only as a last resort

This mode tries to focus and click likely matches or uses a minimal `Tab` fallback. It is the weakest route and should not be your primary plan design.

## Step 3: Choose Validation Anchors

Anchor quality matters more than screen names.

Best-to-worst anchor order:

1. `expected_automation_ids`
2. `require_selected_navigation`
3. `expected_controls`
4. `expected_control_types`

### Prefer `expected_automation_ids`

These are the most stable WPF anchors.

Example:

```json
"expected_automation_ids": ["dfFilter", "txtName", "cmdSave"]
```

Use them when discovery shows stable IDs such as:

- `txtName`
- `cmdSave`
- `dfFilter`
- `dgAttributes`
- `tabColumns`

### Use `require_selected_navigation`

This is critical for shared-shell screens, especially left-navigation families where many pages expose the same `dfFilter`, `txtName`, and `cmdSave` controls.

Example:

```json
"require_selected_navigation": true
```

### Use `expected_controls`

These are plain visible names. Use them when AutomationIds are missing or incomplete.

Example:

```json
"expected_controls": ["Save", "Connect", "Create in DWH"]
```

### Use `expected_control_types`

This is only a bias. It helps when the same name appears across multiple control types.

Example:

```json
"expected_control_types": ["TreeItem", "Button"]
```

## Step 4: Write A Plan Row

### Example: left-navigation screen

```json
{
  "row_id": "LeftNav.Parameters",
  "screen_name": "Parameters",
  "hints": {
    "tree_path": ["Parameters"],
    "expected_automation_ids": ["dfFilter", "txtName", "cmdSave"],
    "require_selected_navigation": true
  }
}
```

### Example: editor tab

```json
{
  "row_id": "EditTable.Columns",
  "screen_name": "Columns",
  "hints": {
    "tree_path": ["Columns"],
    "expected_controls": ["Columns", "Save"],
    "expected_automation_ids": ["dgAttributes", "dfFilter", "txtName"]
  }
}
```

### Example: keyboard-first screen

```json
{
  "row_id": "Shortcut.Connectors",
  "screen_name": "Connectors",
  "module": "Sources",
  "hints": {
    "shortcut": ["Alt+2"],
    "expected_controls": ["Connectors", "Save"],
    "expected_automation_ids": ["cmdSave"]
  }
}
```

## Screenshot Capture Notes

Current working Snagit behavior:

- Snagit writes capture packages into:
  - `C:\Users\lainoborgo\Documents\Snagit`
- the current preset path uses `.snagx` packages
- `CaptureRunner` now extracts the primary PNG from those packages automatically

Use the screenshot switches like this:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\wave1-snagit-poc-plan.json `
  --output .\CaptureRunner\Output\wave1-snagit-poc-report-snagx.json `
  --snagit-hotkey "Ctrl+Shift+Alt+5" `
  --snagit-exe "C:\Program Files\TechSmith\Snagit\SnagitCapture.exe" `
  --snagit-watch-dir "C:\Users\lainoborgo\Documents\Snagit" `
  --capture-output-dir .\CaptureRunner\Output\captures `
  --capture-timeout-ms 20000 `
  --keep-open
```

Current known-good proof:

- [wave1-snagit-poc-report-snagx.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-snagit-poc-report-snagx.json)
- [1.6.18-parameters.png](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures/1.6.18-parameters.png)

Important warning:

- do not patch Snagit's live runtime file casually:
  - `C:\Users\lainoborgo\AppData\Local\TechSmith\Snagit\25\Presets7.xml`
- that broke capture with `Unable to capture`
- editing the exported preset file in Documents is safer than editing the live runtime file

## Step 5: Test The New Row In Isolation

Do not add ten new rows and hope the batch explains itself. Run one row first.

Create a temporary one-row plan and scan it:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\temp-one-row-plan.json `
  --output .\CaptureRunner\Output\temp-one-row-report.json `
  --screen-timeout-ms 60000 `
  --keep-open
```

Promote the row into a curated plan only when:

- navigation succeeds repeatedly
- the expected anchors are found
- the result is at least `partial`
- the limitations are understandable

## How Classification Works

Classification is decided in [ScreenScanner.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/ScreenScanner.cs).

### `full`

You usually need all of these:

- window found
- navigation succeeded
- stable target controls found
- at least one target control has `automation_id`
- validation passed by title or controls

### `partial`

You typically land here when:

- navigation works, but anchors are weak
- controls exist, but IDs are missing
- timing is unstable
- validation is good enough to continue, but not strong enough for production certainty

### `none`

This is typical when:

- no window is available
- navigation times out
- no target controls are found
- the surface is custom-rendered and exposes too little UIA state

## How To Read The Limitations

Common limitation messages and what they mean:

| message pattern | meaning | usual fix |
|---|---|---|
| `interactive controls without AutomationId` | WPF surface is usable, but weakly anchored | Prefer any available IDs, or ask product team to add them |
| `Element set changed between repeated scans` | UI is dynamic or late-loading | Add waits, simplify state, or target a later stable point |
| `Second scan contained more elements than the first` | Delayed loading | Increase wait time or navigation timeout |
| `virtualized` | list/tree/grid may not realize all children | scroll, expand, or use a different anchor |
| `custom-rendered` | canvas/diagram-like surface | likely high-risk or non-UIA |
| `No target controls were found` | plan anchors are wrong or screen did not open | re-check discovery and selected route |

## When To Create A New Plan File

Create a new plan file when:

- you are exploring a new screen family
- you need a temporary dry-run subset
- you want to test a risky navigation route separately

Merge into the master plan when:

- the rows are already stable
- the family behavior is understood
- the output is part of the main baseline

## Naming Conventions

Recommended `row_id` patterns:

- `LeftNav.<ScreenName>`
- `Navigation.<ScreenName>`
- `EditTable.<TabName>`
- `Dialog.<ScreenName>`
- `Wizard.<ScreenName>`

Keep them:

- short
- stable
- human-readable
- unique

## Buttons, Menus, Tabs, And Tree Nodes

`tree_path` is broader than the name suggests. It can target:

- `TreeItem`
- `MenuItem`
- `TabItem`
- `ListItem`
- `Button`
- `Hyperlink`
- `Text`

That matching order is defined in [NavigationEngine.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/NavigationEngine.cs). This is why a route like `["Columns"]` can work against a tab, and a route like `["Sources", "Connectors"]` can work against a ribbon tab plus tree node.

## When A Screen Shares The Same Shell

Many Analytics Creator screens share the same shell controls. In those cases:

- use `require_selected_navigation: true`
- anchor on shared stable IDs like `dfFilter`, `txtName`, `cmdSave`
- still require the correct selected navigation node so the shell alone does not create a false positive

This pattern is already used in:

- [analyticscreator-leftnav-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-plan.json)
- [analyticscreator-leftnav-remaining-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-remaining-plan.json)

## Good Authoring Rules

- Prefer one strong `automation_id` over five weak labels.
- Keep plan rows minimal. Add only the anchors you need.
- Use discovery before editing the plan.
- Validate one row at a time before expanding a family.
- Promote temporary findings into the master plan only after repeatable success.
- Treat custom-rendered or diagram-heavy surfaces as high-risk until proven otherwise.

## Typical Extension Checklist

Before adding a row:

1. Put the app in the target state.
2. Run discovery.
3. Find the navigation route.
4. Choose the best anchors.
5. Write one row.
6. Run the row alone.
7. Inspect the report.
8. Merge it into the proper curated plan.

## Where To Look In Code

| file | role |
|---|---|
| [Program.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Program.cs) | CLI parsing, worker orchestration, discovery-or-scan entrypoint |
| [WindowService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/WindowService.cs) | attach or launch logic, single-instance checks, window lookup |
| [TreeService.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Services/TreeService.cs) | UIA tree snapshotting, matching, discovery heuristics |
| [NavigationEngine.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/NavigationEngine.cs) | tree, keyboard, and focus-based navigation |
| [Validator.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/Validator.cs) | quick-match and final validation logic |
| [UiaExplorer.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/UiaExplorer.cs) | two-pass UIA scan, candidate control extraction, limitations |
| [ScreenScanner.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Runner/ScreenScanner.cs) | classification, recommendations, per-screen orchestration |
| [JsonMap.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/JsonMap.md) | JSON artifact reference |

## Current Baseline Files

Use these as the current reference set:

- [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json)
- [Output/analyticscreator-master-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-master-report.json)
- [Output/discovery.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/discovery.json)

## Snagit Proof Of Concept

The current proof of concept supports:

- one configured Snagit preset
- one global hotkey
- one Snagit watch folder
- one managed output folder
- one capture attempt per successfully opened screen

Inputs:

- `--snagit-hotkey`
- `--snagit-exe`
- `--snagit-watch-dir`
- `--capture-output-dir`
- `--capture-timeout-ms`

Behavior:

1. the screen opens and validates as usual
2. CaptureRunner triggers the Snagit hotkey
3. CaptureRunner watches the Snagit output folder for a new or updated image file
4. CaptureRunner copies that file into its managed output folder using `<row-id>-<screen-name>.<ext>`
5. the report JSON records the capture outcome under `capture`

Starter plan:

- [wave1-snagit-poc-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-snagit-poc-plan.json)

Example command:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\wave1-snagit-poc-plan.json `
  --output .\CaptureRunner\Output\wave1-snagit-poc-report.json `
  --snagit-hotkey "Ctrl+Shift+5" `
  --snagit-exe "C:\Program Files\TechSmith\Snagit\SnagitCapture.exe" `
  --snagit-watch-dir "C:\SnagitDrop" `
  --capture-output-dir .\CaptureRunner\Output\captures `
  --capture-timeout-ms 20000 `
  --keep-open
```

Expected Snagit preset setup:

1. Create one image capture preset in Snagit.
2. Bind it to one global hotkey such as `Ctrl+Shift+5`.
3. Set the preset to save directly to file.
4. Point it to a fixed drop folder such as `C:\SnagitDrop`.
5. Use PNG as the output format.

If Snagit is installed in the default location, CaptureRunner now preflights and starts:

- `C:\Program Files\TechSmith\Snagit\SnagitCapture.exe`
