using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class UiMap
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("generated_at_utc")]
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("repository_name")]
    public string RepositoryName { get; set; } = string.Empty;

    [JsonPropertyName("accepted_threshold")]
    public double AcceptedThreshold { get; set; }

    [JsonPropertyName("startup_state_output")]
    public string? StartupStateOutput { get; set; }

    [JsonPropertyName("screens")]
    public List<UiScreen> Screens { get; set; } = new();

    [JsonPropertyName("remaining_queued_routes")]
    public List<UiRoute> RemainingQueuedRoutes { get; set; } = new();

    [JsonPropertyName("coverage")]
    public UiCoverageSummary Coverage { get; set; } = new();
}

public sealed class UiScreen
{
    [JsonPropertyName("screen_id")]
    public string ScreenId { get; set; } = string.Empty;

    [JsonPropertyName("screen_name")]
    public string ScreenName { get; set; } = string.Empty;

    [JsonPropertyName("module")]
    public string? Module { get; set; }

    [JsonPropertyName("family")]
    public string Family { get; set; } = "discovered";

    [JsonPropertyName("window_title")]
    public string? WindowTitle { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("route")]
    public UiRoute Route { get; set; } = new();

    [JsonPropertyName("alternate_routes")]
    public List<UiRoute> AlternateRoutes { get; set; } = new();

    [JsonPropertyName("required_state")]
    public string RequiredState { get; set; } = "Northwind repository";

    [JsonPropertyName("validation_anchors")]
    public List<UiValidationAnchor> ValidationAnchors { get; set; } = new();

    [JsonPropertyName("available_interactions")]
    public List<UiInteraction> AvailableInteractions { get; set; } = new();

    [JsonPropertyName("modal_outputs")]
    public List<UiModalOutput> ModalOutputs { get; set; } = new();

    [JsonPropertyName("screenshot_strategy")]
    public UiScreenshotStrategy ScreenshotStrategy { get; set; } = new();

    [JsonPropertyName("readiness")]
    public UiReadiness Readiness { get; set; } = new();

    [JsonPropertyName("evidence")]
    public UiEvidence Evidence { get; set; } = new();

    [JsonPropertyName("last_verified_utc")]
    public DateTimeOffset? LastVerifiedUtc { get; set; }
}

public sealed class UiRoute
{
    [JsonPropertyName("route_type")]
    public string RouteType { get; set; } = "current";

    [JsonPropertyName("route_text")]
    public string RouteText { get; set; } = "Current shell";

    [JsonPropertyName("steps")]
    public List<UiRouteStep> Steps { get; set; } = new();
}

public sealed class UiRouteStep
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("automation_id")]
    public string? AutomationId { get; set; }

    [JsonPropertyName("control_type")]
    public string? ControlType { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "select";
}

public sealed class UiInteraction
{
    [JsonPropertyName("interaction_id")]
    public string InteractionId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("automation_id")]
    public string? AutomationId { get; set; }

    [JsonPropertyName("control_type")]
    public string ControlType { get; set; } = string.Empty;

    [JsonPropertyName("class_name")]
    public string? ClassName { get; set; }

    [JsonPropertyName("bounds")]
    public UiBounds? Bounds { get; set; }

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("is_selected")]
    public bool? IsSelected { get; set; }

    [JsonPropertyName("expand_collapse_state")]
    public string? ExpandCollapseState { get; set; }

    [JsonPropertyName("supported_patterns")]
    public List<string> SupportedPatterns { get; set; } = new();

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "unknown";

    [JsonPropertyName("risk")]
    public string Risk { get; set; } = "unknown";

    [JsonPropertyName("readiness")]
    public string Readiness { get; set; } = "review";

    [JsonPropertyName("route_candidate")]
    public bool RouteCandidate { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "uia";
}

public sealed class UiBounds
{
    [JsonPropertyName("left")]
    public double Left { get; set; }

    [JsonPropertyName("top")]
    public double Top { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }
}

public sealed class UiValidationAnchor
{
    [JsonPropertyName("anchor_type")]
    public string AnchorType { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}

public sealed class UiModalOutput
{
    [JsonPropertyName("trigger")]
    public UiRouteStep Trigger { get; set; } = new();

    [JsonPropertyName("window_title")]
    public string? WindowTitle { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "not_opened";
}

public sealed class UiScreenshotStrategy
{
    [JsonPropertyName("backend")]
    public string Backend { get; set; } = "native";

    [JsonPropertyName("area")]
    public string Area { get; set; } = "window";

    [JsonPropertyName("dpi")]
    public float Dpi { get; set; } = 600;

    [JsonPropertyName("bucket")]
    public string Bucket { get; set; } = "review";

    [JsonPropertyName("output_file")]
    public string? OutputFile { get; set; }
}

public sealed class UiReadiness
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "review";

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "low";

    [JsonPropertyName("blocker_type")]
    public string? BlockerType { get; set; }

    [JsonPropertyName("reasons")]
    public List<string> Reasons { get; set; } = new();
}

public sealed class UiEvidence
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "uia_runtime";

    [JsonPropertyName("route_json")]
    public string? RouteJson { get; set; }

    [JsonPropertyName("snapshot_json")]
    public string? SnapshotJson { get; set; }

    [JsonPropertyName("report_json")]
    public string? ReportJson { get; set; }

    [JsonPropertyName("screenshot_png")]
    public string? ScreenshotPng { get; set; }

    [JsonPropertyName("failure_note")]
    public string? FailureNote { get; set; }
}

public sealed class UiCoverageSummary
{
    [JsonPropertyName("discovered_count")]
    public int DiscoveredCount { get; set; }

    [JsonPropertyName("accepted_count")]
    public int AcceptedCount { get; set; }

    [JsonPropertyName("review_count")]
    public int ReviewCount { get; set; }

    [JsonPropertyName("rejected_count")]
    public int RejectedCount { get; set; }

    [JsonPropertyName("blocked_count")]
    public int BlockedCount { get; set; }

    [JsonPropertyName("accepted_ratio")]
    public double AcceptedRatio { get; set; }

    [JsonPropertyName("accepted_threshold")]
    public double AcceptedThreshold { get; set; }

    [JsonPropertyName("threshold_met")]
    public bool ThresholdMet { get; set; }

    [JsonPropertyName("notes")]
    public List<string> Notes { get; set; } = new();
}

public sealed class UiRejectedRoute
{
    [JsonPropertyName("screen_id")]
    public string ScreenId { get; set; } = string.Empty;

    [JsonPropertyName("screen_name")]
    public string ScreenName { get; set; } = string.Empty;

    [JsonPropertyName("route")]
    public UiRoute Route { get; set; } = new();

    [JsonPropertyName("required_state")]
    public string RequiredState { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("blocker_type")]
    public string? BlockerType { get; set; }

    [JsonPropertyName("reasons")]
    public List<string> Reasons { get; set; } = new();
}
