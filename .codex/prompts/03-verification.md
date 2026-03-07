# Verification Prompt

Read `AGENTS.md` first.

Your task in this step is verification.

## Objectives
Validate that the implemented Windows installer experience meets the Phase 1 goals.

## Required checks
1. Verify the bundle builds successfully.
2. Verify Core MSI is included.
3. Verify UI MSI is included by default.
4. Verify Power BI connector is optional.
5. Verify sample DB is staged/installed by default.
6. Verify Quickstart is installed.
7. Verify demo launcher is installed.
8. Verify completion actions or Start Menu path exist.
9. Verify the README makes the bundle the recommended Windows download.
10. Verify the evaluator journey can reach first query in 8 steps or fewer.

## Required output
Return a pass/fail checklist for:
- architecture
- packaging
- docs
- onboarding assets
- build/release flow
- first-run journey
- Power BI path

Also return:
- known limitations
- anything not testable in the current environment
- recommended next fixes if something failed

## Rules
- Be explicit.
- Mark anything untested as untested.
- Do not claim success for anything you could not verify.
