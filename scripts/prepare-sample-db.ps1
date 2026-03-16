[CmdletBinding()]
param(
    [string]$SourcePath = "build/windows/sample-data/gizmosql-demo.duckdb",
    [string]$OutputDir = "installer/staging/sample-data"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "Sample DB was not found at '$SourcePath'. Commit or generate gizmosql-demo.duckdb before building installers."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$destination = Join-Path $OutputDir "gizmosql-demo.duckdb"
Copy-Item -LiteralPath $SourcePath -Destination $destination -Force

Write-Host "Prepared sample DB at $destination"
