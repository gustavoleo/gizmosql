# Bundle Theme Notes

Phase 1 uses a custom WixStdBA theme instead of the stock `rtfLargeLicense` theme.

Why:

- the options page needs explicit checkboxes for Core, Sample Database, UI, and Power BI
- the success page needs a post-install action chooser while still using the standard BA

The theme binds directly to Burn variables through control names:

- `InstallCore=1` keeps the required Core package visibly locked on
- `InstallSampleDb=1` installs the bundled demo DuckDB file through the Core MSI
- `InstallUi=1` installs the separately released GizmoSQL UI MSI
- `InstallPowerBI=1` installs the separately released Power BI connector MSI
- `PostInstallAction` drives the final `LaunchTarget` arguments for `gizmosql-shell.cmd`
