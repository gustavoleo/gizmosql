using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class EnvironmentProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("display_profile")]
    public DisplayProfile DisplayProfile { get; set; } = new();

    [JsonPropertyName("require_unlocked_session")]
    public bool RequireUnlockedSession { get; set; } = true;

    [JsonPropertyName("require_single_target_process")]
    public bool RequireSingleTargetProcess { get; set; } = true;

    [JsonPropertyName("require_snagit_when_capture_enabled")]
    public bool RequireSnagitWhenCaptureEnabled { get; set; } = true;

    [JsonPropertyName("required_capture_width")]
    public int? RequiredCaptureWidth { get; set; }

    [JsonPropertyName("required_capture_height")]
    public int? RequiredCaptureHeight { get; set; }
}
