using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class StartupStateReport
{
    [JsonPropertyName("application_started_or_attached")]
    public bool ApplicationStartedOrAttached { get; set; }

    [JsonPropertyName("login_detected")]
    public bool LoginDetected { get; set; }

    [JsonPropertyName("login_attempted")]
    public bool LoginAttempted { get; set; }

    [JsonPropertyName("login_success")]
    public bool LoginSuccess { get; set; }

    [JsonPropertyName("repository_name")]
    public string RepositoryName { get; set; } = string.Empty;

    [JsonPropertyName("repository_selection_detected")]
    public bool RepositorySelectionDetected { get; set; }

    [JsonPropertyName("repository_selection_attempted")]
    public bool RepositorySelectionAttempted { get; set; }

    [JsonPropertyName("repository_selected")]
    public bool RepositorySelected { get; set; }

    [JsonPropertyName("repository_verified")]
    public bool RepositoryVerified { get; set; }

    [JsonPropertyName("method_used")]
    public string MethodUsed { get; set; } = "none";

    [JsonPropertyName("active_window_title")]
    public string? ActiveWindowTitle { get; set; }

    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }

    [JsonPropertyName("windows_seen")]
    public List<StartupWindowState> WindowsSeen { get; set; } = new();

    [JsonPropertyName("candidate_login_actions")]
    public List<StartupActionState> CandidateLoginActions { get; set; } = new();

    [JsonPropertyName("candidate_repository_selectors")]
    public List<StartupActionState> CandidateRepositorySelectors { get; set; } = new();

    [JsonPropertyName("notes")]
    public List<string> Notes { get; set; } = new();

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }
}

public sealed class StartupWindowState
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("control_type")]
    public string? ControlType { get; set; }

    [JsonPropertyName("process_id")]
    public int? ProcessId { get; set; }
}

public sealed class StartupActionState
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("automation_id")]
    public string? AutomationId { get; set; }

    [JsonPropertyName("control_type")]
    public string? ControlType { get; set; }
}
