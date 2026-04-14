using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class PreflightReport
{
    [JsonPropertyName("profile_name")]
    public string? ProfileName { get; set; }

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("actual_displays")]
    public List<DisplayRuntimeInfo> ActualDisplays { get; set; } = new();

    [JsonPropertyName("checks")]
    public List<PreflightCheck> Checks { get; set; } = new();
}

public sealed class PreflightCheck
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
