using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class UiRoutePromotionSummary
{
    [JsonPropertyName("generated_at_utc")]
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("promotion_source")]
    public string PromotionSource { get; set; } = "none";

    [JsonPropertyName("rejected_route_count")]
    public int RejectedRouteCount { get; set; }

    [JsonPropertyName("remaining_queued_route_count")]
    public int RemainingQueuedRouteCount { get; set; }

    [JsonPropertyName("candidate_groups")]
    public List<UiRoutePromotionCandidateGroup> CandidateGroups { get; set; } = new();
}

public sealed class UiRoutePromotionCandidateGroup
{
    [JsonPropertyName("leaf_name")]
    public string? LeafName { get; set; }

    [JsonPropertyName("leaf_automation_id")]
    public string? LeafAutomationId { get; set; }

    [JsonPropertyName("leaf_control_type")]
    public string? LeafControlType { get; set; }

    [JsonPropertyName("leaf_action")]
    public string? LeafAction { get; set; }

    [JsonPropertyName("occurrence_count")]
    public int OccurrenceCount { get; set; }

    [JsonPropertyName("source_route_texts")]
    public List<string> SourceRouteTexts { get; set; } = new();

    [JsonPropertyName("suggested_recipe_routes")]
    public List<UiRoutePromotionSuggestion> SuggestedRecipeRoutes { get; set; } = new();
}

public sealed class UiRoutePromotionSuggestion
{
    [JsonPropertyName("parent_route_text")]
    public string ParentRouteText { get; set; } = string.Empty;

    [JsonPropertyName("candidate_route_text")]
    public string CandidateRouteText { get; set; } = string.Empty;

    [JsonPropertyName("evidence_source")]
    public string EvidenceSource { get; set; } = string.Empty;

    [JsonPropertyName("match_count")]
    public int MatchCount { get; set; }

    [JsonPropertyName("already_configured")]
    public bool AlreadyConfigured { get; set; }
}
