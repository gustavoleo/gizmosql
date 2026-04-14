# Toolbar File Dry Run Summary

Run date: `2026-04-14`

## Inputs

- Plan: `CaptureRunner/referenceguide-toolbar-file-plan.json`
- Report: `CaptureRunner/Output/referenceguide-toolbar-file-report-pass2.json`
- Environment profile: `CaptureRunner/Profiles/certified-dual-monitor.json`

## Result

- `1.8.3` `DWH Wizard`: `full/high`
- `1.7.8` `Load from cloud`: `partial/low`
- `1.7.17` `Synchronize DWH`: `partial/low`

## Important Note

`DWH Wizard` leaves the application in a wizard state inside the main shell. Because the current batch runner keeps the same AnalyticsCreator session alive across worker rows, the later `File` rows were executed after that state change. That means:

- the `DWH Wizard` row is a real positive signal for the new toolbar action path
- the `Load from cloud` and `Synchronize DWH` rows are not yet clean final judgments
- those two rows should be rerun individually from a reset shell state or after explicit wizard cleanup
