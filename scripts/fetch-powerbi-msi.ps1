[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Url,

    [Parameter(Mandatory = $true)]
    [string]$DestinationPath
)

$ErrorActionPreference = "Stop"

# Phase 1 strategy matches the UI MSI flow: CI fetches the published connector
# MSI from a pinned URL and keeps the standalone MSI available alongside the
# recommended bundle EXE.
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $DestinationPath) | Out-Null
Invoke-WebRequest -Uri $Url -OutFile $DestinationPath

if (-not (Test-Path -LiteralPath $DestinationPath)) {
    throw "Power BI MSI download did not produce '$DestinationPath'."
}

Write-Host "Fetched GizmoSQL Power BI MSI to $DestinationPath"
