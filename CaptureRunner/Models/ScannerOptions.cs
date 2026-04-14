namespace CaptureRunner.Models;

public sealed class ScannerOptions
{
    public const string DefaultOperatorMessage = "Automation in progress. Keep AnalyticsCreator open, close any extra AnalyticsCreator windows, minimize other windows if possible, and do not touch the mouse or keyboard.";
    public const string DefaultSnagitExecutablePath = @"C:\Program Files\TechSmith\Snagit\SnagitCapture.exe";

    public string ExecutablePath { get; set; } = string.Empty;

    public string? PlanPath { get; set; }

    public string OutputPath { get; set; } = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Output", "report.json"));

    public string? DiscoveryOutputPath { get; set; }

    public string? EnvironmentProfilePath { get; set; }

    public EnvironmentProfile? EnvironmentProfile { get; set; }

    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(20);

    public TimeSpan NavigationTimeout { get; set; } = TimeSpan.FromSeconds(4);

    public TimeSpan ScreenTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan CaptureTimeout { get; set; } = TimeSpan.FromSeconds(20);

    public bool KeepApplicationOpen { get; set; }

    public bool ShowOperatorNotice { get; set; } = true;

    public string OperatorMessage { get; set; } = DefaultOperatorMessage;

    public string? SnagitHotkey { get; set; }

    public string? SnagitWatchDirectory { get; set; }

    public string? SnagitExecutablePath { get; set; } = DefaultSnagitExecutablePath;

    public string CaptureOutputDirectory { get; set; } = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Output", "captures"));

    public CaptureBackend CaptureBackend { get; set; } = CaptureBackend.Snagit;

    public NativeCaptureArea NativeCaptureArea { get; set; } = NativeCaptureArea.Window;

    public float? NativePngDpi { get; set; }

    public bool WorkerMode { get; set; }

    public bool ShowHelp { get; set; }

    public bool StrictPreflight { get; set; } = true;

    public bool SnagitCaptureEnabled =>
        !string.IsNullOrWhiteSpace(SnagitHotkey)
        && !string.IsNullOrWhiteSpace(SnagitWatchDirectory);

    public bool IsSnagitCaptureEnabled(ScreenPlan? plan)
    {
        if (CaptureBackend != CaptureBackend.Snagit)
        {
            return false;
        }

        if (SnagitCaptureEnabled)
        {
            return true;
        }

        return plan is not null
               && !string.IsNullOrWhiteSpace(plan.Capture.PresetHotkey)
               && !string.IsNullOrWhiteSpace(plan.Capture.WatchDirectory);
    }

    public bool IsCaptureEnabled(ScreenPlan? plan)
    {
        return CaptureBackend == CaptureBackend.Native || IsSnagitCaptureEnabled(plan);
    }
}
