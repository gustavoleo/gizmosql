[CmdletBinding()]
param(
    [string]$ServerExePath = "",
    [string]$HostName = "127.0.0.1",
    [int]$Port = 31337,
    [string]$Username = "gizmosql_user",
    [string]$Password = "gizmosql_password",
    [string]$SampleDbPath = "C:\ProgramData\GizmoSQL\samples\gizmosql-demo.duckdb"
)

$ErrorActionPreference = "Stop"

function Test-PortAvailable {
    param([int]$PortNumber)

    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $PortNumber)
        $listener.Start()
        return $true
    } catch {
        return $false
    } finally {
        if ($listener -ne $null) {
            $listener.Stop()
        }
    }
}

function Resolve-ExistingPath {
    param(
        [string[]]$Candidates
    )

    foreach ($candidate in $Candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

$installRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $installRoot)

$serverCandidates = @(
    $ServerExePath,
    (Join-Path $installRoot "gizmosql_server.exe"),
    (Join-Path $repoRoot "build\gizmosql_server.exe"),
    (Join-Path $repoRoot "build\Release\gizmosql_server.exe"),
    (Join-Path $repoRoot "build\RelWithDebInfo\gizmosql_server.exe"),
    (Join-Path $repoRoot "build\Debug\gizmosql_server.exe")
)
$resolvedServerExe = Resolve-ExistingPath -Candidates $serverCandidates

$sampleDbCandidates = @(
    $SampleDbPath,
    (Join-Path $repoRoot "build\windows\sample-data\gizmosql-demo.duckdb")
)
$resolvedSampleDbPath = Resolve-ExistingPath -Candidates $sampleDbCandidates

if (-not $resolvedServerExe) {
    $candidateList = ($serverCandidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join "', '"
    Write-Error "GizmoSQL server executable was not found. Tried: '$candidateList'. Repair/reinstall GizmoSQL Core or pass -ServerExePath."
}

if (-not $resolvedSampleDbPath) {
    $candidateList = ($sampleDbCandidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join "', '"
    Write-Error "Sample database was not found. Tried: '$candidateList'. Re-run setup with Sample Database enabled or pass -SampleDbPath."
}

if (-not (Test-PortAvailable -PortNumber $Port)) {
    Write-Error "Port $Port is already in use. Stop the process using that port or relaunch with a different port."
}

Write-Host ""
Write-Host "GizmoSQL demo server" -ForegroundColor Cyan
Write-Host "  Host:       $HostName"
Write-Host "  Port:       $Port"
Write-Host "  Username:   $Username"
Write-Host "  Password:   $Password"
Write-Host "  Server:     $resolvedServerExe"
Write-Host "  Database:   $resolvedSampleDbPath"
Write-Host "  First query: SELECT * FROM demo_recent_orders LIMIT 10;"
Write-Host ""

& $resolvedServerExe `
    --hostname $HostName `
    --port $Port `
    --database-filename $resolvedSampleDbPath `
    --username $Username `
    --password $Password `
    --print-queries

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Error "gizmosql_server exited with code $exitCode."
}
