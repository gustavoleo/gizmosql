# AGENTS.md

## Project
GizmoSQL Windows First-Run Experience — Phase 1

## Mission
Implement a **customer-journey-first Windows installer experience** for GizmoSQL.

The target user outcome is:

- one recommended Windows download
- one guided installation flow
- Core installed
- sample database installed by default
- GizmoSQL UI installed by default
- Power BI connector available as an optional recommended checkbox
- clear completion actions
- first successful query in **8 steps or fewer**

The top-level recommended Windows artifact must become:

- `GizmoSQL-Setup-x64.exe`

This should be implemented with:

- existing Core MSI
- existing UI MSI
- existing Power BI connector MSI
- a new **WiX Burn bundle** that chains them

Do not stop at recommendations. Implement the packaging, build, docs, and onboarding assets.

---

## Context
The public repo already contains a Windows installer path in `installer/`, and the project is already distributed with a Windows MSI flow. GizmoSQL UI v2.5.0 is distributed separately as a Windows MSI release. The Power BI connector is also distributed separately and is already documented as an MSI-based installation path. WiX Burn supports building a top-level bundle that chains MSI packages together.

Today the problem is not “Windows install does not exist.”
The problem is “the Windows customer journey is fragmented.”

The current experience forces users to discover multiple artifacts and infer what to do next. Phase 1 must compress that journey into one obvious path.

---

## Primary goal
Make this the default evaluator journey:

1. Download `GizmoSQL-Setup-x64.exe`
2. Run installer
3. Accept defaults
4. Install
5. Finish
6. Open GizmoSQL UI
7. Open bundled demo connection
8. Run first query

That is the primary KPI.

---

## Secondary goal
Support a Power BI journey with one checkbox from the same installer:

1. Download `GizmoSQL-Setup-x64.exe`
2. Run installer
3. Keep Power BI connector checked
4. Install
5. Finish
6. Launch demo server
7. Open Power BI Desktop
8. Get Data > Database > GizmoSQL
9. Enter host/port
10. Authenticate
11. Browse/query

---

## Hard rules
- Reuse existing MSI assets wherever possible.
- Do not merge all products into one MSI.
- Do not reimplement UI internals.
- Do not reimplement Power BI connector internals.
- Do not generate sample data during installation.
- Use a **prebuilt sample DuckDB database**.
- Avoid fragile custom actions unless strictly necessary.
- Prefer low-risk packaging changes.
- Preserve upgrade, uninstall, and repair coherence.
- Keep individual MSI artifacts available for advanced/manual users.
- Treat **UI as the default first-run surface** on Windows.

---

## Architecture to implement
Create a top-level **WiX Burn bundle** that produces:

- `GizmoSQL-Setup-x64.exe`

The bundle must chain:

1. Core MSI
2. UI MSI
3. optional Power BI connector MSI
4. sample DB payload or sample-data package

Burn is the orchestration layer.
Core MSI remains a package.
UI MSI remains a package.
Power BI connector MSI remains a package.

---

## Scope
### In scope
- Burn bundle project
- Core MSI onboarding improvements
- sample DB installation by default
- UI chained by default
- Power BI connector chained optionally
- Quickstart page
- demo launcher
- Start Menu / completion actions
- CI/CD updates
- README / release messaging updates

### Out of scope
- Windows service mode
- deep enterprise deployment UI
- runtime sample generation during MSI install
- replacing standalone MSI distribution
- redesigning the UI product itself

---

## Customer journey requirements
### Evaluator journey
Optimize for:
- lowest number of user decisions
- no repo-hopping
- no need to understand CLI flags
- no need to find a DB file manually

### Power BI journey
Optimize for:
- one checkbox from the main installer
- no manual trust/certificate workaround
- clear restart reminder for Power BI Desktop

### Technical journey
Still support:
- Start Menu demo launcher
- CLI / DBeaver / other client access
- local sample DB query testing

---

## Required package defaults
Inside the top-level installer:

### Required
- GizmoSQL Core

### Default enabled
- Sample Database
- GizmoSQL UI

### Optional recommended
- Power BI Connector

---

## Expected file paths
Use these unless the repo already has a clearly better convention.

### Existing / likely existing
```text
installer/
.github/workflows/
```

### New / updated
```text
installer/
  GizmoSQL.wxs
  assets/
    quickstart.html
    launch-demo-server.ps1
    gizmosql-shell.cmd

installer-bundle/
  GizmoSQL.Bundle.wxs
  assets/
    license.rtf
    bundle-theme/

build/
  windows/
    sample-data/
      gizmosql-demo.duckdb

scripts/
  fetch-ui-msi.ps1
  fetch-powerbi-msi.ps1
  prepare-sample-db.ps1

docs/
  windows-install.md
  quickstart-windows.md
```

### Installed Windows locations
```text
C:\Program Files\GizmoSQL\
C:\ProgramData\GizmoSQL\samples\gizmosql-demo.duckdb
Start Menu > GizmoSQL
```

---

## Required artifacts
### Public recommended artifact
```text
GizmoSQL-Setup-x64.exe
```

### Keep available as advanced/manual artifacts
```text
GizmoSQL-Core-x64.msi
GizmoSQL-UI-x64.msi
GizmoSQL-PowerBI-Setup-x64.msi
```

---

## Strict task board

### EPIC A — Discover and lock the current packaging model

#### TASK A1
Inspect the current repository and document:
- current Core MSI authoring
- current Windows workflow(s)
- current signing flow
- current install behavior
- current PATH / shortcut behavior

#### TASK A2
Choose and implement one strategy for obtaining:
- UI MSI
- Power BI connector MSI

Allowed strategies:
- fetch release assets in CI
- consume staged local files in CI

Document the chosen strategy in code comments and build notes.

---

### EPIC B — Build the Burn bundle

#### TASK B1
Create a new Burn bundle project that builds:
- `GizmoSQL-Setup-x64.exe`

#### TASK B2
Chain:
- Core MSI
- UI MSI
- optional Power BI connector MSI

#### TASK B3
Expose bundle choices:
- GizmoSQL Core
- Sample Database
- GizmoSQL UI
- Power BI Connector

Defaults:
- Core required
- Sample Database on
- UI on
- Power BI optional

#### TASK B4
Add completion actions:
- Open GizmoSQL UI
- Launch Demo Server
- Open Quickstart Guide

If Power BI connector installed:
- show reminder to restart Power BI Desktop

---

### EPIC C — Improve Core MSI for first-run onboarding

#### TASK C1
Install `quickstart.html` locally.

#### TASK C2
Install `launch-demo-server.ps1` or equivalent locally.

#### TASK C3
Add Start Menu entries for:
- Launch Demo Server
- Open Quickstart
- GizmoSQL UI or UI handoff if appropriate

#### TASK C4
Preserve current Core MSI behavior for:
- binaries
- PATH
- upgrades
- uninstall

---

### EPIC D — Add sample DB experience

#### TASK D1
Create or stage a prebuilt sample DB:
- `gizmosql-demo.duckdb`

Preferred content:
- tiny TPC-H-derived demo dataset

#### TASK D2
Install sample DB by default to:
- `C:\ProgramData\GizmoSQL\samples\gizmosql-demo.duckdb`

#### TASK D3
Ensure the demo launcher points to this installed file.

#### TASK D4
Document uninstall cleanup behavior for the sample DB.

---

### EPIC E — Make UI the hero experience

#### TASK E1
Install UI by default via the bundle.

#### TASK E2
Make completion flow favor opening UI.

#### TASK E3
Update docs and Quickstart so UI is the default Windows first-run path.

#### TASK E4
Make the demo connection path obvious in Quickstart, and preconfigure it if feasible.

---

### EPIC F — Add Power BI connector option

#### TASK F1
Add Power BI connector as an optional checkbox in the bundle.

#### TASK F2
Install it only when selected.

#### TASK F3
Do not replace its existing MSI behavior; reuse it.

#### TASK F4
Add Power BI instructions to Quickstart:
- restart Power BI Desktop
- Get Data > Database > GizmoSQL
- host / port / authentication

---

### EPIC G — CI/CD and release flow

#### TASK G1
Update CI/CD so it:
- builds Core MSI
- fetches or stages UI MSI
- fetches or stages Power BI MSI
- stages sample DB
- builds Burn bundle
- signs Burn bundle correctly
- publishes bundle artifact

#### TASK G2
Keep individual MSI artifacts available.

#### TASK G3
Make the bundle the recommended Windows release artifact.

---

### EPIC H — Documentation and messaging

#### TASK H1
Update README so Windows users are directed first to:
- `GizmoSQL-Setup-x64.exe`

#### TASK H2
Label individual MSI downloads as:
- advanced/manual options

#### TASK H3
Document the 8-step journey.

#### TASK H4
Add troubleshooting notes for:
- UI not opening
- demo server not starting
- Power BI connector not appearing
- upgrade/reinstall concerns

---

### EPIC I — Verification

#### TASK I1
Test:
- clean install
- upgrade
- uninstall
- repair if applicable
- Core + UI + sample DB
- Core + UI + sample DB + Power BI

#### TASK I2
Validate the evaluator journey is 8 steps or fewer.

#### TASK I3
Validate the Power BI path as far as possible in the available environment.

#### TASK I4
Produce verification notes.

---

## Quickstart requirements
`quickstart.html` must include:

### Section 1 — Welcome
- confirm install success
- confirm sample DB installed

### Section 2 — Query in GizmoSQL UI
- open UI
- open demo connection
- run first query

### Section 3 — Query in Power BI
- restart Power BI Desktop
- Get Data > Database > GizmoSQL
- enter host/port
- authenticate

### Section 4 — Query from CLI / DBeaver
- local connection info
- demo credentials
- one example query

### Section 5 — Troubleshooting
- demo server fails
- UI missing
- connector not visible

---

## Demo launcher requirements
`launch-demo-server.ps1` must:
- reference the installed sample DB path
- launch GizmoSQL server against it
- print host / port / credentials / first query hint
- fail clearly if:
  - sample DB missing
  - server exe missing
  - port unavailable

---

## UX copy to use

### Installer options
**GizmoSQL Core**  
Server, client tools, and required runtime files.  
Required.

**Sample Database**  
Install a small demo database so you can run queries immediately.  
Recommended.

**GizmoSQL UI**  
Install the desktop UI for browsing data and running queries.  
Recommended.

**Power BI Connector**  
Install the GizmoSQL connector for Power BI Desktop.  
Recommended for Power BI users.

### Completion screen
**GizmoSQL is ready**

Choose what you want to do next:
- Open GizmoSQL UI
- Launch Demo Server
- Open Quickstart Guide

If Power BI connector installed:
- Restart Power BI Desktop before connecting.

---

## Definition of Done
- [ ] A Burn bundle project exists.
- [ ] The top-level recommended Windows artifact is `GizmoSQL-Setup-x64.exe`.
- [ ] The bundle installs Core MSI.
- [ ] The bundle installs UI MSI by default.
- [ ] The bundle offers Power BI connector MSI as an optional checkbox.
- [ ] A prebuilt sample DuckDB database is bundled or staged.
- [ ] The sample DB is installed by default.
- [ ] The sample DB is installed in a stable machine-wide path.
- [ ] `quickstart.html` is installed locally.
- [ ] `launch-demo-server.ps1` or equivalent is installed locally.
- [ ] Start Menu entries exist for Quickstart and Demo Launcher.
- [ ] Uninstall cleans up shortcuts correctly.
- [ ] UI is the default Windows first-run surface.
- [ ] The completion screen exposes a clear next action.
- [ ] The evaluator journey reaches first query in 8 steps or fewer.
- [ ] Quickstart includes Power BI instructions.
- [ ] CI builds the bundle EXE.
- [ ] CI stages or fetches the UI MSI.
- [ ] CI stages or fetches the Power BI MSI.
- [ ] CI includes the sample DB payload.
- [ ] CI signs the final bundle correctly.
- [ ] README makes the bundle the recommended Windows download.
- [ ] README labels individual MSIs as advanced/manual options.
- [ ] Clean install was tested.
- [ ] Upgrade path was tested or explicitly evaluated.
- [ ] Uninstall path was tested.
- [ ] Demo launcher was tested.
- [ ] Quickstart was tested.
- [ ] Power BI path was tested or clearly marked as pending.

---

## Output requirements
When done, return:

### 1. Summary
- what was implemented
- chosen package acquisition strategy
- chosen bundle structure

### 2. File changes
- exact files created
- exact files modified

### 3. Build and release changes
- workflow changes
- signing changes
- artifact changes

### 4. Verification
- how installer was tested
- whether 8-step path was achieved
- known limitations

### 5. Open issues
- blockers
- deferred work
- tradeoffs made

---

## Final instruction
Execute this as an implementation task.

Do not stop at analysis.
Inspect the repo, make the changes, wire the packaging, update the build/release flow, and return concrete file diffs plus verification notes.

When tradeoffs arise, prioritize:
1. fewer user steps
2. reuse of existing MSI assets
3. lower packaging risk
4. stable upgrade/uninstall behavior
