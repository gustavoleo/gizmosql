using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CaptureBackend
{
    Snagit,
    Native
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NativeCaptureArea
{
    Window,
    Client
}
