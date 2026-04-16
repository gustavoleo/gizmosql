namespace CaptureRunner.Services;

public static class UiInteractionClassifier
{
    private static readonly string[] DestructiveTerms =
    [
        "delete",
        "remove",
        "save",
        "sync",
        "synchronize",
        "run",
        "publish",
        "upgrade",
        "overwrite",
        "drop",
        "truncate",
        "clear",
        "reset",
        "finish",
        "apply",
        "execute",
        "new",
        "exit",
        "quit",
        "disconnect",
        "logout",
        "log out",
        "move",
        "size"
    ];

    private static readonly string[] SafeNavigationNames =
    [
        "home",
        "file",
        "sources",
        "dwh",
        "data mart",
        "etl",
        "deployment",
        "options",
        "help"
    ];

    private static readonly string[] SafeDialogNames =
    [
        "about",
        "eula",
        "interface settings",
        "dwh settings",
        "find on diagram"
    ];

    private static readonly string[] SafeDetailNames =
    [
        "edit",
        "open",
        "view",
        "details",
        "properties",
        "list",
        "list objects",
        "show"
    ];

    private static readonly string[] CloseNames =
    [
        "close",
        "cancel",
        "back"
    ];

    public static ClassifiedInteraction Classify(
        string? name,
        string? automationId,
        string controlType,
        bool isEnabled,
        IReadOnlyCollection<string> supportedPatterns)
    {
        if (!isEnabled)
        {
            return new ClassifiedInteraction("blocked", "blocked", "blocked", false);
        }

        var normalizedName = Normalize(name);
        if (ContainsDestructiveTerm(normalizedName))
        {
            return new ClassifiedInteraction("destructive", "destructive", "blocked", false);
        }

        if (controlType is "TabItem" or "TreeItem")
        {
            return new ClassifiedInteraction("safe_navigation", "safe", "ready", true);
        }

        if (controlType is "MenuItem")
        {
            if (normalizedName.Contains("wizard", StringComparison.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_wizard_entry", "safe", "ready", true);
            }

            if (SafeDialogNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_dialog_open", "safe", "ready", true);
            }

            if (SafeDetailNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_detail_open", "safe", "ready", true);
            }

            return new ClassifiedInteraction("safe_navigation", "safe", "ready", !string.IsNullOrWhiteSpace(normalizedName));
        }

        if (controlType is "Edit")
        {
            return new ClassifiedInteraction("input_edit", "data_entry", "review", false);
        }

        if (controlType is "ComboBox" or "ListItem" or "DataGrid" or "DataItem" or "List")
        {
            return new ClassifiedInteraction("selection", "state_change", "review", false);
        }

        if (controlType is "Button" or "Hyperlink")
        {
            if (CloseNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_close", "safe", "ready", false);
            }

            if (SafeNavigationNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_navigation", "safe", "ready", true);
            }

            if (SafeDialogNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_dialog_open", "safe", "ready", true);
            }

            if (SafeDetailNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_detail_open", "safe", "ready", true);
            }

            if (normalizedName.Contains("wizard", StringComparison.OrdinalIgnoreCase))
            {
                return new ClassifiedInteraction("safe_wizard_entry", "safe", "ready", true);
            }

            return supportedPatterns.Contains("Invoke", StringComparer.OrdinalIgnoreCase)
                ? new ClassifiedInteraction("unknown_invoke", "unknown", "review", false)
                : new ClassifiedInteraction("unknown", "unknown", "review", false);
        }

        if (controlType is "CheckBox" or "RadioButton")
        {
            return new ClassifiedInteraction("selection", "state_change", "review", false);
        }

        return new ClassifiedInteraction("unknown", "unknown", "review", false);
    }

    private static bool ContainsDestructiveTerm(string normalizedName)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        return DestructiveTerms.Any(term =>
            normalizedName.Equals(term, StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains($" {term} ", StringComparison.OrdinalIgnoreCase)
            || normalizedName.StartsWith($"{term} ", StringComparison.OrdinalIgnoreCase)
            || normalizedName.EndsWith($" {term}", StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}

public sealed record ClassifiedInteraction(
    string Kind,
    string Risk,
    string Readiness,
    bool RouteCandidate);
