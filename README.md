# 🚀 GizmoSQL — High-Performance SQL Server for the Cloud

[![DockerHub](https://img.shields.io/badge/dockerhub-image-green.svg?logo=Docker)](https://hub.docker.com/r/gizmodata/gizmosql)
[![GitHub Container](https://img.shields.io/badge/github--package-container--image-green.svg?logo=Docker)](https://github.com/gizmodata/gizmosql/pkgs/container/gizmosql)
[![Documentation](https://img.shields.io/badge/Documentation-dev-yellow.svg)](https://arrow.apache.org/docs/format/FlightSql.html)
[![GitHub](https://img.shields.io/badge/GitHub-gizmodata%2Fgizmosql-blue.svg?logo=Github)](https://github.com/gizmodata/gizmosql)
[![JDBC Driver](https://img.shields.io/badge/GizmoSQL%20JDBC%20Driver-download%20artifact-red?logo=Apache%20Maven)](https://downloads.gizmodata.com/gizmosql-jdbc-driver/latest/gizmosql-jdbc-driver.jar)
[![ADBC PyPI](https://img.shields.io/badge/PyPI-GizmoSQL%20ADBC%20Driver-blue?logo=PyPI)](https://pypi.org/project/adbc-driver-gizmosql/)
[![SQLAlchemy Dialect](https://img.shields.io/badge/PyPI-GizmoSQL%20SQLAlchemy%20Dialect-blue?logo=PyPI)](https://pypi.org/project/sqlalchemy-gizmosql-adbc-dialect/)
[![Ibis Backend](https://img.shields.io/badge/PyPI-GizmoSQL%20Ibis%20Backend-blue?logo=PyPI)](https://pypi.org/project/ibis-gizmosql/)

---

## 🌟 What is GizmoSQL?

**GizmoSQL** is a lightweight, high-performance SQL server built on:

- 🦆 [DuckDB](https://duckdb.org) or 🗃️ [SQLite](https://sqlite.org) for query execution
- 🚀 [Apache Arrow Flight SQL](https://arrow.apache.org/docs/format/FlightSql.html) for fast, modern connectivity
- 🔒 Middleware-based auth with optional TLS & JWT

Originally forked from [`sqlflite`](https://github.com/voltrondata/sqlflite) — and now enhanced into a more extensible, production-ready platform under the Apache 2.0 license.

---

## 📦 Editions

GizmoSQL is available in two editions:

| Feature | Core | Enterprise |
|---------|:----:|:----------:|
| DuckDB & SQLite backends | ✅ | ✅ |
| Arrow Flight SQL protocol | ✅ | ✅ |
| TLS & mTLS authentication | ✅ | ✅ |
| JWT token authentication | ✅ | ✅ |
| Query timeout | ✅ | ✅ |
| Session Instrumentation | ❌ | ✅ |
| Kill Session | ❌ | ✅ |
| Per-Catalog Permissions | ❌ | ✅ |
| SSO/OIDC Authentication (JWKS) | ❌ | ✅ |
| Authorized Email Filtering | ❌ | ✅ |

**GizmoSQL Core** is free and open source under the Apache 2.0 license.

**GizmoSQL Enterprise** requires a commercial license. Contact [sales@gizmodata.com](mailto:sales@gizmodata.com) for licensing information.

For more details, see the [Editions documentation](https://docs.gizmosql.com/#/editions).

---

## 🧠 Why GizmoSQL?

- 🛰️ **Deploy Anywhere** — Run as a container, native binary, or in Kubernetes
- 📦 **Columnar Fast** — Leverages Arrow columnar format for high-speed transfers
- ⚙️ **Dual Backends** — Switch between DuckDB and SQLite at runtime
- 🔐 **Built-in TLS + Auth** — Password-based login + signed JWT tokens
- 📈 **Super Cheap Analytics** — TPC-H SF 1000 in 161s for ~$0.17 on Azure
- 🧪 **CLI, Python, JDBC, SQLAlchemy, Ibis, WebSocket** — Pick your interface

---

## 📦 Component Versions

| Component                                                                        | Version |
|----------------------------------------------------------------------------------|---------|
| [DuckDB](https://duckdb.org)                                                     | v1.4.4  |
| [SQLite](https://sqlite.org)                                                     | 3.51.1  |
| [Apache Arrow (Flight SQL)](https://arrow.apache.org/docs/format/FlightSql.html) | 23.0.1 |
| [jwt-cpp](https://thalhammer.github.io/jwt-cpp/)                                 | v0.7.1  |
| [nlohmann/json](https://json.nlohmann.me)                                        | v3.12.0 |

## 📚 Documentation

Primary docs:

- [GizmoSQL Documentation](https://docs.gizmosql.com)
- [Windows install guide](docs/windows-install.md)
- [Windows quickstart](docs/quickstart-windows.md)
- [Client shell documentation](docs/client.md)
- [Editions](docs/editions.md)
- [Contributing guide](CONTRIBUTING.md)

Internal engineering docs and tooling:

- [CaptureRunner README](CaptureRunner/README.md)
- [CaptureRunner Authoring Guide](CaptureRunner/AuthoringGuide.md)
- [CaptureRunner Current Status](CaptureRunner/CurrentStatus.md)
- [CaptureRunner Quick Start](CaptureRunner/QuickStart_EN.md)
- [CaptureRunner Navigation Map](CaptureRunner/NavigationMap.md)

---

## 🗂️ Repository Inventory

This is the top-level map of the repository. Use it to find the right starting point before you edit anything.

| Path | What it contains | Start here when you need to... |
|---|---|---|
| [README.md](/mnt/e/DDD/GitHub/gizmosql/README.md) | Product overview, install paths, client entry points. | Understand the repo at a high level. |
| [CMakeLists.txt](/mnt/e/DDD/GitHub/gizmosql/CMakeLists.txt) | Root build orchestration for the server, client, and tests. | Change how the core project builds. |
| [src/client](/mnt/e/DDD/GitHub/gizmosql/src/client) | CLI client, shell loop, command processing, output rendering, auth flows. | Work on the interactive client or CLI behavior. |
| [src/common](/mnt/e/DDD/GitHub/gizmosql/src/common) | Shared server/runtime code: logging, telemetry, security, middleware, health. | Change shared runtime behavior used by multiple backends. |
| [src/duckdb](/mnt/e/DDD/GitHub/gizmosql/src/duckdb) | DuckDB backend implementation. | Change DuckDB execution behavior. |
| [src/sqlite](/mnt/e/DDD/GitHub/gizmosql/src/sqlite) | SQLite backend implementation. | Change SQLite execution behavior. |
| [src/enterprise](/mnt/e/DDD/GitHub/gizmosql/src/enterprise) | Enterprise-only features such as instrumentation, OAuth, JWKS, kill session, permissions. | Work on licensed features. |
| [src/protos](/mnt/e/DDD/GitHub/gizmosql/src/protos) | Protobuf and gRPC contract sources. | Change wire contracts or generated protocol inputs. |
| [src/gizmosql_server.cpp](/mnt/e/DDD/GitHub/gizmosql/src/gizmosql_server.cpp) | Main server entry point. | Change server startup wiring. |
| [tests](/mnt/e/DDD/GitHub/gizmosql/tests) | Integration and script-driven tests for server, client, auth, telemetry, and backends. | Add or run automated verification. |
| [docs](/mnt/e/DDD/GitHub/gizmosql/docs) | Public documentation site content. | Update user-facing docs. |
| [installer](/mnt/e/DDD/GitHub/gizmosql/installer) | Core Windows MSI authoring and installed assets such as quickstart and demo launcher. | Change the core Windows MSI. |
| [installer-bundle](/mnt/e/DDD/GitHub/gizmosql/installer-bundle) | WiX Burn bundle for `GizmoSQL-Setup-x64.exe`, including theme and license assets. | Change the top-level Windows installer EXE. |
| [build/windows/sample-data](/mnt/e/DDD/GitHub/gizmosql/build/windows/sample-data) | Bundled demo DuckDB database payload used by the Windows installer. | Update the shipped sample database. |
| [scripts](/mnt/e/DDD/GitHub/gizmosql/scripts) | Build helpers, release fetch scripts, sample-data prep, test helpers, installer validation scripts. | Automate packaging, downloads, or repo workflows. |
| [queries](/mnt/e/DDD/GitHub/gizmosql/queries) | TPC-H and benchmark SQL assets. | Run or adjust benchmark queries. |
| [helm-chart](/mnt/e/DDD/GitHub/gizmosql/helm-chart) | Kubernetes packaging for GizmoSQL deployment. | Change Helm-based deployment behavior. |
| [third_party](/mnt/e/DDD/GitHub/gizmosql/third_party) | Dependency build definitions and patching glue for vendored components. | Adjust dependency versions or build integration. |
| [tls](/mnt/e/DDD/GitHub/gizmosql/tls) | Local certificate generation helpers and TLS notes. | Work on development TLS setup. |
| [CaptureRunner](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner) | Windows UI automation bootstrap, mapper, native PNG capture tooling, navigation plans, reports, and reference-guide workflow docs. | Work on AnalyticsCreator screen discovery, verification, and screenshot automation. |
| [CaptureRunner.Tests](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner.Tests) | Unit tests for CaptureRunner monitor logic, placement logic, and toolbar navigation helpers. | Verify CaptureRunner behavior safely. |

### Quick Orientation By Task

- Want to change server behavior: start in [src/common](/mnt/e/DDD/GitHub/gizmosql/src/common), [src/duckdb](/mnt/e/DDD/GitHub/gizmosql/src/duckdb), or [src/sqlite](/mnt/e/DDD/GitHub/gizmosql/src/sqlite).
- Want to change the CLI: start in [src/client](/mnt/e/DDD/GitHub/gizmosql/src/client).
- Want to change public docs: start in [docs](/mnt/e/DDD/GitHub/gizmosql/docs).
- Want to change Windows packaging: start in [installer](/mnt/e/DDD/GitHub/gizmosql/installer) and [installer-bundle](/mnt/e/DDD/GitHub/gizmosql/installer-bundle).
- Want to change screenshot automation: start in [CaptureRunner](/mnt/e/DDD/GitHub/gizmosql/CaptureRunner).

### Second-Level Engineering Map

#### `src/`

Use this when you need to know where server and client code actually lives under the main source tree.

| Path | What it contains | Start here when you need to... |
|---|---|---|
| [src/client](/mnt/e/DDD/GitHub/gizmosql/src/client) | Interactive shell, CLI commands, rendering, connection handling, auth prompts. | Change what the user sees and does in the CLI. |
| [src/common](/mnt/e/DDD/GitHub/gizmosql/src/common) | Shared runtime code used by the server and backends. | Change core behavior that cuts across the product. |
| [src/common/include](/mnt/e/DDD/GitHub/gizmosql/src/common/include) | Shared public headers for common runtime code. | Change common interfaces or shared declarations. |
| [src/duckdb](/mnt/e/DDD/GitHub/gizmosql/src/duckdb) | DuckDB adapter and execution integration. | Work on DuckDB-specific query execution or connection behavior. |
| [src/sqlite](/mnt/e/DDD/GitHub/gizmosql/src/sqlite) | SQLite adapter and execution integration. | Work on SQLite-specific behavior. |
| [src/enterprise](/mnt/e/DDD/GitHub/gizmosql/src/enterprise) | Enterprise feature area root. | Work on licensed or gated capabilities. |
| [src/enterprise/catalog_permissions](/mnt/e/DDD/GitHub/gizmosql/src/enterprise/catalog_permissions) | Catalog-level permissions logic. | Change enterprise authorization over schemas, tables, or catalogs. |
| [src/enterprise/instrumentation](/mnt/e/DDD/GitHub/gizmosql/src/enterprise/instrumentation) | Enterprise instrumentation and observability hooks. | Change enterprise telemetry or tracing behavior. |
| [src/enterprise/jwks](/mnt/e/DDD/GitHub/gizmosql/src/enterprise/jwks) | JWKS resolution and key management. | Work on token verification key loading. |
| [src/enterprise/kill_session](/mnt/e/DDD/GitHub/gizmosql/src/enterprise/kill_session) | Session termination controls. | Change enterprise session management. |
| [src/enterprise/license_mgr](/mnt/e/DDD/GitHub/gizmosql/src/enterprise/license_mgr) | License validation and enterprise gating. | Change license checks or tier enforcement. |
| [src/enterprise/oauth](/mnt/e/DDD/GitHub/gizmosql/src/enterprise/oauth) | OAuth and enterprise identity integration. | Work on enterprise authentication flows. |
| [src/protos](/mnt/e/DDD/GitHub/gizmosql/src/protos) | Protocol source definitions. | Change request/response contracts. |
| [src/protos/grpc](/mnt/e/DDD/GitHub/gizmosql/src/protos/grpc) | gRPC-facing protocol definitions and generated-input sources. | Change gRPC service boundaries. |
| [src/gizmosql_server.cpp](/mnt/e/DDD/GitHub/gizmosql/src/gizmosql_server.cpp) | Main server process entry point. | Change startup wiring, initialization order, or process boot behavior. |

#### `installer/`

Use this when you are changing the Windows Core MSI or the assets it installs.

| Path | What it contains | Start here when you need to... |
|---|---|---|
| [installer/GizmoSQL.wxs](/mnt/e/DDD/GitHub/gizmosql/installer/GizmoSQL.wxs) | Main WiX authoring for the Core MSI. | Change MSI components, features, shortcuts, or installed files. |
| [installer/assets](/mnt/e/DDD/GitHub/gizmosql/installer/assets) | Files installed with the MSI for onboarding and launch flows. | Change the files users receive after install. |
| [installer/assets/quickstart.html](/mnt/e/DDD/GitHub/gizmosql/installer/assets/quickstart.html) | Local Windows quickstart page. | Change post-install onboarding content. |
| [installer/assets/launch-demo-server.ps1](/mnt/e/DDD/GitHub/gizmosql/installer/assets/launch-demo-server.ps1) | Demo server launcher script. | Change first-run demo startup behavior. |
| [installer/assets/gizmosql-shell.cmd](/mnt/e/DDD/GitHub/gizmosql/installer/assets/gizmosql-shell.cmd) | Windows shell helper entry point. | Change how the installed shell is invoked from shortcuts or Start Menu entries. |
| [installer/gizmosql.ico](/mnt/e/DDD/GitHub/gizmosql/installer/gizmosql.ico) | Primary installer or app icon asset. | Change Windows icon branding. |
| [installer/gizmosql_client.ico](/mnt/e/DDD/GitHub/gizmosql/installer/gizmosql_client.ico) | Client-specific icon asset. | Change client shortcut branding. |
| [installer/gizmosql_logo.png](/mnt/e/DDD/GitHub/gizmosql/installer/gizmosql_logo.png) | Installer/logo bitmap asset. | Change Windows packaging visuals. |

#### `scripts/`

Use this when you need automation around builds, releases, sample data, or validation.

| Path | What it contains | Start here when you need to... |
|---|---|---|
| [scripts/fetch-ui-msi.ps1](/mnt/e/DDD/GitHub/gizmosql/scripts/fetch-ui-msi.ps1) | Fetch or stage the GizmoSQL UI MSI. | Control how the Windows bundle gets the UI installer. |
| [scripts/fetch-powerbi-msi.ps1](/mnt/e/DDD/GitHub/gizmosql/scripts/fetch-powerbi-msi.ps1) | Fetch or stage the Power BI connector MSI. | Control how the Windows bundle gets the Power BI installer. |
| [scripts/prepare-sample-db.ps1](/mnt/e/DDD/GitHub/gizmosql/scripts/prepare-sample-db.ps1) | Prepare or stage the bundled sample database payload. | Update the Windows demo database flow. |
| [scripts/test-windows-installer-flow.ps1](/mnt/e/DDD/GitHub/gizmosql/scripts/test-windows-installer-flow.ps1) | Installer verification helper. | Validate the Windows setup journey. |
| [scripts/create_duckdb_database_file.py](/mnt/e/DDD/GitHub/gizmosql/scripts/create_duckdb_database_file.py) | Python helper for creating DuckDB test or sample files. | Generate local DuckDB assets. |
| [scripts/start_gizmosql.sh](/mnt/e/DDD/GitHub/gizmosql/scripts/start_gizmosql.sh) | Main Linux/macOS server start helper. | Start the server locally with the standard script path. |
| [scripts/start_gizmosql_slim.sh](/mnt/e/DDD/GitHub/gizmosql/scripts/start_gizmosql_slim.sh) | Slim server start helper. | Start a leaner local runtime variant. |
| [scripts/test_gizmosql.py](/mnt/e/DDD/GitHub/gizmosql/scripts/test_gizmosql.py) | Python-based test helper for GizmoSQL validation. | Run or extend scripted validation in Python. |
| [scripts/test_gizmosql.sh](/mnt/e/DDD/GitHub/gizmosql/scripts/test_gizmosql.sh) | Shell-based test helper for GizmoSQL validation. | Run shell-driven verification quickly. |
| [scripts/test_telemetry.sh](/mnt/e/DDD/GitHub/gizmosql/scripts/test_telemetry.sh) | Telemetry test helper. | Validate telemetry flows. |
| [scripts/docker-compose.telemetry-test.yaml](/mnt/e/DDD/GitHub/gizmosql/scripts/docker-compose.telemetry-test.yaml) | Docker Compose setup for telemetry testing. | Stand up the telemetry test environment. |

#### `tests/`

Use this when you need to find the right verification entry point before adding or changing behavior.

| Path | What it contains | Start here when you need to... |
|---|---|---|
| [tests/CMakeLists.txt](/mnt/e/DDD/GitHub/gizmosql/tests/CMakeLists.txt) | Test build wiring for the C++ test targets. | Change how the compiled test suite is built. |
| [tests/integration](/mnt/e/DDD/GitHub/gizmosql/tests/integration) | Main C++ integration test suite for server, backends, auth, and enterprise behavior. | Add or debug deeper product-level integration coverage. |
| [tests/integration/test_authentication.cpp](/mnt/e/DDD/GitHub/gizmosql/tests/integration/test_authentication.cpp) | Authentication path coverage. | Change login or auth behavior and need to verify it. |
| [tests/integration/test_catalog_access.cpp](/mnt/e/DDD/GitHub/gizmosql/tests/integration/test_catalog_access.cpp) | Catalog access behavior coverage. | Validate schema or catalog visibility behavior. |
| [tests/integration/test_catalog_permissions_enterprise.cpp](/mnt/e/DDD/GitHub/gizmosql/tests/integration/test_catalog_permissions_enterprise.cpp) | Enterprise catalog-permission coverage. | Verify enterprise authorization behavior. |
| [tests/integration/test_interactive_client.cpp](/mnt/e/DDD/GitHub/gizmosql/tests/integration/test_interactive_client.cpp) | Interactive client integration coverage. | Validate CLI shell behavior from compiled tests. |
| [tests/integration/test_sqlite_backend.cpp](/mnt/e/DDD/GitHub/gizmosql/tests/integration/test_sqlite_backend.cpp) | SQLite backend integration coverage. | Validate SQLite-specific changes. |
| [tests/integration/test_tpch_benchmark.cpp](/mnt/e/DDD/GitHub/gizmosql/tests/integration/test_tpch_benchmark.cpp) | Benchmark-oriented TPC-H coverage. | Check performance-oriented or benchmark query behavior. |
| [tests/test_bulk_ingest.py](/mnt/e/DDD/GitHub/gizmosql/tests/test_bulk_ingest.py) | Python-level bulk-ingest validation. | Quickly test ingestion flows from Python. |
| [tests/test_geoarrow.py](/mnt/e/DDD/GitHub/gizmosql/tests/test_geoarrow.py) | Python-level GeoArrow validation. | Check geometry/GeoArrow behavior quickly. |
| [tests/test_pivot_multi_statement.py](/mnt/e/DDD/GitHub/gizmosql/tests/test_pivot_multi_statement.py) | Python-level multi-statement pivot validation. | Validate multi-statement SQL behavior from Python. |
| [tests/test_client_shell.sh](/mnt/e/DDD/GitHub/gizmosql/tests/test_client_shell.sh) | Shell-driven CLI verification. | Smoke test the client shell quickly from a script. |

#### `docs/`

Use this when you need to update the public documentation site or find the right user-facing page.

| Path | What it contains | Start here when you need to... |
|---|---|---|
| [docs/index.html](/mnt/e/DDD/GitHub/gizmosql/docs/index.html) | Docs site entry page. | Change the public landing experience for the docs site. |
| [docs/documentation.md](/mnt/e/DDD/GitHub/gizmosql/docs/documentation.md) | General documentation hub content. | Change top-level public documentation structure. |
| [docs/client.md](/mnt/e/DDD/GitHub/gizmosql/docs/client.md) | CLI and client documentation. | Update shell usage or client-facing instructions. |
| [docs/windows-install.md](/mnt/e/DDD/GitHub/gizmosql/docs/windows-install.md) | Windows installer guidance. | Update Windows installation steps or troubleshooting. |
| [docs/quickstart-windows.md](/mnt/e/DDD/GitHub/gizmosql/docs/quickstart-windows.md) | Windows first-run quickstart. | Update the post-install Windows journey. |
| [docs/editions.md](/mnt/e/DDD/GitHub/gizmosql/docs/editions.md) | Edition and packaging comparison. | Clarify product tiers or feature packaging. |
| [docs/integrations.md](/mnt/e/DDD/GitHub/gizmosql/docs/integrations.md) | Integration entry points and ecosystem docs. | Update client or tool integration guidance. |
| [docs/oauth_sso_setup.md](/mnt/e/DDD/GitHub/gizmosql/docs/oauth_sso_setup.md) | OAuth SSO setup guide. | Update identity-provider setup instructions. |
| [docs/token_authentication.md](/mnt/e/DDD/GitHub/gizmosql/docs/token_authentication.md) | Token-authentication guide. | Change token-based auth documentation. |
| [docs/opentelemetry.md](/mnt/e/DDD/GitHub/gizmosql/docs/opentelemetry.md) | OpenTelemetry guide. | Update telemetry or tracing documentation. |
| [docs/session_instrumentation.md](/mnt/e/DDD/GitHub/gizmosql/docs/session_instrumentation.md) | Session instrumentation documentation. | Update observability guidance for sessions. |
| [docs/ducklake.md](/mnt/e/DDD/GitHub/gizmosql/docs/ducklake.md) | DuckLake documentation. | Update DuckLake integration or usage guidance. |
| [docs/bulk_ingestion.md](/mnt/e/DDD/GitHub/gizmosql/docs/bulk_ingestion.md) | Bulk ingestion guide. | Update ingestion workflow documentation. |
| [docs/geometry.md](/mnt/e/DDD/GitHub/gizmosql/docs/geometry.md) | Geometry feature documentation. | Update geospatial or geometry user guidance. |
| [docs/python_adbc.md](/mnt/e/DDD/GitHub/gizmosql/docs/python_adbc.md) | Python ADBC integration guide. | Update Python client or ADBC instructions. |
| [docs/adbc_scanner_duckdb.md](/mnt/e/DDD/GitHub/gizmosql/docs/adbc_scanner_duckdb.md) | ADBC scanner with DuckDB documentation. | Update ADBC scanning usage details. |
| [docs/one_trillion_row_challenge.md](/mnt/e/DDD/GitHub/gizmosql/docs/one_trillion_row_challenge.md) | Large-scale benchmark narrative. | Update benchmark showcase or performance story content. |
| [docs/VIEW_DOCS_LOCALLY.md](/mnt/e/DDD/GitHub/gizmosql/docs/VIEW_DOCS_LOCALLY.md) | Local docs-site development instructions. | Run or preview the docs site locally. |
| [docs/_sidebar.md](/mnt/e/DDD/GitHub/gizmosql/docs/_sidebar.md) | Docs site navigation sidebar. | Change docs navigation structure. |
| [docs/_navbar.md](/mnt/e/DDD/GitHub/gizmosql/docs/_navbar.md) | Docs site top navigation. | Change docs site top-level links. |

---

## 🚀 Quick Start

> **Default credentials:** The server's default username is `gizmosql_user` (override with `--username` or `GIZMOSQL_USERNAME`). A password is always required via `--password` or `GIZMOSQL_PASSWORD`.

### Option 1: Run from Docker

```bash
# Username defaults to "gizmosql_user" when GIZMOSQL_USERNAME is not set
docker run --name gizmosql \
           --detach \
           --rm \
           --tty \
           --init \
           --publish 31337:31337 \
           --env TLS_ENABLED="1" \
           --env GIZMOSQL_PASSWORD="gizmosql_password" \
           --env PRINT_QUERIES="1" \
           --pull always \
           gizmodata/gizmosql:latest
```

### Option 2: Mount Your Own DuckDB database file

```bash
duckdb ./tpch_sf1.duckdb << EOF
INSTALL tpch; LOAD tpch; CALL dbgen(sf=1);
EOF

docker run --name gizmosql \
           --detach \
           --rm \
           --tty \
           --init \
           --publish 31337:31337 \
           --env TLS_ENABLED="1" \
           --env GIZMOSQL_PASSWORD="gizmosql_password" \
           --pull always \
           --mount type=bind,source=$(pwd),target=/opt/gizmosql/data \
           --env DATABASE_FILENAME="data/tpch_sf1.duckdb" \
           gizmodata/gizmosql:latest
```

### Option 3: Install via Homebrew (macOS & Linux)

```bash
brew tap gizmodata/tap
brew install gizmosql
```

Supported platforms:
- macOS (Apple Silicon / ARM64)
- Linux (x86-64 / AMD64)
- Linux (ARM64)

Then run the server (username defaults to `gizmosql_user`):

```bash
GIZMOSQL_PASSWORD="gizmosql_password" gizmosql_server --database-filename your.duckdb --print-queries
```

### Option 4: Windows Installer (Recommended)

Download [`GizmoSQL-Setup-x64.exe`](https://github.com/gizmodata/gizmosql/releases) from GitHub Releases. This is the recommended Windows artifact and installs:

- GizmoSQL Core
- the bundled demo DuckDB database by default
- GizmoSQL UI by default
- the Power BI connector as an optional recommended checkbox

The default evaluator journey is 8 steps:

1. Download `GizmoSQL-Setup-x64.exe`
2. Run setup
3. Accept the default options
4. Install
5. Finish
6. Open GizmoSQL UI
7. Open the demo connection
8. Run your first query

The default `Open GizmoSQL UI` completion action attempts to start the bundled local demo server automatically so the UI remains the first-run surface.

Advanced/manual Windows artifacts remain available separately:

- `GizmoSQL-Core-x64.msi`
- `GizmoSQL-UI-x64.msi`
- `GizmoSQL-PowerBI-Setup-x64.msi`

Windows install details and troubleshooting:

- [Windows install guide](docs/windows-install.md)
- [Windows quickstart](docs/quickstart-windows.md)

---

## 🧰 Clients and Tools

### 🔗 JDBC

Use with DBeaver or other JDBC clients:

```text
jdbc:gizmosql://localhost:31337?useEncryption=true&user=gizmosql_user&password=gizmosql_password&disableCertificateVerification=true
```

More info: [Setup guide](https://github.com/gizmodata/setup-gizmosql-jdbc-driver-in-dbeaver)

---

### 🐍 Python (ADBC)

**Prerequisite:** Python 3.10+ and the [GizmoSQL ADBC driver](https://pypi.org/project/adbc-driver-gizmosql/):

```bash
pip install adbc-driver-gizmosql
```

The driver also supports OAuth/SSO authentication for GizmoSQL Enterprise users.

```python
from adbc_driver_gizmosql import dbapi as gizmosql

with gizmosql.connect(
    "grpc+tls://localhost:31337",
    username="gizmosql_user",
    password="gizmosql_password",
    tls_skip_verify=True,  # Not needed if you use a trusted CA-signed TLS cert
) as conn:
    with conn.cursor() as cur:
        cur.execute(
            "SELECT n_nationkey, n_name FROM nation WHERE n_nationkey = ?",
            parameters=[24],
        )
        x = cur.fetch_arrow_table()
```

---

### 🔑 Token authentication
See: https://github.com/gizmodata/generate-gizmosql-token for an example of how to generate a token and use it with GizmoSQL.

### 💻 CLI Client

GizmoSQL ships with an interactive SQL shell inspired by `psql` and the DuckDB CLI:

```bash
# Interactive session
GIZMOSQL_PASSWORD="gizmosql_password" gizmosql_client --host localhost --username gizmosql_user --tls --tls-skip-verify
```

Run a single query with `--command`:

```bash
GIZMOSQL_PASSWORD="gizmosql_password" gizmosql_client \
  --host localhost --username gizmosql_user --tls --tls-skip-verify \
  --command "SELECT version()"
```

Pipe SQL from a heredoc:

```bash
GIZMOSQL_PASSWORD="gizmosql_password" gizmosql_client \
  --host localhost --username gizmosql_user --tls --tls-skip-verify --quiet <<'EOF'
SELECT n_nationkey, n_name
FROM nation
WHERE n_nationkey = 24;
EOF
```

More info: [Client Shell documentation](https://docs.gizmosql.com/#/client)

---

## 🏗️ Build from Source (Optional)

```bash
git clone https://github.com/gizmodata/gizmosql --recurse-submodules
cd gizmosql
cmake -S . -B build -G Ninja -DCMAKE_INSTALL_PREFIX=/usr/local
cmake --build build --target install
```

Then run:

```bash
GIZMOSQL_PASSWORD="..." gizmosql_server --database-filename ./data/your.db --print-queries
```

---

## 🧪 Advanced Features

- ✅ DuckDB + SQLite backend support
- ✅ TLS & optional mTLS
- ✅ JWT-based auth (automatically issued, signed server-side)
- ✅ Server initialization via `INIT_SQL_COMMANDS` or `INIT_SQL_COMMANDS_FILE`
- ✅ Slim Docker image for minimal runtime

---

## 🛠 Backend Selection

```bash
# DuckDB (default)
gizmosql_server -B duckdb --database-filename data/foo.duckdb

# SQLite
gizmosql_server -B sqlite --database-filename data/foo.sqlite
```

> [!TIP]
> You can now use the: `--query-timeout` argument to set a maximum query timeout in seconds for the server.  Queries running longer than the timeout will be killed.  The default value of: `0` means "unlimited".
> Example: `gizmosql_server (other args...) --query-timeout 10`
> will set a timeout of 10 seconds for all queries.

> [!TIP]
> The health check query can be customized using `--health-check-query` or the `GIZMOSQL_HEALTH_CHECK_QUERY` environment variable.
> The default is `SELECT 1`. This is useful when you need a more specific health check for your deployment.
> Example: `gizmosql_server (other args...) --health-check-query "SELECT 1 FROM my_table LIMIT 1"`

---


## 🧩 Extensions & Integrations

- 💻 [GizmoSQL UI](https://github.com/gizmodata/gizmosql-ui) 🚀 **NEW!**
- 🔌 [SQLAlchemy dialect](https://github.com/gizmodata/sqlalchemy-gizmosql-adbc-dialect)
- 💿 [Apache Superset compatible SQLAlchemy driver](https://github.com/gizmodata/superset-sqlalchemy-gizmosql-adbc-dialect)
- 🔌 [Ibis adapter](https://github.com/gizmodata/ibis-gizmosql)
- 🌐 [Flight SQL over WebSocket Proxy](https://github.com/gizmodata/flight-sql-websocket-proxy)
- 📈 [Metabase driver](https://github.com/J0hnG4lt/metabase-flightsql-driver)
- ⚙️ [dbt Adapter](https://github.com/gizmodata/dbt-gizmosql)
- 🥅 [SQLMesh Adapter](https://github.com/gizmodata/sqlmesh-gizmosql) 🚀 **NEW!**
- ✨ [PySpark SQLFrame adapter](https://github.com/gizmodata/sqlframe-gizmosql) 🚀 **NEW!**
- 🪩 [ADBC Scanner by Query.Farm](docs/adbc_scanner_duckdb.md) 🚀 **NEW!**
- ⚓️ [Kubernetes Operator](https://github.com/gizmodata/gizmosql-operator) 🚀 **NEW!**
- 📺 [GizmoSQLLine JDBC CLI Client](https://github.com/gizmodata/gizmosqlline) **NEW!**
- 🔥 [Grafana Plugin](https://github.com/gizmodata/grafana-gizmosql-datasource) **NEW!**
- 🕸️ [JavaScript/TypeScript Client](https://github.com/gizmodata/gizmosql-client-js) **NEW!**
- ☕️ [JDBC Driver](https://downloads.gizmodata.com/gizmosql-jdbc-driver/latest/gizmosql-jdbc-driver.jar) **NEW!**
- 🐍 [Python ADBC Driver (with OAuth/SSO)](https://github.com/gizmodata/adbc-driver-gizmosql) **NEW!**
- 🔌 [ODBC Driver](https://github.com/gizmodata/gizmosql-odbc-driver) **NEW!**
- 📊 [Power BI Connector](https://github.com/gizmodata/gizmosql-powerbi-connector) **NEW!**

For Windows users, `GizmoSQL-Setup-x64.exe` is the recommended entry point. The standalone UI and Power BI connector MSIs remain available as advanced/manual options.
---

## 📊 Performance

💡 On Azure VM `Standard_E64pds_v6` (~$3.74/hr):

- TPC-H SF 1000 benchmark:  
  ⏱️ 161.4 seconds  
  💰 ~$0.17 USD total

> 🏁 Speed for the win. Performance for pennies.

---

## 🔒 License

**GizmoSQL Core** is licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).

**Enterprise features** (in `src/enterprise/`) are proprietary and require a commercial license from GizmoData LLC. See [src/enterprise/LICENSE](src/enterprise/LICENSE) for details.

---

## 📫 Contact

Questions or consulting needs?

📧 info@gizmodata.com  
🌐 [https://gizmodata.com](https://gizmodata.com)

---

> Built with ❤️ by [GizmoData™](https://gizmodata.com)
