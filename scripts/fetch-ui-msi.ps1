[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Url,

    [Parameter(Mandatory = $true)]
    [string]$DestinationPath
)

$ErrorActionPreference = "Stop"

# Phase 1 strategy: fetch separately released MSI assets in CI. This keeps the
# standalone MSIs available for advanced/manual users while letting Burn stitch
# them into a single recommended Windows entry point.
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $DestinationPath) | Out-Null
Invoke-WebRequest -Uri $Url -OutFile $DestinationPath

if (-not (Test-Path -LiteralPath $DestinationPath)) {
    throw "UI MSI download did not produce '$DestinationPath'."
}

Write-Host "Fetched GizmoSQL UI MSI to $DestinationPath"
