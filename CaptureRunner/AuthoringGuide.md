# CaptureRunner Authoring Guide

This is the single human guide for adding new screens and new navigation paths to `CaptureRunner`.

Use this file for instructions. It is written in **Markdown** for humans.

Use JSON for files that `CaptureRunner` executes:

- `*-plan.json` for screen plans
- `Profiles/*.json` for environment profiles

YAML is not supported today. The loader in [Program.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Program.cs) uses `System.Text.Json` for plans and profiles.

## The 5-Minute Version

1. Find the closest screen in [NavigationMap.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/NavigationMap.md).
2. Copy the closest existing JSON row.
3. Change the route and validation anchors.
4. Test one row only.
5. Trust the report, not your guess.
6. Promote the row only after the report proves it.

## What File Type To Use

| file | format | purpose |
|---|---|---|
| [AuthoringGuide.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/AuthoringGuide.md) | Markdown | Human instructions for contributors. |
| `temp-one-row-plan.json` | JSON | One-row plan file for testing a new screen. |
| [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json) | JSON | Canonical combined plan baseline. |
| `Profiles/*.json` | JSON | Certified monitor and environment profiles. |

## How To Add A New Screen

### Step 1: Identify the screen family

Start from [NavigationMap.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/NavigationMap.md). Find the closest existing screen in one of these families:

- `left-nav list`
- `toolbar dialog`
- `toolbar wizard`
- `left-nav detail`
- `context page`
- `context dialog`

If the closest route is marked `seed-data`, `harness`, or `manual`, do not treat it like a normal simple row. See [When Not To Author A Normal Row](#when-not-to-author-a-normal-row).

### Step 2: Copy the closest JSON row

Do not start from an empty file unless you have to.

Recommended starting points:

- `left-nav list`: copy from [wave1-startnow-native-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-startnow-native-plan.json)
- `toolbar dialog/wizard`: copy from [referenceguide-toolbar-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/referenceguide-toolbar-plan.json)
- `authored but not verified`: copy from [referenceguide-laneb-first10-pass2-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/referenceguide-laneb-first10-pass2-plan.json)

### Step 3: Update the row

At minimum, update:

- `row_id`
- `screen_name`
- `module`
- `hints`
- `actions`

Use the real schema from:

- [InputPlan.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/InputPlan.cs)
- [PlanAction.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/PlanAction.cs)
- [CaptureHints.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/CaptureHints.cs)

### Step 4: Run discovery

Use discovery before you lock anchors:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --discover-output .\CaptureRunner\Output\discovery.json `
  --keep-open
```

Look for:

- likely screen names
- likely modules
- `automation_id` values
- visible control names you can validate against

### Step 5: Test one row only

Never start by adding many new rows at once.

Create a one-row plan file, run it, inspect the result, then decide if the route is real.

### Step 6: Read the result

Use the report to decide what happened:

- `full/high`: strong candidate
- `partial`: route may open, but anchors are weak or wrong
- `none`: wrong route or wrong anchors
- `capture.success = true`: screenshot path worked

### Step 7: Promote the row

Promotion order:

1. one-row test plan
2. authored plan file
3. curated plan
4. navigation map or production batch once stable

Do not call a row verified until the report proves it.

## How To Add A New Path

### Left-nav list

Use this for screens like:

- `Parameters`
- `Layers`
- `Macros`
- `Snapshots`

Typical shape:

- `tree_path` points to the left navigation entry
- `expected_automation_ids` prove the shell
- `require_selected_navigation = true` prevents false positives

Use this when the page opens by selecting a normal navigation node.

### Toolbar dialog or wizard

Use this for screens like:

- `File -> DWH Wizard`
- `Help -> About`
- `Options -> DWH settings`

Typical shape:

- `tree_path` points to the top-level toolbar tab such as `File`, `Options`, or `Help`
- first action is usually `Invoke`
- second action is often `WaitForWindow`

Use `WaitForWindow` when a new dialog or wizard should appear.

### Context/detail route

Use this for screens that need an already-selected object, such as:

- `Pages > Source`
- `Pages > Table`
- `Pages > Model Dimension`

Typical shape:

- start from a left-nav family
- use `actions` to move deeper
- select or open the object-specific surface
- validate with stronger anchors than the shared shell

### When to use `WaitForWindow`

Use `WaitForWindow` when success means:

- a dialog opened
- a wizard opened
- a modal changed the active surface

Good examples:

- `About`
- `DWH Wizard`
- `EULA`

### When to use `WaitForElement`

Use `WaitForElement` when success means:

- the screen stayed inside the main shell
- a tab, list, or editor surface changed
- a stable control should appear

Good examples:

- `cboSchema`
- `cmdSave`
- `cmbGroup`
- `leftall`

### When `SendKeys` is allowed

Use `SendKeys` only when:

- direct UIA invocation is not available
- the target is real but not exposed cleanly
- you already tried a direct route first

It is fallback only, not the default authoring strategy.

## Copy-Paste JSON Templates

All examples below are **plan JSON**, not documentation format. Save them as `.json`.

### 1. Left-nav list template

Based on the current `Parameters` pattern:

```json
[
  {
    "row_id": "1.6.18",
    "screen_name": "Parameters",
    "module": "Lists",
    "hints": {
      "tree_path": ["Parameters"],
      "expected_automation_ids": ["dfFilter", "txtName", "cmdSave"],
      "require_selected_navigation": true
    }
  }
]
```

Use this shape for simple list pages like `Layers`, `Macros`, and `Snapshots`.

### 2. Toolbar wizard template

Based on the current `DWH Wizard` pattern:

```json
[
  {
    "row_id": "1.8.3",
    "screen_name": "DWH Wizard",
    "module": "Wizards",
    "hints": {
      "tree_path": ["File"],
      "expected_controls": ["Cancel"],
      "expected_automation_ids": ["cmdBack"],
      "use_only_expected_controls": true,
      "require_expected_control_match": true
    },
    "actions": [
      {
        "kind": "Invoke",
        "name": "DWH Wizard",
        "control_type": "Button",
        "required": true,
        "timeout_ms": 4000,
        "post_action_delay_ms": 600
      },
      {
        "kind": "WaitForWindow",
        "window_title": "DWH Wizard",
        "match_mode": "Contains",
        "required": true,
        "timeout_ms": 8000,
        "post_action_delay_ms": 400
      }
    ]
  }
]
```

Use this shape for:

- toolbar dialogs
- toolbar wizards
- modal windows launched from `File`, `Options`, or `Help`

### 3. Context/detail route template

Based on the current `Model Dimension` authored pattern:

```json
[
  {
    "row_id": "1.5.8",
    "screen_name": "Model Dimension",
    "module": "Pages",
    "hints": {
      "tree_path": ["Models"],
      "expected_controls": ["Dimensions"],
      "expected_automation_ids": ["cmbGroup", "leftall", "rightall"],
      "use_only_expected_controls": true,
      "require_expected_control_match": true
    },
    "actions": [
      {
        "kind": "Invoke",
        "name": "Dimensions",
        "control_type": "Button",
        "required": true,
        "timeout_ms": 4000
      },
      {
        "kind": "WaitForElement",
        "automation_id": "cmbGroup",
        "required": true,
        "timeout_ms": 4000
      },
      {
        "kind": "WaitForElement",
        "automation_id": "leftall",
        "required": true,
        "timeout_ms": 4000
      },
      {
        "kind": "WaitForElement",
        "automation_id": "rightall",
        "required": true,
        "timeout_ms": 4000
      }
    ]
  }
]
```

Use this shape when:

- a normal left-nav selection is not enough
- you need one or more deeper actions
- the target is a detail surface rather than a top-level list

### 4. Optional native capture block

Use this only if you want the one-row test to save a screenshot too:

```json
"capture": {
  "mode": "Default"
}
```

Current recommended runtime defaults are controlled from the command line:

- `--capture-backend native`
- `--native-capture-area client`
- `--png-dpi 600`

## How To Choose Validation Anchors

Anchor priority is:

1. `expected_automation_ids`
2. `require_selected_navigation`
3. `expected_controls`

### 1. Prefer `expected_automation_ids`

These are the strongest anchors.

Good current examples:

- `dfFilter`
- `txtName`
- `cmdSave`
- `cboSchema`
- `cmdBack`
- `cmbGroup`

### 2. Use `require_selected_navigation`

This matters for shared-shell pages where many screens expose the same controls.

Use it on list families like:

- `Parameters`
- `Layers`
- `Macros`
- `Packages`

### 3. Use `expected_controls` only when needed

These are visible names, not stable internal IDs.

Use them when:

- there is no useful automation ID
- the visible label is specific enough

Good examples:

- `Dimensions`
- `Facts`
- `Historizations`
- `DWH Wizard`

Bad examples by themselves:

- `Save`
- `Close`
- `Cancel`

Those are too generic unless paired with stronger anchors.

## How To Test One Screen

Recommended first-proof command:

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\temp-one-row-plan.json `
  --output .\CaptureRunner\Output\temp-one-row-report.json `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --capture-backend native `
  --native-capture-area client `
  --png-dpi 600 `
  --capture-output-dir .\CaptureRunner\Output\captures-temp `
  --screen-timeout-ms 120000 `
  --keep-open
```

Default rules:

- use native capture first
- test one row only
- keep the app open during iteration
- do not move the mouse or use the keyboard during the run

## How To Read The Result

### `full/high`

The route is a strong candidate.

Usually means:

- navigation worked
- validation anchors matched
- the result is stable enough to promote

### `partial`

Something opened, but the proof is weak.

Typical reasons:

- generic anchors matched
- wrong screen inside the same shell
- route opened but screen-specific controls did not appear

Do not promote this as verified.

### `none`

Treat this as failure.

Typical reasons:

- wrong route
- wrong anchors
- screen never opened

### `capture.success = true`

This only tells you the screenshot path worked.

It does **not** automatically mean the route is verified.

The route still needs the validation result to be good.

## When Not To Author A Normal Row

Stop and classify the route instead if it belongs in one of these buckets.

### `seed-data`

Use this when the route needs real objects to exist first.

Examples:

- `Source`
- `Table`
- `Package`
- `Transformation`

### `harness`

Use this when the screen only appears after a forced state.

Examples:

- `Error description`
- `Login`
- `Upgrade repository`

### `manual`

Use this when the route depends on unstable diagram or object-context behavior.

Examples:

- `Star`
- `Object groups`
- `Source constraints`
- `Hash keys`

If a route belongs here, do not pretend it is a simple authoring task.

## Advanced Reference

### Current plan schema

Main fields in a row:

- `row_id`
- `screen_name`
- `module`
- `hints`
- `required_profile`
- `actions`
- `capture`

See:

- [InputPlan.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/InputPlan.cs)
- [PlanAction.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/PlanAction.cs)
- [CaptureHints.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/CaptureHints.cs)

### Supported action kinds

Current supported actions are:

- `Invoke`
- `WaitForElement`
- `WaitForWindow`
- `SendKeys`
- `Delay`

### Current default authoring decisions

- instructions format: Markdown
- plan/profile format: JSON
- first-proof screenshot backend: native
- first test scope: one row
- shell examples: PowerShell
- promotion rule: report must prove the row

### Best source of truth before you add anything

Check these in this order:

1. [NavigationMap.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/NavigationMap.md)
2. [referenceguide-laneb-first10-pass2-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/referenceguide-laneb-first10-pass2-plan.json)
3. [referenceguide-toolbar-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/referenceguide-toolbar-plan.json)
4. [wave1-startnow-native-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-startnow-native-plan.json)
5. [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json)

If you cannot find a close example in those files, run discovery before you invent anything.
