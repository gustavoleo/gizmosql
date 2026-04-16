using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class UiMapRunResult
{
    [JsonPropertyName("startup_state")]
    public StartupStateReport StartupState { get; set; } = new();

    [JsonPropertyName("ui_map")]
    public UiMap UiMap { get; set; } = new();
}

public sealed class UiScreenEvidenceReport
{
    [JsonPropertyName("screen_id")]
    public string ScreenId { get; set; } = string.Empty;

    [JsonPropertyName("route")]
    public UiRoute Route { get; set; } = new();

    [JsonPropertyName("validation_passed")]
    public bool ValidationPassed { get; set; }

    [JsonPropertyName("capture")]
    public CaptureResult Capture { get; set; } = new();

    [JsonPropertyName("png_validation")]
    public PngValidationResult PngValidation { get; set; } = new();

    [JsonPropertyName("readiness")]
    public UiReadiness Readiness { get; set; } = new();
}

public sealed class PngValidationResult
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("horizontal_dpi")]
    public float HorizontalDpi { get; set; }

    [JsonPropertyName("vertical_dpi")]
    public float VerticalDpi { get; set; }

    [JsonPropertyName("expected_dpi")]
    public float ExpectedDpi { get; set; }

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
