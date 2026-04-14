# CaptureRunner Quick Start

## Purpose

`CaptureRunner` automates the technical assessment and step-by-step screenshot capture workflow for `AnalyticsCreator`.

The current state is **not yet full automation for every screen**, but the foundation is working:

- UIA/FlaUI navigation exists for the verified baseline
- Snagit integration is real and usable
- monitor preflight, window placement, and mouse parking are now profile-driven

## What Is Proven Today

- Verified scanner baseline:
  - [analyticscreator-master-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-master-report.json)
- Successful one-screen Snagit proof:
  - [wave1-snagit-poc-report-snagx.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-snagit-poc-report-snagx.json)
- Successful extracted sample image:
  - [1.6.18-parameters.png](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures/1.6.18-parameters.png)
- Successful monitor preflight with the certified dual-monitor profile:
  - [dual-monitor-preflight-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/dual-monitor-preflight-report.json)

## Technical Prerequisites

- Windows desktop session, not disconnected and not locked
- exactly **one** running `AnalyticsCreator.exe`
- Snagit installed:
  - `C:\Program Files\TechSmith\Snagit\SnagitCapture.exe`
- active Snagit preset:
  - `ac_refguide_4k_png`
- working Snagit trigger:
  - `Ctrl+Shift+Alt+5`
- current Snagit output folder:
  - `C:\Users\lainoborgo\Documents\Snagit`
- certified monitor profile:
  - Capture: `\\.\DISPLAY5` (`3840x2160`, `125%`)
  - Operator: `\\.\DISPLAY2` (`1080x1920`, `100%`)

## Quick Start

### 1. Build

```powershell
dotnet build .\CaptureRunner\CaptureRunner.csproj
```

### 2. Run One Verified Screen With Snagit

```powershell
dotnet run --project .\CaptureRunner\CaptureRunner.csproj -- `
  --exe "C:\Users\lainoborgo\AppData\Local\Apps\2.0\BHDWDODX.YZH\CAA2DYCG.06J\anal..tion_e2c6de6c47453e9f_0003.0007_060d7646675b6351\AnalyticsCreator.exe" `
  --plan .\CaptureRunner\wave1-snagit-poc-plan.json `
  --output .\CaptureRunner\Output\wave1-snagit-poc-report-snagx.json `
  --environment-profile .\CaptureRunner\Profiles\certified-dual-monitor.json `
  --snagit-hotkey "Ctrl+Shift+Alt+5" `
  --snagit-exe "C:\Program Files\TechSmith\Snagit\SnagitCapture.exe" `
  --snagit-watch-dir "C:\Users\lainoborgo\Documents\Snagit" `
  --capture-output-dir .\CaptureRunner\Output\captures `
  --capture-timeout-ms 20000 `
  --keep-open
```

## Expected Result

- preflight must return `passed: true`
- the target screen must open and validate
- Snagit must write a `.snagx` file
- `CaptureRunner` automatically extracts the primary image from that package
- result file:
  - [1.6.18-parameters.png](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures/1.6.18-parameters.png)

## Current Bottleneck

The main blocker is **no longer** Snagit and **no longer** multi-monitor handling.

The current blocker is navigation for list-family screens where a tree click must be followed by an explicit secondary action, for example:

- `Parameters -> List Parameters`
- `Packages -> List ...`
- `Layers -> List ...`

That is why single-screen proofs already work while batch runs for multiple list screens still fail.

## Next Technical Step

The next work package is:

- extend the plan schema with explicit post-navigation actions
- re-author the first Wave 1 list screens with those actions
- rerun the batch with Snagit enabled

In short:

**The platform is now stable enough for controlled expansion. The remaining work is in screen-family navigation logic, not in the capture infrastructure.**
