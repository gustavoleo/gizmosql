using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class CaptureHints
{
    [JsonPropertyName("mode")]
    public CaptureMode Mode { get; set; } = CaptureMode.Default;

    [JsonPropertyName("preset_hotkey")]
    public string? PresetHotkey { get; set; }

    [JsonPropertyName("watch_dir")]
    public string? WatchDirectory { get; set; }

    [JsonPropertyName("click_strategy")]
    public WindowClickStrategy ClickStrategy { get; set; } = WindowClickStrategy.TitleBarCenter;

    [JsonPropertyName("retry_click_strategies")]
    public List<WindowClickStrategy> RetryClickStrategies { get; set; } = new();

    [JsonPropertyName("selection_settle_ms")]
    public int? SelectionSettleMs { get; set; } = 400;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CaptureMode
{
    Default,
    Window,
    Menu,
    Region
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WindowClickStrategy
{
    TitleBarCenter,
    ClientCenter,
    TopLeftInset
}
