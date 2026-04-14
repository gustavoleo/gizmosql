# Wave 1 Start-Now Native Capture Summary

Run date: `2026-04-14`

## Inputs

- Plan: `CaptureRunner/wave1-startnow-native-plan.json`
- Environment profile: `CaptureRunner/Profiles/certified-dual-monitor.json`
- Capture backend: `native`
- Capture area: `client`
- PNG DPI metadata: `600`

## Outputs

- Report: `CaptureRunner/Output/wave1-startnow-native-report.json`
- All captures: `CaptureRunner/Output/captures-wave1-startnow-native/`
- Accepted captures: `CaptureRunner/Output/captures-wave1-startnow-native-accepted/`
- Review captures: `CaptureRunner/Output/captures-wave1-startnow-native-review/`

## Result

- Total rows: `16`
- Accepted now: `15`
- Needs follow-up: `1`

## Accepted Now

- `1.6.2` `Galaxies`
- `1.6.3` `Layers`
- `1.6.4` `Models`
- `1.6.7` `Deployments`
- `1.6.10` `Hierarchies`
- `1.6.13` `Indexes`
- `1.6.14` `Macros`
- `1.6.16` `Object Scripts`
- `1.6.17` `Packages`
- `1.6.18` `Parameters`
- `1.6.19` `Partitions`
- `1.6.20` `Predefined Transformations`
- `1.6.22` `OLAP Roles`
- `1.6.23` `SQL Script`
- `1.6.25` `Snapshots`

## Needs Follow-Up

- `1.6.6` `Connectors`
  - Classification: `partial/medium`
  - Reason: the screen opened and the PNG was captured, but the expected Connectors-specific controls and automation IDs were not present in the current UIA surface, so this row did not meet the same acceptance standard as the other fifteen.
