using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class PlacementResult
{
    [JsonPropertyName("attempted")]
    public bool Attempted { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("left")]
    public int? Left { get; set; }

    [JsonPropertyName("top")]
    public int? Top { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("center_on_target")]
    public bool? CenterOnTarget { get; set; }

    [JsonPropertyName("contained_in_target")]
    public bool? ContainedInTarget { get; set; }

    [JsonPropertyName("overlapped_other_display")]
    public bool? OverlappedOtherDisplay { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
