using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class WindowDiscoveryReport
{
    [JsonPropertyName("window_title")]
    public string? WindowTitle { get; set; }

    [JsonPropertyName("total_descendants")]
    public int TotalDescendants { get; set; }

    [JsonPropertyName("control_type_summary")]
    public List<ControlTypeCount> ControlTypeSummary { get; set; } = new();

    [JsonPropertyName("likely_modules")]
    public List<DiscoveryElement> LikelyModules { get; set; } = new();

    [JsonPropertyName("likely_screens")]
    public List<DiscoveryElement> LikelyScreens { get; set; } = new();

    [JsonPropertyName("interesting_controls")]
    public List<DiscoveryElement> InterestingControls { get; set; } = new();

    [JsonPropertyName("notes")]
    public List<string> Notes { get; set; } = new();
}

public sealed class ControlTypeCount
{
    [JsonPropertyName("control_type")]
    public string ControlType { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class DiscoveryElement
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("control_type")]
    public string ControlType { get; set; } = string.Empty;

    [JsonPropertyName("automation_id")]
    public string? AutomationId { get; set; }

    [JsonPropertyName("class_name")]
    public string? ClassName { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
