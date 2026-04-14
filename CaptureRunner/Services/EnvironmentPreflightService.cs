using System.Diagnostics;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class EnvironmentPreflightService
{
    private readonly DisplayTopologyService _displayTopologyService;

    public EnvironmentPreflightService(DisplayTopologyService displayTopologyService)
    {
        _displayTopologyService = displayTopologyService;
    }

    public PreflightReport Run(ScannerOptions options)
    {
        var displays = _displayTopologyService.GetDisplays();
        var report = new PreflightReport
        {
            ProfileName = options.EnvironmentProfile?.Name,
            ActualDisplays = displays.ToList()
        };

        AddCheck(report, "interactive_windows_session", Environment.UserInteractive, "Interactive Windows session is required.");
        AddCheck(report, "target_executable_exists", File.Exists(options.ExecutablePath), $"Target executable must exist: {options.ExecutablePath}");

        if (options.EnvironmentProfile is not null)
        {
            var captureDisplay = _displayTopologyService.FindMatch(options.EnvironmentProfile.DisplayProfile.CaptureDisplay, displays);
            AddCheck(
                report,
                "capture_display_profile",
                captureDisplay is not null,
                BuildDisplayMismatchMessage("capture", displays));

            DisplayRuntimeInfo? operatorDisplay = null;
            if (options.EnvironmentProfile.DisplayProfile.OperatorDisplay is not null)
            {
                operatorDisplay = _displayTopologyService.FindMatch(options.EnvironmentProfile.DisplayProfile.OperatorDisplay, displays);
                AddCheck(
                    report,
                    "operator_display_profile",
                    operatorDisplay is not null,
                    BuildDisplayMismatchMessage("operator", displays));
            }

            if (captureDisplay is not null && operatorDisplay is not null)
            {
                AddCheck(
                    report,
                    "capture_operator_distinct",
                    !captureDisplay.DeviceName.Equals(operatorDisplay.DeviceName, StringComparison.OrdinalIgnoreCase),
                    $"Capture and operator displays resolved to the same device ({captureDisplay.DeviceName}). Actual displays: {DisplayTopologyService.DescribeDisplays(displays)}");
            }
        }

        if (options.EnvironmentProfile?.RequireSnagitWhenCaptureEnabled == false)
        {
            // Intentionally allow capture configuration checks to be skipped.
        }
        else if (options.SnagitCaptureEnabled)
        {
            AddCheck(report, "snagit_watch_dir", !string.IsNullOrWhiteSpace(options.SnagitWatchDirectory) && Directory.Exists(options.SnagitWatchDirectory), $"Snagit watch directory must exist: {options.SnagitWatchDirectory}");
            AddCheck(report, "snagit_exe", !string.IsNullOrWhiteSpace(options.SnagitExecutablePath) && File.Exists(options.SnagitExecutablePath), $"Snagit executable must exist: {options.SnagitExecutablePath}");
        }

        if (options.EnvironmentProfile?.RequireSingleTargetProcess ?? true)
        {
            var processName = Path.GetFileNameWithoutExtension(options.ExecutablePath);
            var processCount = Process.GetProcessesByName(processName).Count(process => !process.HasExited);
            AddCheck(report, "single_target_process", processCount <= 1, $"Expected zero or one {processName}.exe process before run, found {processCount}.");
        }

        report.Passed = report.Checks.All(check => check.Passed);
        return report;
    }

    private static string BuildDisplayMismatchMessage(string role, IReadOnlyList<DisplayRuntimeInfo> displays)
    {
        return $"The configured {role} display did not match the certified profile. Actual displays: {DisplayTopologyService.DescribeDisplays(displays)}";
    }

    private static void AddCheck(PreflightReport report, string name, bool passed, string message)
    {
        report.Checks.Add(new PreflightCheck
        {
            Name = name,
            Passed = passed,
            Message = passed ? "ok" : message
        });
    }
}
