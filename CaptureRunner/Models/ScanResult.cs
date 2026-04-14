using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class ScreenScanResult
{
    [JsonPropertyName("row_id")]
    public string RowId { get; set; } = string.Empty;

    [JsonPropertyName("screen_name")]
    public string ScreenName { get; set; } = string.Empty;

    [JsonPropertyName("module")]
    public string? Module { get; set; }

    [JsonPropertyName("open_result")]
    public OpenResult OpenResult { get; set; } = new();

    [JsonPropertyName("uia_detection")]
    public UiaDetectionResult UiaDetection { get; set; } = new();

    [JsonPropertyName("validation")]
    public ValidationResult Validation { get; set; } = new();

    [JsonPropertyName("classification")]
    public ClassificationResult Classification { get; set; } = new();

    [JsonPropertyName("limitations")]
    public List<string> Limitations { get; set; } = new();

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("diagnostics")]
    public DiagnosticsResult Diagnostics { get; set; } = new();

    [JsonPropertyName("capture")]
    public CaptureResult Capture { get; set; } = new();

    [JsonPropertyName("placement")]
    public PlacementResult? Placement { get; set; }

    public static ScreenScanResult CreateFatalFailure(ScreenPlan screen, string limitation, string? exception)
    {
        return new ScreenScanResult
        {
            RowId = screen.RowId,
            ScreenName = screen.ScreenName,
            Module = screen.Module,
            OpenResult = new OpenResult
            {
                Success = false,
                MethodUsed = "none",
                DurationMs = 0
            },
            UiaDetection = new UiaDetectionResult
            {
                WindowFound = false
            },
            Validation = new ValidationResult(),
            Classification = new ClassificationResult
            {
                Automatable = "none",
                Confidence = "low"
            },
            Limitations = new List<string> { limitation },
            Recommendation = "Verify the executable path, application startup behavior, and desktop session before retrying the scan.",
            Diagnostics = new DiagnosticsResult
            {
                DurationMs = 0,
                Exception = exception,
                RetryCount = 0
            }
        };
    }

    public static ScreenScanResult CreateTimeoutFailure(ScreenPlan screen, TimeSpan timeout)
    {
        return new ScreenScanResult
        {
            RowId = screen.RowId,
            ScreenName = screen.ScreenName,
            Module = screen.Module,
            OpenResult = new OpenResult
            {
                Success = false,
                MethodUsed = "timed_out",
                DurationMs = (long)timeout.TotalMilliseconds
            },
            UiaDetection = new UiaDetectionResult
            {
                WindowFound = false
            },
            Validation = new ValidationResult(),
            Classification = new ClassificationResult
            {
                Automatable = "none",
                Confidence = "low"
            },
            Limitations = new List<string> { $"Screen scan timed out after {timeout.TotalMilliseconds:0} ms." },
            Recommendation = "Retry the screen individually, or increase the per-screen timeout after confirming the app is responsive.",
            Diagnostics = new DiagnosticsResult
            {
                DurationMs = (long)timeout.TotalMilliseconds,
                RetryCount = 0,
                Exception = "Screen scan timed out."
            }
        };
    }
}

public sealed class CaptureResult
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("attempted")]
    public bool Attempted { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "none";

    [JsonPropertyName("source_file")]
    public string? SourceFile { get; set; }

    [JsonPropertyName("output_file")]
    public string? OutputFile { get; set; }

    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }
}

public sealed class OpenResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("method_used")]
    public string MethodUsed { get; set; } = "none";

    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }
}

public sealed class UiaDetectionResult
{
    [JsonPropertyName("window_found")]
    public bool WindowFound { get; set; }

    [JsonPropertyName("window_title")]
    public string? WindowTitle { get; set; }

    [JsonPropertyName("total_descendants")]
    public int TotalDescendants { get; set; }

    [JsonPropertyName("elements_found")]
    public List<DetectedElement> ElementsFound { get; set; } = new();

    [JsonPropertyName("candidate_controls")]
    public List<DetectedElement> CandidateControls { get; set; } = new();

    [JsonPropertyName("missing_elements")]
    public List<string> MissingElements { get; set; } = new();

    [JsonPropertyName("stability")]
    public StabilityResult Stability { get; set; } = new();
}

public sealed class DetectedElement
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("control_type")]
    public string? ControlType { get; set; }

    [JsonPropertyName("automation_id")]
    public string? AutomationId { get; set; }
}

public sealed class StabilityResult
{
    [JsonPropertyName("consistent")]
    public bool Consistent { get; set; }

    [JsonPropertyName("first_scan_count")]
    public int FirstScanCount { get; set; }

    [JsonPropertyName("second_scan_count")]
    public int SecondScanCount { get; set; }

    [JsonPropertyName("difference_count")]
    public int DifferenceCount { get; set; }
}

public sealed class ValidationResult
{
    [JsonPropertyName("title_match")]
    public bool TitleMatch { get; set; }

    [JsonPropertyName("selected_navigation_match")]
    public bool SelectedNavigationMatch { get; set; }

    [JsonPropertyName("expected_control_match")]
    public bool ExpectedControlMatch { get; set; }

    [JsonPropertyName("control_match")]
    public bool ControlMatch { get; set; }

    [JsonPropertyName("target_element_found")]
    public bool TargetElementFound { get; set; }
}

public sealed class ClassificationResult
{
    [JsonPropertyName("automatable")]
    public string Automatable { get; set; } = "none";

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "low";
}

public sealed class DiagnosticsResult
{
    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }

    [JsonPropertyName("fallback_used")]
    public string? FallbackUsed { get; set; }

    [JsonPropertyName("retry_count")]
    public int RetryCount { get; set; }

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }

    [JsonPropertyName("preflight")]
    public PreflightReport? Preflight { get; set; }
}
