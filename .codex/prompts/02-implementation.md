# Implementation Prompt

Read `AGENTS.md` first.

Your task in this step is implementation.

## Objectives
Implement the Phase 1 Windows first-run experience according to `AGENTS.md`.

## Required implementation areas
1. Add a Burn bundle project producing `GizmoSQL-Setup-x64.exe`.
2. Chain Core MSI.
3. Chain UI MSI by default.
4. Add optional Power BI connector MSI checkbox.
5. Add or stage a prebuilt sample DB.
6. Install Quickstart assets.
7. Install demo launcher.
8. Add Start Menu or completion-flow first-run actions.
9. Update CI/CD to build/sign/publish the bundle.
10. Update README/release messaging so the bundle is the recommended Windows path.

## Output requirements
Return:
- summary of what was implemented
- exact files created
- exact files modified
- exact build/workflow changes
- any assumptions made
- any unresolved issues

## Rules
- Reuse existing MSI assets wherever possible.
- Do not redesign UI or the Power BI connector internals.
- Keep changes low-risk.
- Preserve current MSI behavior unless change is required for the journey.
- Prefer stable package identities and coherent upgrade/uninstall behavior.
