using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class PlanAction
{
    [JsonPropertyName("kind")]
    public PlanActionKind Kind { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("automation_id")]
    public string? AutomationId { get; set; }

    [JsonPropertyName("control_type")]
    public string? ControlType { get; set; }

    [JsonPropertyName("keys")]
    public string? Keys { get; set; }

    [JsonPropertyName("window_title")]
    public string? WindowTitle { get; set; }

    [JsonPropertyName("match_mode")]
    public WindowTitleMatchMode MatchMode { get; set; } = WindowTitleMatchMode.Contains;

    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    [JsonPropertyName("timeout_ms")]
    public int? TimeoutMs { get; set; }

    [JsonPropertyName("post_action_delay_ms")]
    public int? PostActionDelayMs { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlanActionKind
{
    Invoke,
    WaitForElement,
    WaitForWindow,
    SendKeys,
    Delay
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WindowTitleMatchMode
{
    Contains,
    Exact
}
