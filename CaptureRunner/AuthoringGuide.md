# CaptureRunner Authoring Guide

This is the working authoring guide for `CaptureRunner`.

Use it for four things:

1. bootstrap profiles
2. route recipes
3. authored screen plans
4. route promotion after a fresh mapper run

The current rule is simple:

- use JSON for everything `CaptureRunner` executes
- trust runtime evidence, not guesses
- only promote new routes that appear in the mapper artifacts

## Choose the right file type

| file | format | use it for |
|---|---|---|
| `Profiles/northwind-bootstrap.json` | JSON | startup, saved-password login, repository selection, repository verification |
| `Profiles/*.json` | JSON | monitor/environment profiles |
| `Profiles/high-value-route-recipes.json` | JSON | multi-step routes that should always be seeded into the mapper queue |
| `*-plan.json` | JSON | explicit authored plan execution |
| `Output/*/ui-map.json` | JSON | runtime-discovered screen map |
| `Output/*/UiMap.md` | Markdown | human-readable map of the same run |
| `Output/*/rejected-routes.json` | JSON | routes that opened but failed evidence or were rejected |
| `Output/*/remaining-queued-routes.json` | JSON | routes still queued when traversal stopped |
| `Output/*/route-promotion-summary.json` | JSON | grouped suggestions derived from rejected routes first, then remaining queued routes |

## The current promotion workflow

Always start from a fresh cold-start `--map-ui` run.

Promotion order:

1. inspect `rejected-routes.json`
2. if it is empty, inspect `remaining-queued-routes.json`
3. use `route-promotion-summary.json` to group the real stalled leaf actions
4. add or refine route recipes only for routes that appear in those artifacts
5. rerun the same fresh command

Do not add routes because the toolbar, tab, or dialog is "probably there". The route must appear in the artifacts first.

## Bootstrap profiles

Bootstrap profiles exist so every automated run starts from the same application state.

Current default:

- [Profiles/northwind-bootstrap.json](Profiles/northwind-bootstrap.json)

Current `Northwind` bootstrap contract:

- accept saved-password login
- use repository selector `cmbName`
- confirm with a positive action such as `OK` or `Connect`
- verify the final shell contains `Northwind`

Keep bootstrap profiles conservative. They should identify startup actions and repository verification anchors, not encode broader navigation logic.

## Route recipes

Route recipes seed known safe multi-step routes into the mapper queue.

Use them for:

- toolbar dialogs
- toolbar wizards
- safe entry points that require a parent tab before the leaf button exists

Current seeded examples:

- `Help > About`
- `Help > EULA`
- `File > Find on diagram`
- `File > DWH Wizard`

See:

- [Profiles/high-value-route-recipes.json](Profiles/high-value-route-recipes.json)

### When to add a route recipe

Add a route recipe only when:

- a fresh run produced a rejected or remaining queued route for that exact leaf
- the route is safe and non-destructive
- the leaf depends on a parent route that UIA discovery cannot infer reliably from the current shell snapshot

### Route recipe template

```json
[
  {
    "route_type": "safe_dialog_open",
    "route_text": "Help > About",
    "steps": [
      {
        "kind": "bootstrap",
        "name": "Northwind",
        "action": "verify"
      },
      {
        "kind": "TabItem",
        "name": "Help",
        "control_type": "TabItem",
        "action": "select"
      },
      {
        "kind": "Button",
        "name": "About",
        "control_type": "Button",
        "action": "invoke"
      }
    ]
  }
]
```

## Authored screen plans

Authored plans still matter when you want a fixed row set or a curated batch outside the automatic mapper.

Use authored plans for:

- one-row validation
- regression checks on known screens
- tightly curated screenshot batches
- screen families that need explicit post-navigation actions

Relevant schema:

- `Models/InputPlan.cs`
- `Models/PlanAction.cs`
- `Models/CaptureHints.cs`

### One-row workflow

1. run discovery first
2. copy the closest existing row
3. set the route and anchors
4. run one row only
5. inspect the report before promoting the row

### Discovery command

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Path\To\AnalyticsCreator.exe" `
  --discover-output .\CaptureRunner\Output\discovery.json `
  --keep-open
```

### One-row plan template

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

### Toolbar dialog or wizard plan template

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

## Validation rules

Use stable anchors whenever possible:

- window title containing the expected repository or dialog title
- explicit `automation_id`
- expected visible control names
- selected navigation state when the shell is shared

Do not mark a screen verified unless:

- the navigation report passed
- the validation anchors passed
- a PNG screenshot exists for that run when capture is expected

## Safety rules

- prefer UIA `Invoke`, `SelectionItem`, `ExpandCollapse`, and `Value` patterns
- treat keyboard fallback as scoped fallback only
- never use mouse-coordinate automation as the primary route
- do not add destructive routes to recipes
- treat unknown confirmation dialogs as blocked until proven safe
- assume dialogs and wizards must be explicitly closed after capture

## What not to do

- do not promote routes from memory or from the authored workbook alone
- do not author recipe families from empty artifacts
- do not mark a screen accepted because it opened once without evidence
- do not weaken anchors to force acceptance

## Related files

- [README.md](README.md)
- [CurrentStatus.md](CurrentStatus.md)
- [QuickStart_EN.md](QuickStart_EN.md)
- [NavigationMap.md](NavigationMap.md)
