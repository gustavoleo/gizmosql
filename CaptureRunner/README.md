# CaptureRunner

CaptureRunner is a Windows-only `.NET 8` console application that uses `FlaUI` with the `UIA3` backend to assess how automatable a WPF screen is.

It is intentionally a capability scanner, not an execution engine. For each planned screen it:

- launches or attaches to the target application
- attempts navigation using UIA tree hints, keyboard shortcuts, then focus fallback
- scans the UI Automation tree twice for stability
- validates whether the requested screen appears reachable
- classifies the screen as `full`, `partial`, or `none`

It also supports a discovery mode which dumps the current window's likely modules, likely screens, and interesting controls before you lock a scan plan.

## Documentation

- [AuthoringGuide.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/AuthoringGuide.md) - single guide for adding screens, adding paths, and understanding the plan format
- [JsonMap.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/JsonMap.md)
- [ScreenshotIntegrationPlan.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/ScreenshotIntegrationPlan.md)
- [CurrentStatus.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/CurrentStatus.md)
- [QuickStart_EN.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/QuickStart_EN.md)
- [AGENTS.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/AGENTS.md)
- [wave1-snagit-poc-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-snagit-poc-plan.json)
- [wave1-first5-snagit-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-first5-snagit-plan.json)
- [ReferenceGuideScreenshotExecutionMatrix.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/ReferenceGuideScreenshotExecutionMatrix.md)
- [ReferenceGuideScreenshotGoNoGoQueue.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/ReferenceGuideScreenshotGoNoGoQueue.md)

## Project layout

```text
CaptureRunner/
  Program.cs
  Models/
  Runner/
  Services/
  Output/
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

## Run

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Users\lainoborgo\AppData\Local\Apps\2.0\BHDWDODX.YZH\CAA2DYCG.06J\anal..tion_e2c6de6c47453e9f_0003.0007_060d7646675b6351\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\sample-plan.json `
  --screen-timeout-ms 120000 `
  --output .\CaptureRunner\Output\report.json

dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Users\lainoborgo\AppData\Local\Apps\2.0\BHDWDODX.YZH\CAA2DYCG.06J\anal..tion_e2c6de6c47453e9f_0003.0007_060d7646675b6351\AnalyticsCreator.exe" `
  --discover-output .\CaptureRunner\Output\discovery.json

dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Users\lainoborgo\AppData\Local\Apps\2.0\BHDWDODX.YZH\CAA2DYCG.06J\anal..tion_e2c6de6c47453e9f_0003.0007_060d7646675b6351\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\wave1-snagit-poc-plan.json `
  --output .\CaptureRunner\Output\wave1-snagit-poc-report-snagx.json `
  --snagit-hotkey "Ctrl+Shift+Alt+5" `
  --snagit-exe "C:\Program Files\TechSmith\Snagit\SnagitCapture.exe" `
  --snagit-watch-dir "C:\Users\lainoborgo\Documents\Snagit" `
  --capture-output-dir .\CaptureRunner\Output\captures `
  --capture-timeout-ms 20000 `
  --keep-open
```

Current proven Snagit workflow:

- Snagit writes `.snagx` packages into `C:\Users\lainoborgo\Documents\Snagit`
- CaptureRunner watches that folder, extracts the primary PNG, and saves the managed screenshot into the requested capture output directory
- `Ctrl+Shift+Alt+5` currently works through Snagit's `Repeat last capture` path and therefore depends on the active Snagit preset being set correctly in the UI

## Input schema

`sample-plan.json` follows the required JSON shape:

```json
[
  {
    "row_id": "Entities.Connectors",
    "screen_name": "Connectors",
    "module": "Sources",
    "hints": {
      "tree_path": ["Sources", "Connectors"],
      "shortcut": ["Alt+2"]
    }
  }
]
```

Additional optional hint fields are supported:

- `expected_controls`: explicit control names to validate after navigation
- `expected_control_types`: optional control type names to bias matching
- `expected_automation_ids`: explicit automation IDs for stable WPF anchors when names are generic or repeated
- `require_selected_navigation`: require the requested tree or tab node to be selected before the screen is treated as open

The repo includes a richer real-world plan for the provided Analytics Creator executable:

- [analyticscreator-master-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-master-plan.json)
- [analyticscreator-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-plan.json)
- [analyticscreator-leftnav-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-plan.json)
- [analyticscreator-leftnav-remaining-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-remaining-plan.json)
- [analyticscreator-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-discovery-plan.json)
- [analyticscreator-tabs-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-tabs-discovery-plan.json)
- [analyticscreator-leftnav-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-discovery-plan.json)
- [analyticscreator-leftnav-remaining-discovery-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/analyticscreator-leftnav-remaining-discovery-plan.json)

## Runtime notes

- Run in an interactive Windows desktop session. UIA scanning will not work reliably from a disconnected or non-interactive context.
- CaptureRunner fails fast if multiple `AnalyticsCreator.exe` instances are running. Close extra instances before starting a scan.
- `--keep-open` leaves the application open after the scan. Without it, CaptureRunner only tries to close applications that it launched itself.
- Plan scans run each screen in an isolated worker process and rewrite the output JSON after each completed screen, so a timeout still leaves a usable partial report.
- `--screen-timeout-ms` controls the hard per-screen worker timeout. The default is `120000` ms.
- `--snagit-hotkey` plus `--snagit-watch-dir` enables the Snagit proof of concept. CaptureRunner triggers the hotkey, waits for a new image in the watch folder, then copies it into the managed capture output folder.
- Snagit integration now also supports `.snagx` package output. CaptureRunner extracts the primary image asset from the package and saves it as a managed PNG or source-format file.
- `--snagit-exe` defaults to `C:\Program Files\TechSmith\Snagit\SnagitCapture.exe`. CaptureRunner uses it to preflight and start SnagitCapture when needed.
- `--capture-timeout-ms` controls how long CaptureRunner waits for Snagit to write a file. The default is `20000` ms.
- During parent runs, CaptureRunner uses the resolved `operator_display` from the selected environment profile for both the operator notice and mouse parking. If no certified operator display is configured, both behaviors are skipped explicitly instead of falling back to a guessed monitor.
- The JSON report includes limitations for missing `AutomationId`, delayed loading, virtualization heuristics, and custom-rendered controls.
- Discovery output is written with `--discover-output`; if you pass both `--discover-output` and `--plan`, the tool writes the discovery file first and then executes the plan scan.
- Some screen families share the same editor shell controls. For those, the curated plan combines `tree_path` selection with shared `expected_automation_ids` so the scanner validates both navigation state and shell stability without inventing fake uniqueness.
- Left-navigation screens in the curated plans use `require_selected_navigation: true` so shared shell controls like `dfFilter`, `txtName`, and `cmdSave` do not create false positives when the wrong node is still active.
- The first 5 Wave 1 screenshot batch is not production-ready yet. `Scripts` succeeded end to end, but `Connectors`, `Layers`, `Packages`, and `Parameters` still time out because the current plan model lacks explicit post-navigation actions for list-family routes.
