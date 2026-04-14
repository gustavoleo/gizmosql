using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class DisplayProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("capture_display")]
    public DisplayTarget? CaptureDisplay { get; set; }

    [JsonPropertyName("operator_display")]
    public DisplayTarget? OperatorDisplay { get; set; }
}

public sealed class DisplayTarget
{
    [JsonPropertyName("role")]
    public DisplayRole Role { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("primary")]
    public bool? Primary { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("scale_percent")]
    public int? ScalePercent { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisplayRole
{
    Capture,
    Operator
}

public sealed class DisplayRuntimeInfo
{
    public required string DeviceName { get; init; }

    public required bool Primary { get; init; }

    public required int Left { get; init; }

    public required int Top { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required int WorkingLeft { get; init; }

    public required int WorkingTop { get; init; }

    public required int WorkingWidth { get; init; }

    public required int WorkingHeight { get; init; }

    public required int ScalePercent { get; init; }
}
