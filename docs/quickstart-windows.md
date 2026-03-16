# Windows Quickstart

## 8-step evaluator path

1. Download `GizmoSQL-Setup-x64.exe`
2. Run the installer
3. Keep the default options enabled
4. Install
5. Finish
6. Open GizmoSQL UI
7. Open the demo connection in the UI
8. Run the first query

The default `Open GizmoSQL UI` completion action also attempts to start the local demo server automatically when port `31337` is free. If you skip that action, use `Start Menu > GizmoSQL > Launch Demo Server`.

## Demo connection

Start Menu:

- `GizmoSQL > Open GizmoSQL UI`
- `GizmoSQL > Launch Demo Server`
- `GizmoSQL > Open Quickstart`

Demo server defaults:

- Host: `127.0.0.1`
- Port: `31337`
- Username: `gizmosql_user`
- Password: `gizmosql_password`
- TLS: off for the local demo path

Bundled demo DB:

- `C:\ProgramData\GizmoSQL\samples\gizmosql-demo.duckdb`

Example query:

```sql
SELECT customer_name, order_total
FROM demo_recent_orders
ORDER BY order_total DESC
LIMIT 10;
```

## Power BI path

1. Run `GizmoSQL-Setup-x64.exe`
2. Select `Power BI Connector`
3. Install
4. Restart Power BI Desktop
5. Launch the demo server
6. In Power BI: `Get Data > Database > GizmoSQL`
7. Enter `127.0.0.1` and `31337`
8. Authenticate with `gizmosql_user` / `gizmosql_password`

## Troubleshooting

### UI does not open

Re-run the bundle and keep `GizmoSQL UI` enabled, or install `GizmoSQL-UI-x64.msi` manually.

### Demo server does not start

Re-run `Launch Demo Server` from the Start Menu and read the PowerShell window. The default `Open GizmoSQL UI` action also tries to start the demo server in a minimized PowerShell window when the port is available. The launcher checks for:

- missing `gizmosql_server.exe`
- missing sample DB
- port `31337` already in use

### Power BI connector does not appear

Restart Power BI Desktop after install. If it still does not show up, re-run setup with the connector enabled or install `GizmoSQL-PowerBI-Setup-x64.msi` manually.

### Upgrade or reinstall concerns

Re-running `GizmoSQL-Setup-x64.exe` preserves the split-product model. Core, UI, and Power BI continue to upgrade through their own MSI identities while the bundle remains the main user-facing entry point.
