[CmdletBinding(DefaultParameterSetName = "Bundle")]
param(
    [Parameter(Mandatory = $true, ParameterSetName = "Bundle")]
    [string]$BundlePath,

    [Parameter(Mandatory = $true, ParameterSetName = "ArtifactZip")]
    [string]$ArtifactZip,

    [switch]$InstallPowerBI,
    [switch]$CleanupAfter,
    [switch]$SkipUiLaunch,
    [switch]$SkipQuickstartLaunch,

    [int]$DemoPort = 31337,
    [int]$InstallTimeoutSeconds = 900,
    [int]$DemoStartupTimeoutSeconds = 25,
    [string]$InstallRoot = "C:\Program Files\GizmoSQL",
    [string]$SampleDbPath = "C:\ProgramData\GizmoSQL\samples\gizmosql-demo.duckdb",
    [string]$UiInstallRoot = "C:\Program Files\GizmoSQL UI",
    [string]$ReportPath = "",
    [string]$Query = "SELECT customer_name, order_total FROM demo_recent_orders ORDER BY order_total DESC LIMIT 3;"
)

<#
.SYNOPSIS
Installs a GizmoSQL Windows bundle and validates the first-run onboarding flow.

.DESCRIPTION
Use this on a real Windows 10/11 machine or VM. The script runs the bundle with
default options, verifies the installed files and Start Menu shortcuts, starts
the installed demo launcher, runs a query through gizmosql_client, and exercises
the quickstart and UI handoff entry points.

For a real onboarding validation, use a bundle built from the main packaging
flow or a local bundle that chains the real UI and Power BI MSIs. The branch
smoke-test artifact produced by test-msi.yml uses placeholder chained MSIs and
is only suitable for packaging validation.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\test-windows-installer-flow.ps1 `
  -BundlePath C:\Downloads\GizmoSQL-Setup-x64.exe

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\test-windows-installer-flow.ps1 `
  -ArtifactZip C:\Downloads\GizmoSQL-windows-installers.zip -InstallPowerBI

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\test-windows-installer-flow.ps1 `
  -BundlePath C:\Downloads\GizmoSQL-Setup-x64.exe -CleanupAfter
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:SelectedParameterSet = $PSCmdlet.ParameterSetName
$script:Results = New-Object System.Collections.Generic.List[object]
$script:HadFailure = $false
$script:HadWarning = $false
$script:WorkDir = $null
$script:ResolvedBundlePath = $null
$script:ResolvedArtifactZip = $null
$script:InstallLogPath = $null
$script:QuickstartLogPath = $null
$script:UiLaunchLogPath = $null
$script:DemoServerLogPath = $null
$script:ClientLogPath = $null
$script:UninstallLogPath = $null
$script:ResolvedReportPath = $null
$script:LaunchedServerProcess = $null
$script:LaunchedServerPids = @()
$script:ExistingInstallDetected = $false

function Add-Result {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Step,

        [Parameter(Mandatory = $true)]
        [ValidateSet("PASS", "FAIL", "WARN", "INFO")]
        [string]$Status,

        [Parameter(Mandatory = $true)]
        [string]$Details
    )

    if ($Status -eq "FAIL") {
        $script:HadFailure = $true
    } elseif ($Status -eq "WARN") {
        $script:HadWarning = $true
    }

    $script:Results.Add([pscustomobject]@{
            Step    = $Step
            Status  = $Status
            Details = $Details
        })
}

function Normalize-Path {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.Path]::GetFullPath($Path)
}

function New-WorkDirectory {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $directory = Join-Path $env:TEMP "gizmosql-installer-test-$timestamp"
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    return $directory
}

function Assert-WindowsAdministrator {
    if ([System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
        throw "This validation script only runs on Windows."
    }

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell session."
    }
}

function Resolve-BundleSource {
    if ($script:SelectedParameterSet -eq "ArtifactZip") {
        $script:ResolvedArtifactZip = Normalize-Path -Path $ArtifactZip
        if (-not (Test-Path -LiteralPath $script:ResolvedArtifactZip)) {
            throw "Artifact zip was not found at '$script:ResolvedArtifactZip'."
        }

        $extractDir = Join-Path $script:WorkDir "artifact"
        Expand-Archive -LiteralPath $script:ResolvedArtifactZip -DestinationPath $extractDir -Force

        $bundle = Get-ChildItem -Path $extractDir -Filter "GizmoSQL-Setup-x64*.exe" -Recurse |
            Sort-Object FullName |
            Select-Object -First 1
        if (-not $bundle) {
            throw "No GizmoSQL bundle EXE was found inside '$script:ResolvedArtifactZip'."
        }

        $script:ResolvedBundlePath = $bundle.FullName
        Add-Result -Step "Bundle source" -Status "INFO" -Details "Using extracted bundle '$($script:ResolvedBundlePath)'."
        return
    }

    $script:ResolvedBundlePath = Normalize-Path -Path $BundlePath
    if (-not (Test-Path -LiteralPath $script:ResolvedBundlePath)) {
        throw "Bundle EXE was not found at '$script:ResolvedBundlePath'."
    }

    Add-Result -Step "Bundle source" -Status "INFO" -Details "Using bundle '$($script:ResolvedBundlePath)'."
}

function Invoke-Installer {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Install", "Uninstall")]
        [string]$Mode
    )

    if ($Mode -eq "Install") {
        $logPath = $script:InstallLogPath
        $modeSwitch = "/install"
    } else {
        $logPath = $script:UninstallLogPath
        $modeSwitch = "/uninstall"
    }

    $arguments = @(
        $modeSwitch,
        "/quiet",
        "/norestart",
        "/log",
        $logPath
    )

    if ($Mode -eq "Install") {
        if ($InstallPowerBI) {
            $arguments += "InstallPowerBI=1"
        }
    }

    $process = Start-Process -FilePath $script:ResolvedBundlePath -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit($InstallTimeoutSeconds * 1000)) {
        try {
            $process.Kill()
        } catch {
        }
        throw "$Mode timed out after $InstallTimeoutSeconds seconds."
    }

    if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
        throw "$Mode failed with exit code $($process.ExitCode). See '$logPath'."
    }

    if ($process.ExitCode -eq 3010) {
        Add-Result -Step $Mode -Status "WARN" -Details "$Mode completed with exit code 3010. A reboot may be required. Log: $logPath"
        return
    }

    Add-Result -Step $Mode -Status "PASS" -Details "$Mode completed successfully. Log: $logPath"
}

function Get-UninstallEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    $roots = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    )

    $entries = foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        Get-ChildItem -LiteralPath $root | ForEach-Object {
            $item = Get-ItemProperty -LiteralPath $_.PSPath
            if ([string]::IsNullOrWhiteSpace($item.DisplayName)) {
                return
            }

            foreach ($pattern in $Patterns) {
                if ($item.DisplayName -like $pattern) {
                    [pscustomobject]@{
                        DisplayName = $item.DisplayName
                        DisplayVersion = $item.DisplayVersion
                        Publisher = $item.Publisher
                    }
                    break
                }
            }
        }
    }

    return @($entries)
}

function Test-FilePresent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Step
    )

    if (Test-Path -LiteralPath $Path) {
        Add-Result -Step $Step -Status "PASS" -Details "Found '$Path'."
        return $true
    }

    Add-Result -Step $Step -Status "FAIL" -Details "Missing '$Path'."
    return $false
}

function Test-PathContainsInstallRoot {
    $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $segments = @()
    if (-not [string]::IsNullOrWhiteSpace($machinePath)) {
        $segments = $machinePath.Split(";") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    }

    $found = $false
    foreach ($segment in $segments) {
        if ($segment.TrimEnd("\") -ieq $InstallRoot.TrimEnd("\")) {
            $found = $true
            break
        }
    }

    if ($found) {
        Add-Result -Step "PATH entry" -Status "PASS" -Details "Machine PATH contains '$InstallRoot'."
        return
    }

    Add-Result -Step "PATH entry" -Status "FAIL" -Details "Machine PATH does not contain '$InstallRoot'."
}

function Get-ShortcutMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ShortcutPath
    )

    $shell = New-Object -ComObject WScript.Shell
    try {
        return $shell.CreateShortcut($ShortcutPath)
    } finally {
        [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)
    }
}

function Test-Shortcut {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ShortcutPath,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedTarget,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedArguments,

        [Parameter(Mandatory = $true)]
        [string]$Step
    )

    if (-not (Test-Path -LiteralPath $ShortcutPath)) {
        Add-Result -Step $Step -Status "FAIL" -Details "Missing shortcut '$ShortcutPath'."
        return
    }

    $shortcut = Get-ShortcutMetadata -ShortcutPath $ShortcutPath
    $targetMatches = $shortcut.TargetPath.TrimEnd("\") -ieq $ExpectedTarget.TrimEnd("\")
    $argumentsMatch = $shortcut.Arguments -eq $ExpectedArguments

    if ($targetMatches -and $argumentsMatch) {
        Add-Result -Step $Step -Status "PASS" -Details "Shortcut points to '$ExpectedTarget $ExpectedArguments'."
        return
    }

    Add-Result -Step $Step -Status "FAIL" -Details "Shortcut expected '$ExpectedTarget $ExpectedArguments' but found '$($shortcut.TargetPath) $($shortcut.Arguments)'."
}

function Resolve-UiExecutable {
    $candidates = @(
        (Join-Path $UiInstallRoot "GizmoSQL UI.exe"),
        (Join-Path $UiInstallRoot "gizmosql-ui.exe"),
        (Join-Path $InstallRoot "GizmoSQL UI.exe"),
        (Join-Path $InstallRoot "gizmosql-ui.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Test-PortAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
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

function Wait-ForPortListening {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Port,

        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $client = $null
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $async = $client.BeginConnect("127.0.0.1", $Port, $null, $null)
            if ($async.AsyncWaitHandle.WaitOne(750, $false) -and $client.Connected) {
                $client.EndConnect($async) | Out-Null
                return $true
            }
        } catch {
        } finally {
            if ($client -ne $null) {
                $client.Dispose()
            }
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Start-DemoServer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LauncherPath
    )

    if (-not (Test-PortAvailable -Port $DemoPort)) {
        Add-Result -Step "Demo port" -Status "FAIL" -Details "Port $DemoPort is already in use. Stop the process using that port and re-run the test."
        return $false
    }

    $existingPids = @(Get-Process -Name "gizmosql_server" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
    $powershellExe = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\powershell.exe"

    $script:LaunchedServerProcess = Start-Process `
        -FilePath $powershellExe `
        -ArgumentList @(
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            $LauncherPath,
            "-Port",
            $DemoPort
        ) `
        -RedirectStandardOutput $script:DemoServerLogPath `
        -RedirectStandardError $script:DemoServerLogPath `
        -PassThru

    if (-not (Wait-ForPortListening -Port $DemoPort -TimeoutSeconds $DemoStartupTimeoutSeconds)) {
        Add-Result -Step "Launch demo server" -Status "FAIL" -Details "The demo launcher did not open port $DemoPort within $DemoStartupTimeoutSeconds seconds. Log: $($script:DemoServerLogPath)"
        return $false
    }

    $script:LaunchedServerPids = @(
        Get-Process -Name "gizmosql_server" -ErrorAction SilentlyContinue |
        Where-Object { $existingPids -notcontains $_.Id } |
        Select-Object -ExpandProperty Id
    )

    $logContent = ""
    if (Test-Path -LiteralPath $script:DemoServerLogPath) {
        Start-Sleep -Milliseconds 500
        $logContent = Get-Content -LiteralPath $script:DemoServerLogPath -Raw
    }

    if ($logContent -match "First query:" -and $logContent -match "Database:") {
        Add-Result -Step "Launch demo server" -Status "PASS" -Details "Demo launcher started and printed connection hints. Log: $($script:DemoServerLogPath)"
        return $true
    }

    Add-Result -Step "Launch demo server" -Status "WARN" -Details "Demo server started, but the launcher output did not include the expected onboarding hints. Log: $($script:DemoServerLogPath)"
    return $true
}

function Invoke-ClientQuery {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ClientExePath
    )

    $previousPassword = $env:GIZMOSQL_PASSWORD
    try {
        $env:GIZMOSQL_PASSWORD = "gizmosql_password"
        $output = & $ClientExePath `
            --host 127.0.0.1 `
            --port $DemoPort `
            --username gizmosql_user `
            --command $Query 2>&1
        $exitCode = $LASTEXITCODE
    } finally {
        if ($null -eq $previousPassword) {
            Remove-Item Env:GIZMOSQL_PASSWORD -ErrorAction SilentlyContinue
        } else {
            $env:GIZMOSQL_PASSWORD = $previousPassword
        }
    }

    $outputText = ($output | Out-String).Trim()
    $outputText | Out-File -LiteralPath $script:ClientLogPath -Encoding ascii

    if ($exitCode -ne 0) {
        Add-Result -Step "Client query" -Status "FAIL" -Details "gizmosql_client exited with code $exitCode. Log: $($script:ClientLogPath)"
        return $false
    }

    if ($outputText -match "customer_name" -or $outputText -match "order_total") {
        Add-Result -Step "Client query" -Status "PASS" -Details "Query succeeded against the bundled demo DB. Log: $($script:ClientLogPath)"
        return $true
    }

    Add-Result -Step "Client query" -Status "WARN" -Details "Query succeeded but the output did not contain the expected column names. Log: $($script:ClientLogPath)"
    return $true
}

function Invoke-OnboardingEntryPoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ShellCmdPath,

        [Parameter(Mandatory = $true)]
        [string]$Action,

        [Parameter(Mandatory = $true)]
        [string]$LogPath,

        [Parameter(Mandatory = $true)]
        [string]$Step
    )

    $output = & $ShellCmdPath $Action 2>&1
    $exitCode = $LASTEXITCODE
    ($output | Out-String).Trim() | Out-File -LiteralPath $LogPath -Encoding ascii

    if ($exitCode -ne 0) {
        Add-Result -Step $Step -Status "FAIL" -Details "Action '$Action' failed with exit code $exitCode. Log: $LogPath"
        return $false
    }

    Add-Result -Step $Step -Status "PASS" -Details "Action '$Action' returned successfully. Log: $LogPath"
    return $true
}

function Stop-LaunchedProcesses {
    foreach ($pid in $script:LaunchedServerPids) {
        try {
            Stop-Process -Id $pid -Force -ErrorAction Stop
        } catch {
        }
    }

    if ($script:LaunchedServerProcess -ne $null) {
        try {
            if (-not $script:LaunchedServerProcess.HasExited) {
                Stop-Process -Id $script:LaunchedServerProcess.Id -Force -ErrorAction Stop
            }
        } catch {
        }
    }
}

function Write-Report {
    if ([string]::IsNullOrWhiteSpace($ReportPath)) {
        $script:ResolvedReportPath = Join-Path $script:WorkDir "test-windows-installer-flow-report.json"
    } else {
        $script:ResolvedReportPath = Normalize-Path -Path $ReportPath
        $reportDirectory = Split-Path -Parent $script:ResolvedReportPath
        if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
            New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
        }
    }

    $report = [ordered]@{
        generatedAtUtc     = (Get-Date).ToUniversalTime().ToString("o")
        machineName        = $env:COMPUTERNAME
        bundlePath         = $script:ResolvedBundlePath
        artifactZip        = $script:ResolvedArtifactZip
        installPowerBI     = [bool]$InstallPowerBI
        cleanupAfter       = [bool]$CleanupAfter
        existingInstall    = [bool]$script:ExistingInstallDetected
        demoPort           = $DemoPort
        installRoot        = $InstallRoot
        sampleDbPath       = $SampleDbPath
        logs               = [ordered]@{
            install    = $script:InstallLogPath
            demoServer = $script:DemoServerLogPath
            client     = $script:ClientLogPath
            quickstart = $script:QuickstartLogPath
            uiLaunch   = $script:UiLaunchLogPath
            uninstall  = $script:UninstallLogPath
        }
        overallStatus      = if ($script:HadFailure) { "FAIL" } elseif ($script:HadWarning) { "WARN" } else { "PASS" }
        results            = @($script:Results)
    }

    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $script:ResolvedReportPath -Encoding ascii
    Write-Host ""
    Write-Host "Validation report: $($script:ResolvedReportPath)"
    Write-Host ""
    $script:Results | Format-Table -AutoSize | Out-Host
}

try {
    Assert-WindowsAdministrator

    $script:WorkDir = New-WorkDirectory
    $script:InstallLogPath = Join-Path $script:WorkDir "bundle-install.log"
    $script:QuickstartLogPath = Join-Path $script:WorkDir "quickstart-launch.log"
    $script:UiLaunchLogPath = Join-Path $script:WorkDir "ui-launch.log"
    $script:DemoServerLogPath = Join-Path $script:WorkDir "demo-server.log"
    $script:ClientLogPath = Join-Path $script:WorkDir "client-query.log"
    $script:UninstallLogPath = Join-Path $script:WorkDir "bundle-uninstall.log"

    Resolve-BundleSource

    $preExistingEntries = Get-UninstallEntries -Patterns @("GizmoSQL*", "GizmoSQL Setup*", "GizmoSQL UI*", "*GizmoSQL*Power BI*")
    if ($preExistingEntries.Count -gt 0) {
        $script:ExistingInstallDetected = $true
        $details = ($preExistingEntries | ForEach-Object { $_.DisplayName }) -join ", "
        Add-Result -Step "Existing install check" -Status "WARN" -Details "Detected existing GizmoSQL-related products: $details. This run is validating upgrade/reinstall behavior instead of a clean machine."
        if ($CleanupAfter) {
            throw "CleanupAfter is unsafe when GizmoSQL is already installed. Use a clean Windows VM or rerun without -CleanupAfter."
        }
    } else {
        Add-Result -Step "Existing install check" -Status "PASS" -Details "No existing GizmoSQL-related products were detected."
    }

    Invoke-Installer -Mode "Install"

    $coreEntries = Get-UninstallEntries -Patterns @("GizmoSQL*")
    if ($coreEntries.Count -gt 0) {
        Add-Result -Step "Core ARP entry" -Status "PASS" -Details "Found uninstall entry: $($coreEntries[0].DisplayName)"
    } else {
        Add-Result -Step "Core ARP entry" -Status "FAIL" -Details "Did not find a GizmoSQL Core uninstall entry after install."
    }

    $shellCmdPath = Join-Path $InstallRoot "gizmosql-shell.cmd"
    $launcherPath = Join-Path $InstallRoot "launch-demo-server.ps1"
    $clientExePath = Join-Path $InstallRoot "gizmosql_client.exe"
    $serverExePath = Join-Path $InstallRoot "gizmosql_server.exe"
    $quickstartPath = Join-Path $InstallRoot "quickstart.html"

    $installChecks = @(
        (Test-FilePresent -Path $serverExePath -Step "Core server binary"),
        (Test-FilePresent -Path $clientExePath -Step "Core client binary"),
        (Test-FilePresent -Path $launcherPath -Step "Demo launcher asset"),
        (Test-FilePresent -Path $shellCmdPath -Step "Shell handoff asset"),
        (Test-FilePresent -Path $quickstartPath -Step "Quickstart asset"),
        (Test-FilePresent -Path $SampleDbPath -Step "Sample database")
    )

    Test-PathContainsInstallRoot

    $startMenuRoot = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\GizmoSQL"
    Test-Shortcut `
        -ShortcutPath (Join-Path $startMenuRoot "Launch Demo Server.lnk") `
        -ExpectedTarget $shellCmdPath `
        -ExpectedArguments "launch-demo-server" `
        -Step "Start Menu - Launch Demo Server"
    Test-Shortcut `
        -ShortcutPath (Join-Path $startMenuRoot "Open Quickstart.lnk") `
        -ExpectedTarget $shellCmdPath `
        -ExpectedArguments "open-quickstart" `
        -Step "Start Menu - Open Quickstart"
    Test-Shortcut `
        -ShortcutPath (Join-Path $startMenuRoot "Open GizmoSQL UI.lnk") `
        -ExpectedTarget $shellCmdPath `
        -ExpectedArguments "open-ui" `
        -Step "Start Menu - Open GizmoSQL UI"

    $uiExePath = Resolve-UiExecutable
    if ($uiExePath) {
        Add-Result -Step "UI install check" -Status "PASS" -Details "Found UI executable '$uiExePath'."
    } else {
        Add-Result -Step "UI install check" -Status "FAIL" -Details "Could not find GizmoSQL UI in the default install locations. Use a bundle with the real UI MSI for first-run testing."
    }

    if ($InstallPowerBI) {
        $powerBiEntries = Get-UninstallEntries -Patterns @("*GizmoSQL*Power BI*", "*Power BI*GizmoSQL*")
        if ($powerBiEntries.Count -gt 0) {
            Add-Result -Step "Power BI install check" -Status "PASS" -Details "Found Power BI connector uninstall entry: $($powerBiEntries[0].DisplayName)"
        } else {
            Add-Result -Step "Power BI install check" -Status "WARN" -Details "No GizmoSQL Power BI uninstall entry was found. Validate this only with a bundle that chains the real connector MSI."
        }
    }

    $canRunOnboarding = $true
    foreach ($check in $installChecks) {
        if (-not $check) {
            $canRunOnboarding = $false
        }
    }

    if ($canRunOnboarding) {
        if (Start-DemoServer -LauncherPath $launcherPath) {
            [void](Invoke-ClientQuery -ClientExePath $clientExePath)
        }

        if (-not $SkipQuickstartLaunch) {
            [void](Invoke-OnboardingEntryPoint -ShellCmdPath $shellCmdPath -Action "open-quickstart" -LogPath $script:QuickstartLogPath -Step "Open Quickstart action")
        } else {
            Add-Result -Step "Open Quickstart action" -Status "INFO" -Details "Skipped by request."
        }

        if (-not $SkipUiLaunch) {
            [void](Invoke-OnboardingEntryPoint -ShellCmdPath $shellCmdPath -Action "open-ui" -LogPath $script:UiLaunchLogPath -Step "Open UI action")
        } else {
            Add-Result -Step "Open UI action" -Status "INFO" -Details "Skipped by request."
        }
    } else {
        Add-Result -Step "Onboarding flow" -Status "FAIL" -Details "Skipped onboarding commands because one or more required installed assets are missing."
    }

    if ($CleanupAfter) {
        Stop-LaunchedProcesses
        Invoke-Installer -Mode "Uninstall"

        if (-not (Test-Path -LiteralPath $serverExePath) -and -not (Test-Path -LiteralPath $clientExePath)) {
            Add-Result -Step "Post-uninstall binaries" -Status "PASS" -Details "Core binaries were removed from '$InstallRoot'."
        } else {
            Add-Result -Step "Post-uninstall binaries" -Status "FAIL" -Details "Core binaries still exist under '$InstallRoot' after uninstall."
        }

        if (-not (Test-Path -LiteralPath $SampleDbPath)) {
            Add-Result -Step "Post-uninstall sample DB" -Status "PASS" -Details "Sample DB was removed from '$SampleDbPath'."
        } else {
            Add-Result -Step "Post-uninstall sample DB" -Status "FAIL" -Details "Sample DB still exists at '$SampleDbPath' after uninstall."
        }

        if (-not (Test-Path -LiteralPath $startMenuRoot)) {
            Add-Result -Step "Post-uninstall Start Menu" -Status "PASS" -Details "Start Menu folder was removed."
        } else {
            Add-Result -Step "Post-uninstall Start Menu" -Status "FAIL" -Details "Start Menu folder '$startMenuRoot' still exists after uninstall."
        }
    }
} catch {
    Add-Result -Step "Unhandled error" -Status "FAIL" -Details $_.Exception.Message
} finally {
    Stop-LaunchedProcesses
    if ($script:WorkDir -ne $null) {
        Write-Report
    }
}

if ($script:HadFailure) {
    exit 1
}

exit 0
