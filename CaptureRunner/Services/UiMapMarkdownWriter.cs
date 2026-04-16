using System.Globalization;
using System.Text;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class UiMapMarkdownWriter
{
    public string Render(UiMap map)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AnalyticsCreator UI Map");
        builder.AppendLine();
        builder.AppendLine($"Generated UTC: `{map.GeneratedAtUtc:O}`");
        builder.AppendLine($"Repository: `{map.RepositoryName}`");
        builder.AppendLine($"Accepted threshold: `{map.AcceptedThreshold.ToString("P2", CultureInfo.InvariantCulture)}`");
        builder.AppendLine();
        builder.AppendLine("## Coverage");
        builder.AppendLine();
        builder.AppendLine("| discovered | accepted | review | rejected | blocked | accepted ratio | threshold met |");
        builder.AppendLine("|---:|---:|---:|---:|---:|---:|---|");
        builder.AppendLine(
            $"| {map.Coverage.DiscoveredCount} | {map.Coverage.AcceptedCount} | {map.Coverage.ReviewCount} | {map.Coverage.RejectedCount} | {map.Coverage.BlockedCount} | {map.Coverage.AcceptedRatio.ToString("P2", CultureInfo.InvariantCulture)} | {map.Coverage.ThresholdMet} |");
        builder.AppendLine();
        foreach (var note in map.Coverage.Notes)
        {
            builder.AppendLine($"- {Escape(note)}");
        }

        if (map.Coverage.Notes.Count > 0)
        {
            builder.AppendLine();
        }

        if (map.RemainingQueuedRoutes.Count > 0)
        {
            builder.AppendLine("## Remaining Queued Routes");
            builder.AppendLine();
            builder.AppendLine($"Queued routes not yet executed: `{map.RemainingQueuedRoutes.Count}`");
            builder.AppendLine();
            foreach (var route in map.RemainingQueuedRoutes.Take(20))
            {
                builder.AppendLine($"- `{Escape(route.RouteText)}`");
            }

            if (map.RemainingQueuedRoutes.Count > 20)
            {
                builder.AppendLine($"- ... `{map.RemainingQueuedRoutes.Count - 20}` more in `remaining-queued-routes.json`");
            }

            builder.AppendLine();
        }

        var rejectedRoutes = map.Screens
            .Where(screen => screen.Readiness.Status == "rejected")
            .ToList();
        if (rejectedRoutes.Count > 0)
        {
            builder.AppendLine("## Rejected Routes");
            builder.AppendLine();
            foreach (var screen in rejectedRoutes)
            {
                var reason = screen.Readiness.Reasons.FirstOrDefault() ?? "Rejected without a recorded reason.";
                builder.AppendLine($"- `{Escape(screen.Route.RouteText)}`: {Escape(reason)}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Screens");
        builder.AppendLine();
        builder.AppendLine("| screen | readiness | confidence | route | validation anchors | screenshot |");
        builder.AppendLine("|---|---|---|---|---|---|");

        foreach (var screen in map.Screens.OrderBy(screen => screen.ScreenId, StringComparer.OrdinalIgnoreCase))
        {
            var anchors = string.Join("<br>", screen.ValidationAnchors
                .Take(5)
                .Select(anchor => $"{Escape(anchor.AnchorType)}: `{Escape(anchor.Value)}`"));
            var screenshot = string.IsNullOrWhiteSpace(screen.Evidence.ScreenshotPng)
                ? string.Empty
                : Escape(screen.Evidence.ScreenshotPng);

            var routeText = screen.AlternateRoutes.Count == 0
                ? Escape(screen.Route.RouteText)
                : $"{Escape(screen.Route.RouteText)}<br>+ {screen.AlternateRoutes.Count} alternate";

            builder.AppendLine(
                $"| `{Escape(screen.ScreenId)}`<br>{Escape(screen.ScreenName)} | {Escape(screen.Readiness.Status)} | {Escape(screen.Readiness.Confidence)} | {routeText} | {anchors} | {screenshot} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Interaction Summary");
        builder.AppendLine();
        builder.AppendLine("| screen | safe routes | review | blocked/destructive |");
        builder.AppendLine("|---|---:|---:|---:|");
        foreach (var screen in map.Screens.OrderBy(screen => screen.ScreenId, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine(
                $"| `{Escape(screen.ScreenId)}` | {screen.AvailableInteractions.Count(IsSafeRoute)} | {screen.AvailableInteractions.Count(interaction => interaction.Readiness == "review")} | {screen.AvailableInteractions.Count(interaction => interaction.Readiness == "blocked" || interaction.Risk == "destructive")} |");
        }

        return builder.ToString();
    }

    private static bool IsSafeRoute(UiInteraction interaction)
    {
        return interaction.RouteCandidate
               && interaction.Risk == "safe"
               && interaction.Readiness == "ready";
    }

    private static string Escape(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
