using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class ScreenPlan
{
    [JsonPropertyName("row_id")]
    public string RowId { get; set; } = string.Empty;

    [JsonPropertyName("screen_name")]
    public string ScreenName { get; set; } = string.Empty;

    [JsonPropertyName("module")]
    public string? Module { get; set; }

    [JsonPropertyName("hints")]
    public ScreenHints Hints { get; set; } = new();

    [JsonPropertyName("required_profile")]
    public string? RequiredProfile { get; set; }

    [JsonPropertyName("actions")]
    public List<PlanAction> Actions { get; set; } = new();

    [JsonPropertyName("capture")]
    public CaptureHints Capture { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ScreenHints
{
    [JsonPropertyName("tree_path")]
    public List<string> TreePath { get; set; } = new();

    [JsonPropertyName("shortcut")]
    public List<string> Shortcut { get; set; } = new();

    [JsonPropertyName("expected_controls")]
    public List<string> ExpectedControls { get; set; } = new();

    [JsonPropertyName("expected_control_types")]
    public List<string> ExpectedControlTypes { get; set; } = new();

    [JsonPropertyName("expected_automation_ids")]
    public List<string> ExpectedAutomationIds { get; set; } = new();

    [JsonPropertyName("use_only_expected_controls")]
    public bool UseOnlyExpectedControls { get; set; }

    [JsonPropertyName("require_expected_control_match")]
    public bool RequireExpectedControlMatch { get; set; }

    [JsonPropertyName("require_selected_navigation")]
    public bool RequireSelectedNavigation { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
