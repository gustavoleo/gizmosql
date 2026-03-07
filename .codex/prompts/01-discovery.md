# Discovery Prompt

Read `AGENTS.md` first.

Your task in this step is discovery only. Do not implement yet.

## Objectives
1. Inspect the repository’s current Windows installer/build layout.
2. Confirm the current Core MSI authoring path.
3. Confirm how the current Windows workflows build and sign installers.
4. Identify the cleanest strategy for acquiring or staging:
   - GizmoSQL UI MSI
   - Power BI connector MSI
5. Identify the best location and strategy for bundling the sample DB.
6. Identify the least risky Burn integration approach.

## Required outputs
Return:
- current installer architecture summary
- current workflow/build summary
- risks and constraints
- recommended file changes
- recommended build changes
- recommended artifact flow
- any blockers or uncertainties

## Rules
- Do not make code changes in this step.
- Be concrete about file paths and workflow names.
- Prefer reuse of existing assets over repackaging.
- Call out anything that could break upgrade/uninstall stability.
