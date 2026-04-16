using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class RoutePromotionSummaryService
{
    public UiRoutePromotionSummary Build(
        UiMap map,
        IReadOnlyCollection<UiRejectedRoute> rejectedRoutes,
        IReadOnlyCollection<UiRoute> configuredRoutes)
    {
        var summary = new UiRoutePromotionSummary
        {
            RejectedRouteCount = rejectedRoutes.Count,
            RemainingQueuedRouteCount = map.RemainingQueuedRoutes.Count
        };

        var sourceRoutes = rejectedRoutes.Count > 0
            ? rejectedRoutes.Select(route => route.Route).ToList()
            : map.RemainingQueuedRoutes.ToList();
        summary.PromotionSource = rejectedRoutes.Count > 0
            ? "rejected_routes"
            : sourceRoutes.Count > 0
                ? "remaining_queued_routes"
                : "none";

        if (sourceRoutes.Count == 0)
        {
            return summary;
        }

        summary.CandidateGroups = sourceRoutes
            .Select(route => new RouteLeaf(route, GetLeafStep(route)))
            .Where(candidate => candidate.Leaf is not null)
            .GroupBy(candidate => BuildLeafKey(candidate.Leaf!))
            .Select(group => BuildCandidateGroup(group, map, configuredRoutes))
            .OrderByDescending(group => group.OccurrenceCount)
            .ThenBy(group => group.LeafName ?? group.LeafAutomationId ?? group.LeafControlType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return summary;
    }

    private static UiRoutePromotionCandidateGroup BuildCandidateGroup(
        IGrouping<string, RouteLeaf> group,
        UiMap map,
        IReadOnlyCollection<UiRoute> configuredRoutes)
    {
        var routes = group.ToList();
        var leaf = group.First().Leaf!;
        var suggestions = new Dictionary<string, UiRoutePromotionSuggestion>(StringComparer.OrdinalIgnoreCase);

        foreach (var route in configuredRoutes.Where(route => GetConfiguredDepth(route) > 1 && StepsMatch(GetLeafStep(route), leaf)))
        {
            var parent = GetParentRouteText(route);
            if (string.IsNullOrWhiteSpace(parent))
            {
                continue;
            }

            AddSuggestion(
                suggestions,
                parent,
                route.RouteText,
                evidenceSource: "configured_route",
                alreadyConfigured: true);
        }

        if (ShouldSuggestParentRoutes(leaf, routes))
        {
            foreach (var screen in map.Screens.Where(screen => screen.Readiness.Status == "accepted" && !IsCurrentShell(screen.Route)))
            {
                foreach (var interaction in screen.AvailableInteractions.Where(IsSafeRoute))
                {
                    if (!InteractionMatchesStep(interaction, leaf))
                    {
                        continue;
                    }

                    var parent = screen.Route.RouteText;
                    if (string.IsNullOrWhiteSpace(parent))
                    {
                        continue;
                    }

                    var label = leaf.Name ?? leaf.AutomationId ?? leaf.ControlType ?? "Unnamed";
                    AddSuggestion(
                        suggestions,
                        parent,
                        $"{parent} > {label}",
                        evidenceSource: "accepted_screen_interaction",
                        alreadyConfigured: false);
                }
            }
        }

        return new UiRoutePromotionCandidateGroup
        {
            LeafName = leaf.Name,
            LeafAutomationId = leaf.AutomationId,
            LeafControlType = leaf.ControlType,
            LeafAction = leaf.Action,
            OccurrenceCount = routes.Count,
            SourceRouteTexts = routes
                .Select(candidate => candidate.Route.RouteText)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            SuggestedRecipeRoutes = suggestions.Values
                .OrderByDescending(suggestion => suggestion.AlreadyConfigured)
                .ThenByDescending(suggestion => suggestion.MatchCount)
                .ThenBy(suggestion => suggestion.CandidateRouteText, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static bool ShouldSuggestParentRoutes(UiRouteStep leaf, IReadOnlyCollection<RouteLeaf> routes)
    {
        var depthOneOnly = routes.All(route => GetConfiguredDepth(route.Route) == 1);
        var topLevelShellNavigation = depthOneOnly && leaf.ControlType is "TabItem" or "TreeItem";
        return !topLevelShellNavigation;
    }

    private static void AddSuggestion(
        IDictionary<string, UiRoutePromotionSuggestion> suggestions,
        string parentRouteText,
        string candidateRouteText,
        string evidenceSource,
        bool alreadyConfigured)
    {
        if (suggestions.TryGetValue(candidateRouteText, out var existing))
        {
            existing.MatchCount++;
            existing.AlreadyConfigured |= alreadyConfigured;
            return;
        }

        suggestions[candidateRouteText] = new UiRoutePromotionSuggestion
        {
            ParentRouteText = parentRouteText,
            CandidateRouteText = candidateRouteText,
            EvidenceSource = evidenceSource,
            MatchCount = 1,
            AlreadyConfigured = alreadyConfigured
        };
    }

    private static bool IsSafeRoute(UiInteraction interaction)
    {
        return interaction.RouteCandidate
               && interaction.Risk == "safe"
               && interaction.Readiness == "ready"
               && interaction.IsEnabled;
    }

    private static bool IsCurrentShell(UiRoute route)
    {
        return string.Equals(route.RouteType, "current", StringComparison.OrdinalIgnoreCase)
               || string.Equals(route.RouteText, "Current Northwind shell", StringComparison.OrdinalIgnoreCase);
    }

    private static UiRouteStep? GetLeafStep(UiRoute route)
    {
        return route.Steps.LastOrDefault(step =>
            !string.Equals(step.Kind, "bootstrap", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(step.Action, "verify", StringComparison.OrdinalIgnoreCase));
    }

    private static int GetConfiguredDepth(UiRoute route)
    {
        return route.Steps.Count(step =>
            !string.Equals(step.Kind, "bootstrap", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(step.Action, "verify", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildLeafKey(UiRouteStep step)
    {
        return string.Join("|",
            Normalize(step.Name),
            Normalize(step.AutomationId),
            Normalize(step.ControlType ?? step.Kind),
            Normalize(step.Action));
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private static bool StepsMatch(UiRouteStep? left, UiRouteStep right)
    {
        if (left is null)
        {
            return false;
        }

        return string.Equals(left.Name ?? string.Empty, right.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.AutomationId ?? string.Empty, right.AutomationId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.ControlType ?? left.Kind ?? string.Empty, right.ControlType ?? right.Kind ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.Action ?? string.Empty, right.Action ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool InteractionMatchesStep(UiInteraction interaction, UiRouteStep step)
    {
        return string.Equals(interaction.Name ?? string.Empty, step.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && string.Equals(interaction.AutomationId ?? string.Empty, step.AutomationId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && string.Equals(interaction.ControlType ?? string.Empty, step.ControlType ?? step.Kind ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetParentRouteText(UiRoute route)
    {
        var index = route.RouteText.LastIndexOf(" > ", StringComparison.Ordinal);
        return index <= 0
            ? string.Empty
            : route.RouteText[..index];
    }

    private sealed record RouteLeaf(UiRoute Route, UiRouteStep? Leaf);
}
