# CaptureRunner JSON Map

This file maps the JSON artifacts used by `CaptureRunner`. The schemas are defined in [InputPlan.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/InputPlan.cs), [ScanResult.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/ScanResult.cs), and [DiscoveryReport.cs](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Models/DiscoveryReport.cs).

## JSON Families

`CaptureRunner` uses three JSON shapes:

1. `plan` JSON
   Input to the scanner. Always an array of screen definitions.
2. `report` JSON
   Output from a scan. Always an array of per-screen results.
3. `discovery` JSON
   Output from discovery mode. Always a single object describing the current UIA surface.

## Plan JSON

Shape:

```json
[
  {
    "row_id": "Navigation.Connectors",
    "screen_name": "Connectors",
    "module": "Sources",
    "hints": {
      "tree_path": ["Sources", "Connectors"],
      "shortcut": ["Alt+2"],
      "expected_controls": ["Connect", "Save"],
      "expected_control_types": ["Button", "TreeItem"],
      "expected_automation_ids": ["cmdCreate", "cmdSave"],
      "require_selected_navigation": true
    }
  }
]
```

Fields:

| field | type | meaning |
|---|---|---|
| `row_id` | string | Stable identifier for the screen or workbook row. |
| `screen_name` | string | Human-readable target name. |
| `module` | string or null | Optional grouping label used by title matching and reporting. |
| `hints.tree_path` | string array | Ordered UI path used by tree, menu, tab, list, or button navigation. |
| `hints.shortcut` | string array | Optional keyboard shortcut fallback. Usually one item. |
| `hints.expected_controls` | string array | Visible labels expected after navigation. |
| `hints.expected_control_types` | string array | Optional control types that narrow matching when names repeat. |
| `hints.expected_automation_ids` | string array | Strongest WPF anchors. Prefer these whenever available. |
| `hints.require_selected_navigation` | boolean | Prevents false positives from shared shell controls. |

## Report JSON

Shape:

```json
[
  {
    "row_id": "Navigation.Connectors",
    "screen_name": "Connectors",
    "module": "Sources",
    "open_result": {
      "success": true,
      "method_used": "tree_navigation",
      "duration_ms": 5376
    },
    "uia_detection": {
      "window_found": true,
      "window_title": "Edit Table [Northwind]",
      "total_descendants": 315,
      "elements_found": [],
      "candidate_controls": [],
      "missing_elements": [],
      "stability": {
        "consistent": true,
        "first_scan_count": 315,
        "second_scan_count": 315,
        "difference_count": 0
      }
    },
    "validation": {
      "title_match": false,
      "selected_navigation_match": true,
      "expected_control_match": true,
      "control_match": true,
      "target_element_found": true
    },
    "classification": {
      "automatable": "full",
      "confidence": "high"
    },
    "limitations": [],
    "recommendation": "Use UIA tree navigation with element re-validation after the screen opens.",
    "diagnostics": {
      "duration_ms": 6200,
      "fallback_used": null,
      "retry_count": 0,
      "exception": null
    }
  }
]
```

Fields:

| field | type | meaning |
|---|---|---|
| `open_result.success` | boolean | Whether CaptureRunner reached the target state. |
| `open_result.method_used` | string | `already_open`, `tree_navigation`, `keyboard`, `focus_change`, `timed_out`, or `none`. |
| `uia_detection.window_found` | boolean | Whether a window was available for scanning. |
| `uia_detection.elements_found` | array | Matching target controls found by name or automation ID. |
| `uia_detection.candidate_controls` | array | Additional controls worth using for future plan refinement. |
| `uia_detection.missing_elements` | array | Expected names or `automation_id:*` anchors not found. |
| `uia_detection.stability` | object | Two-pass consistency check for timing instability. |
| `validation` | object | Title, selected-navigation, and control-based confirmation flags. |
| `classification.automatable` | string | `full`, `partial`, or `none`. |
| `classification.confidence` | string | `high`, `medium`, or `low`. |
| `limitations` | array | Why a screen did not reach stronger confidence. |
| `recommendation` | string | Suggested automation strategy. |
| `diagnostics` | object | Duration, fallback used, retry count, and exception text. |
| `capture` | object | Optional Snagit capture result when screenshot mode is enabled. |

## Discovery JSON

Shape:

```json
{
  "window_title": "Edit Table [Northwind]",
  "total_descendants": 573,
  "control_type_summary": [],
  "likely_modules": [],
  "likely_screens": [],
  "interesting_controls": [],
  "notes": []
}
```

Fields:

| field | type | meaning |
|---|---|---|
| `window_title` | string or null | Active window title when discovery ran. |
| `total_descendants` | number | Total UIA descendants found. |
| `control_type_summary` | array | Counts by UIA control type. |
| `likely_modules` | array | Named `TabItem` or `MenuItem` candidates that look like modules. |
| `likely_screens` | array | Named `TreeItem`, `ListItem`, or `TabItem` candidates that look like screens. |
| `interesting_controls` | array | Candidate buttons, edits, grids, tabs, trees, and similar controls. |
| `notes` | array | Heuristics such as missing automation IDs, virtualization, or custom rendering. |

## Repository JSON Inventory

The current repository JSON files map like this:

| file | shape | row count | role |
|---|---|---:|---|
| [sample-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/sample-plan.json) | plan | 1 | Minimal example input. |
| [analyticscreator-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-plan.json) | plan | 6 | Curated starter plan for Connectors plus editor tabs. |
| [analyticscreator-leftnav-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-plan.json) | plan | 5 | Curated left-navigation plan for the first tree family. |
| [analyticscreator-leftnav-remaining-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-remaining-plan.json) | plan | 14 | Curated left-navigation plan for the remaining tree screens. |
| [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json) | plan | 25 | Canonical combined plan. |
| [analyticscreator-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-discovery-plan.json) | plan | 8 | Early exploratory screen set. |
| [analyticscreator-tabs-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-tabs-discovery-plan.json) | plan | 6 | Exploratory tab-screen set. |
| [analyticscreator-leftnav-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-discovery-plan.json) | plan | 5 | Exploratory left-navigation set. |
| [analyticscreator-leftnav-remaining-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-remaining-discovery-plan.json) | plan | 14 | Exploratory left-navigation remainder. |
| [wave1-snagit-poc-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-snagit-poc-plan.json) | plan | 1 | One-row Snagit proof plan for `Parameters`. |
| [wave1-first5-snagit-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-first5-snagit-plan.json) | plan | 5 | First Wave 1 screenshot batch for `Connectors`, `Layers`, `Packages`, `Parameters`, and `Scripts`. |
| [Output/report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/report.json) | report | 1 | Output from `sample-plan.json`. |
| [Output/analyticscreator-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-report.json) | report | 6 | Output from the curated starter plan. |
| [Output/analyticscreator-leftnav-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-leftnav-report.json) | report | 5 | Output from the first curated left-nav plan. |
| [Output/analyticscreator-leftnav-remaining-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-leftnav-remaining-report.json) | report | 14 | Output from the remaining curated left-nav plan. |
| [Output/analyticscreator-master-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-master-report.json) | report | 25 | Canonical combined report. |
| [Output/discovery-scan-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/discovery-scan-report.json) | report | 8 | Scan results for the exploratory starter set. |
| [Output/tabs-discovery-scan-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/tabs-discovery-scan-report.json) | report | 6 | Scan results for the exploratory tab set. |
| [Output/leftnav-discovery-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/leftnav-discovery-report.json) | report | 5 | Scan results for the exploratory left-nav set. |
| [Output/leftnav-remaining-discovery-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/leftnav-remaining-discovery-report.json) | report | 14 | Scan results for the exploratory left-nav remainder. |
| [Output/discovery.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/discovery.json) | discovery | 1 object | Raw discovery snapshot used to design the curated plans. |
| [Output/wave1-snagit-poc-report-snagx.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-snagit-poc-report-snagx.json) | report | 1 | Successful one-row Snagit proof using `.snagx` package extraction. |
| [Output/wave1-first5-snagit-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-first5-snagit-report.json) | report | 5 | First Wave 1 screenshot batch result. Only `Scripts` succeeded end to end in the current model. |

## Which Files Matter Most

For day-to-day work, the most important JSON files are:

1. [Output/discovery.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/discovery.json)
   Use this to discover candidate screens and controls.
2. [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json)
   Use this as the main input baseline.
3. [Output/analyticscreator-master-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-master-report.json)
   Use this as the main validation baseline.

## Naming Convention

- `*-plan.json`
  Planned targets to scan.
- `*-report.json`
  Results written after a scan.
- `*discovery*.json`
  Exploration artifacts used before the final plan is locked.

## Temporary Runtime JSON

During worker-based runs, CaptureRunner also creates temporary JSON files under the system temp directory. Those are intermediate worker plan and report files used for per-screen isolation and partial report persistence. They are not part of the repository baseline.

## Capture Outputs

Screenshot files are not JSON, but two managed output folders now matter operationally:

- [Output/captures](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures)
  Holds the successful one-row Snagit proof output.
- [Output/captures-wave1-first5](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures-wave1-first5)
  Holds managed screenshots from the first Wave 1 batch.
