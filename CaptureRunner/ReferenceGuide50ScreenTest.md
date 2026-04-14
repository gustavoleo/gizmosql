# Reference Guide 50-Screen Test Set

This file defines the first concrete `50`-screen test campaign for the AnalyticsCreator reference guide.

It is derived from:

- [ReferenceGuideScreenshotGoNoGoQueue.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/ReferenceGuideScreenshotGoNoGoQueue.md)
- [ReferenceGuideScreenshotExecutionMatrix.md](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/ReferenceGuideScreenshotExecutionMatrix.md)
- [analyticscreator-master-report.json](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner/Output/analyticscreator-master-report.json)

## Selection Rule

The test set is intentionally split into three lanes:

- `42` screens from the current near-term queue:
  - `16` `Start now`
  - `26` `Needs exact plan row`
- `8` seeded-data candidates from already proven screen families

This gives a real `50`-screen scope without mixing in harness-only or manual-fallback work.

## Campaign Summary

- `Lane A` `Start now`: `16`
- `Lane B` `Needs exact plan row`: `26`
- `Lane C` `Seeded-data pilot`: `8`
- `Total`: `50`

## 50-Screen Set

| ID | target | lane | readiness | note |
|---|---|---|---|---|
| `1.6.2` | `Lists > Galaxies` | `Lane A` | `Start now` | Family already proven |
| `1.6.3` | `Lists > Layers` | `Lane A` | `Start now` | Family already proven |
| `1.6.4` | `Lists > Models` | `Lane A` | `Start now` | Family already proven |
| `1.6.6` | `Lists > Connectors` | `Lane A` | `Start now` | Family already proven |
| `1.6.7` | `Lists > Deploymens` | `Lane A` | `Start now` | Family already proven |
| `1.6.10` | `Lists > Hierarchies` | `Lane A` | `Start now` | Family already proven |
| `1.6.13` | `Lists > Indexes` | `Lane A` | `Start now` | Family already proven |
| `1.6.14` | `Lists > Macros` | `Lane A` | `Start now` | Family already proven |
| `1.6.16` | `Lists > Object Scripts` | `Lane A` | `Start now` | Family already proven |
| `1.6.17` | `Lists > Packages` | `Lane A` | `Start now` | Family already proven |
| `1.6.18` | `Lists > Parameters` | `Lane A` | `Start now` | Family already proven |
| `1.6.19` | `Lists > Partitions` | `Lane A` | `Start now` | Family already proven |
| `1.6.20` | `Lists > Predefined transformations` | `Lane A` | `Start now` | Family already proven |
| `1.6.22` | `Lists > OLAP roles` | `Lane A` | `Start now` | Family already proven |
| `1.6.23` | `Lists > SQL Script` | `Lane A` | `Start now` | Family already proven |
| `1.6.25` | `Lists > Snapshots` | `Lane A` | `Start now` | Family already proven |
| `1.5.8` | `Pages > Model Dimension` | `Lane B` | `Needs exact plan row` | Simple-route page |
| `1.5.9` | `Pages > Model Fact` | `Lane B` | `Needs exact plan row` | Simple-route page |
| `1.6.1` | `Lists > Encrypted strings` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.5` | `Lists > Schemas` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.8` | `Lists > Exports` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.9` | `Lists > Object group content` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.11` | `Lists > Historizations` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.12` | `Lists > Imports` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.15` | `Lists > Tables` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.21` | `Lists > Table references` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.24` | `Lists > Snapshot groups` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.26` | `Lists > Sources` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.27` | `Lists > Source references` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.28` | `Lists > Datamart stars` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.29` | `Lists > Transformations` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.6.30` | `Lists > User groups` | `Lane B` | `Needs exact plan row` | Simple-route list |
| `1.7.1` | `Dialogs > About` | `Lane B` | `Needs exact plan row` | Simple-route dialog |
| `1.7.2` | `Dialogs > Interface settings` | `Lane B` | `Needs exact plan row` | Simple-route dialog |
| `1.7.3` | `Dialogs > DWH settings` | `Lane B` | `Needs exact plan row` | Simple-route dialog |
| `1.7.5` | `Dialogs > EULA` | `Lane B` | `Needs exact plan row` | Simple-route dialog |
| `1.7.8` | `Dialogs > Open/save in cloud` | `Lane B` | `Needs exact plan row` | Simple-route dialog |
| `1.7.17` | `Dialogs > Synchronize DWH` | `Lane B` | `Needs exact plan row` | Simple-route dialog |
| `1.8.3` | `Wizards > DWH wizard` | `Lane B` | `Needs exact plan row` | Simple-route wizard |
| `1.8.9` | `Wizards > Create source` | `Lane B` | `Needs exact plan row` | Simple-route wizard |
| `1.8.12` | `Wizards > Create DataVault object` | `Lane B` | `Needs exact plan row` | Simple-route wizard |
| `1.8.13` | `Wizards > Run object script` | `Lane B` | `Needs exact plan row` | Simple-route wizard |
| `1.5.1` | `Pages > Connector` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.6` | `Pages > Index` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.11` | `Pages > Object script` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.12` | `Pages > Package` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.13` | `Pages > OLAP Partition` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.15` | `Pages > Predefined transformation` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.17` | `Pages > OLAP role` | `Lane C` | `Needs seeded data` | Surrounding family already proven |
| `1.5.18` | `Pages > SQL Script` | `Lane C` | `Needs seeded data` | Surrounding family already proven |

## Recommended Execution Order

1. Execute `Lane A` first.
2. Author and validate exact plan rows for `Lane B`.
3. Seed deterministic data and then execute `Lane C`.

## Success Definition

This 50-screen campaign is complete when:

- all `16` `Lane A` screens have reports and captures
- all `26` `Lane B` screens have curated plan rows and validated reports
- all `8` `Lane C` screens have deterministic seeded-data recipes and validated reports
