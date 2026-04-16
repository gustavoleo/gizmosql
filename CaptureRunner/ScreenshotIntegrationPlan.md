# Screenshot Integration Plan

This document captures the current screenshot architecture for `CaptureRunner`.

## Decision summary

Use the native PNG capture backend as the default screenshot engine.

Keep `Snagit` as optional support for legacy plan-driven or manual-assisted runs only.

Why:

- `--map-ui` already forces native capture in code
- native capture gives a deterministic file path and direct PNG output
- the mapper validates PNG evidence immediately, including `600 DPI` metadata
- the primary automation lane no longer depends on external preset state

## Current production pipeline

1. start or attach to `AnalyticsCreator`
2. run bootstrap and verify `Northwind`
3. traverse a safe UIA route
4. validate anchors on the resulting surface
5. capture the window or client area as PNG
6. validate the PNG metadata and existence
7. classify the screen into `accepted`, `review`, `rejected`, or `blocked`
8. write the evidence bundle and map artifacts

## Native capture rules

- backend: `native`
- file format: `PNG`
- default DPI metadata: `600`
- capture area: `window` by default, `client` when requested
- output root: provided by `--screenshot-output-root` for mapper runs

For `--map-ui`, native capture and `600 DPI` are not an optional documentation preference. They are the enforced default behavior.

## Resolution reality

Important constraint:

- `600 DPI` is metadata, not extra pixels
- screenshot fidelity is limited by the pixels actually shown on the capture surface
- changing DPI after capture does not create new detail

The current system uses `600 DPI` because downstream document workflows want print metadata, not because it creates new source information.

## Evidence contract

An accepted screen must have both:

- passing navigation and validation evidence
- a real PNG screenshot file

The mapper writes:

- `startup-state.json`
- `ui-map.json`
- `UiMap.md`
- `coverage-summary.json`
- `rejected-routes.json`
- `remaining-queued-routes.json`
- `route-promotion-summary.json`

Per-screen evidence is written under the screenshot root, typically in bucket folders such as:

- `accepted`
- `review`
- `rejected`
- `blocked`

Each evidence bundle may include route JSON, snapshot JSON, report JSON, and PNG output.

## Screen classification

### accepted

Use this only when:

- route replay succeeded
- validation anchors passed
- PNG exists
- PNG evidence validation passed

### review

Use this when:

- the surface opened
- capture exists or nearly exists
- one part of the validation evidence is weaker than the acceptance bar

### rejected

Use this when:

- navigation failed
- validation failed
- wrong window was captured
- PNG evidence is missing or invalid

### blocked

Use this when:

- the route is unsafe
- the surface needs seeded data or special state not currently available
- the screen is manual-only or custom-rendered in a way that fails the current automation contract

## Cleanup rules

Screenshot generation must not leave surfaces open.

Current rule:

- dialogs, wizards, and detail surfaces must be closed explicitly after capture
- use UIA `Cancel`, `Close`, or `Back` first
- use scoped `Esc` only as fallback for the active modal

Leaving screens open is treated as a traversal bug because it distorts the next route and can crash the application.

## Route-promotion rule

Screenshot expansion follows the mapper artifacts, not the workbook by itself.

After a fresh run:

1. inspect `rejected-routes.json`
2. if empty, inspect `remaining-queued-routes.json`
3. use `route-promotion-summary.json` to group only those real stalls
4. add recipes only for the routes that appear there

## Optional Snagit lane

Snagit still exists for legacy plan-driven runs. It can be useful when you intentionally want a manual-assisted lane or when you are validating an older authored batch.

It is not the default screenshot engine for automatic UI mapping.

## Current proof point

Latest fresh cold-start baseline:

- [Output/bfs-production-fresh3/startup-state.json](Output/bfs-production-fresh3/startup-state.json)
- [Output/bfs-production-fresh3/ui-map.json](Output/bfs-production-fresh3/ui-map.json)
- [Output/bfs-production-fresh3/coverage-summary.json](Output/bfs-production-fresh3/coverage-summary.json)
- [Output/bfs-production-fresh3/rejected-routes.json](Output/bfs-production-fresh3/rejected-routes.json)
- [Output/bfs-production-fresh3/remaining-queued-routes.json](Output/bfs-production-fresh3/remaining-queued-routes.json)
- [Output/bfs-production-fresh3/route-promotion-summary.json](Output/bfs-production-fresh3/route-promotion-summary.json)

Result:

- `32 / 32` accepted
- zero rejected routes
- zero remaining queued routes
