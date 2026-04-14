# Screenshot Integration Plan

This document captures the current plan for adding screenshot capture on top of the validated `CaptureRunner` navigation layer.

## Decision Summary

Use `Snagit` as the primary capture engine.

Use `PowerToys` only as a helper tool, not as the capture engine.

Why:

- Snagit supports capture presets, preset hotkeys, and direct save-to-file workflows.
- PowerToys does not provide a dedicated screenshot-capture utility in its current utility set.
- PowerToys `Image Resizer` can resize saved image files after capture, but resizing does not add real source detail.

## What The Official Docs Support

### Snagit

The official TechSmith docs confirm:

- you can assign hotkeys to Snagit presets
- you can create a preset that saves directly to a file
- you can configure file format and destination folder from the preset
- Snagit can use or override capture hotkeys such as `Print Screen`
- changing DPI after capture does not add pixels or improve actual capture quality

Sources:

- [Hotkey Problems and Questions in Snagit](https://support.techsmith.com/hc/en-us/articles/203731558-Hotkey-Problems-and-Questions-in-Snagit)
- [Automatically Save Images to a Specific Format During Capture](https://support.techsmith.com/hc/en-us/articles/203731148-Automatically-Save-Images-to-a-Specific-Format-During-Capture)
- [Snagit Print Screen Hotkey Not Working](https://support.techsmith.com/hc/en-us/articles/203731428-Snagit-Print-Screen-Hotkey-Not-Working)
- [Effect of Changing DPI on Snagit Capture Quality](https://support.techsmith.com/hc/en-us/articles/203732188-Effect-of-Changing-DPI-on-Snagit-Capture-Quality)
- [Snagit MSI Installation Guide](https://www.techsmith.com/wp-content/uploads/2025/08/Snagit-MSI-Installation-Guide.pdf)

### PowerToys

The official Microsoft docs confirm:

- PowerToys includes `Image Resizer`, which resizes existing image files
- PowerToys includes `Screen Ruler`, which measures pixels on screen
- PowerToys includes layout and mouse utilities
- the current PowerToys utility list does not include a dedicated screenshot-capture utility

Sources:

- [PowerToys overview](https://learn.microsoft.com/en-us/windows/powertoys/)
- [PowerToys Image Resizer](https://learn.microsoft.com/en-us/windows/powertoys/image-resizer)
- [PowerToys Screen Ruler](https://learn.microsoft.com/en-us/windows/powertoys/screen-ruler)

## Resolution Reality

For screenshots, output quality is limited by the pixels actually displayed on screen at capture time.

Important constraint:

- `600 DPI` is print metadata, not extra detail
- a screenshot only contains the pixels shown on the display or capture surface
- increasing DPI after the fact does not create new image information
- resizing a capture larger also does not create new source detail

That is explicitly stated by TechSmith in the DPI article above.

## Recommended Capture Baseline

If the goal is high-quality reference-guide screenshots, the safest baseline is:

1. Use a `3840 x 2160` display, not `3840 x 2140`.
2. Run Windows display scaling at `100%`.
3. Keep the target application on the same display every time.
4. Use PNG output, not JPG.
5. Turn HDR off if capture fidelity becomes inconsistent.
6. Normalize window size and placement before every capture.
7. Capture at native displayed size, then only set DPI metadata later if print layout requires it.

## Recommended Tool Roles

### Snagit

Use Snagit for:

- actual capture
- fixed preset settings
- fixed output format
- fixed output folder
- reliable hotkey-based triggering

### PowerToys

Use PowerToys for:

- `FancyZones` to keep windows in deterministic positions
- `Screen Ruler` to confirm capture region sizes during setup
- `Image Resizer` to generate alternate delivery sizes after capture

Do not use PowerToys as the primary screenshot engine.

## Integration Architecture

The recommended architecture is:

1. `CaptureRunner` opens and validates the target screen.
2. `CaptureRunner` normalizes the window state and foreground focus.
3. Snagit is triggered through a dedicated preset hotkey.
4. Snagit writes either a direct image file or a `.snagx` capture package.
5. The orchestration waits for the output file to appear.
6. If Snagit wrote a `.snagx` package, the primary image is extracted.
7. The managed output file is mapped to the workbook row ID.

## Phase Plan

### Phase 1

Build a manual-assisted capture lane:

- create one Snagit preset for image capture
- assign a dedicated hotkey
- save directly to PNG in a fixed folder
- verify one end-to-end capture from a proven `CaptureRunner` screen

This phase is now implemented as a proof of concept in `CaptureRunner` with:

- [wave1-snagit-poc-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-snagit-poc-plan.json)
- `--snagit-hotkey`
- `--snagit-exe`
- `--snagit-watch-dir`
- `--capture-output-dir`
- `--capture-timeout-ms`

The one-row proof is now successful with:

- report:
  - [wave1-snagit-poc-report-snagx.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-snagit-poc-report-snagx.json)
- extracted image:
  - [1.6.18-parameters.png](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/captures/1.6.18-parameters.png)

Important implementation note:

- the real working watch folder is `C:\Users\lainoborgo\Documents\Snagit`
- the current user preset writes `.snagx` capture packages, not direct PNG files
- `CaptureRunner` now extracts the primary image from the Snagit package automatically

The current trigger is:

- `Ctrl+Shift+Alt+5`

That works through Snagit's `Repeat last capture` path and therefore depends on the active Snagit preset being correct in Snagit UI.

The current default Snagit executable path is:

- `C:\Program Files\TechSmith\Snagit\SnagitCapture.exe`

### Phase 2

Add deterministic naming and orchestration:

- map screenshot filename to workbook row ID
- wait for the file after the hotkey trigger
- log success or failure per row

This is now implemented for both direct image files and `.snagx` package output.

### Phase 3

Add batch screenshot production:

- run against the `Start now` queue first
- then expand into exact-plan-row and seeded-data waves

The first real batch attempt is:

- plan:
  - [wave1-first5-snagit-plan.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/wave1-first5-snagit-plan.json)
- report:
  - [wave1-first5-snagit-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/wave1-first5-snagit-report.json)

Current result:

- `1.6.23 Scripts` succeeded end to end
- `1.6.6 Connectors` timed out
- `1.6.3 Layers` timed out
- `1.6.17 Packages` timed out
- `1.6.18 Parameters` timed out in the batch plan even though the one-row proof works

So the Phase 3 blocker is now:

- not Snagit transport
- not screenshot extraction
- but list-family navigation fidelity in `CaptureRunner`

The next technical step is to extend the plan and navigation model with explicit post-navigation actions for routes like:

- `Parameters -> List Parameters`
- module selection followed by a named list action
- family-node selection followed by a secondary button or menu invocation

## Recommendation

Choose `Snagit` as the screenshot backend for the project.

Do not choose `PowerToys` as the screenshot backend.

Use PowerToys only to support the capture environment.
