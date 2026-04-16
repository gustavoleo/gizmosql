using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace CaptureRunner.Services;

public sealed class UiInteractionExtractor
{
    private static readonly HashSet<string> RelevantControlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Button",
        "CheckBox",
        "ComboBox",
        "DataGrid",
        "DataItem",
        "Edit",
        "Hyperlink",
        "List",
        "ListItem",
        "MenuItem",
        "RadioButton",
        "Tab",
        "TabItem",
        "Tree",
        "TreeItem"
    };

    public List<UiInteraction> Extract(AutomationElement root)
    {
        try
        {
            return root
                .FindAllDescendants()
                .Select(ToInteraction)
                .Where(interaction => interaction is not null)
                .Select(interaction => interaction!)
                .Where(interaction => !IsWindowChrome(interaction))
                .GroupBy(interaction => interaction.InteractionId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(interaction => GetSortPriority(interaction))
                .ThenBy(interaction => interaction.ControlType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(interaction => interaction.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(interaction => interaction.AutomationId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<UiInteraction>();
        }
    }

    private static UiInteraction? ToInteraction(AutomationElement element)
    {
        var controlType = ReadControlType(element);
        if (!RelevantControlTypes.Contains(controlType))
        {
            return null;
        }

        var name = NullIfWhiteSpace(ReadName(element));
        var automationId = NullIfWhiteSpace(ReadAutomationId(element));
        var className = NullIfWhiteSpace(ReadClassName(element));
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(automationId))
        {
            return null;
        }

        var supportedPatterns = ReadSupportedPatterns(element);
        var classification = UiInteractionClassifier.Classify(
            name,
            automationId,
            controlType,
            ReadIsEnabled(element),
            supportedPatterns);

        return new UiInteraction
        {
            InteractionId = BuildInteractionId(name, automationId, controlType, className),
            Name = name,
            AutomationId = automationId,
            ControlType = controlType,
            ClassName = className,
            Bounds = ReadBounds(element),
            IsEnabled = ReadIsEnabled(element),
            IsSelected = ReadIsSelected(element),
            ExpandCollapseState = ReadExpandCollapseState(element),
            SupportedPatterns = supportedPatterns,
            Kind = classification.Kind,
            Risk = classification.Risk,
            Readiness = classification.Readiness,
            RouteCandidate = classification.RouteCandidate
        };
    }

    private static List<string> ReadSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();

        TryAddPattern(patterns, "Invoke", () => element.Patterns.Invoke.TryGetPattern(out _));
        TryAddPattern(patterns, "SelectionItem", () => element.Patterns.SelectionItem.TryGetPattern(out _));
        TryAddPattern(patterns, "ExpandCollapse", () => element.Patterns.ExpandCollapse.TryGetPattern(out _));
        TryAddPattern(patterns, "Value", () => element.Patterns.Value.TryGetPattern(out _));
        TryAddPattern(patterns, "Toggle", () => element.Patterns.Toggle.TryGetPattern(out _));

        return patterns;
    }

    private static void TryAddPattern(List<string> patterns, string name, Func<bool> isSupported)
    {
        try
        {
            if (isSupported())
            {
                patterns.Add(name);
            }
        }
        catch
        {
            // Pattern availability can throw for stale UIA elements.
        }
    }

    private static UiBounds? ReadBounds(AutomationElement element)
    {
        try
        {
            var rect = element.BoundingRectangle;
            return new UiBounds
            {
                Left = rect.Left,
                Top = rect.Top,
                Width = rect.Width,
                Height = rect.Height
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool? ReadIsSelected(AutomationElement element)
    {
        try
        {
            if (element.Patterns.SelectionItem.TryGetPattern(out var pattern))
            {
                return pattern.IsSelected;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? ReadExpandCollapseState(AutomationElement element)
    {
        try
        {
            if (element.Patterns.ExpandCollapse.TryGetPattern(out var pattern))
            {
                return pattern.ExpandCollapseState.ToString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool ReadIsEnabled(AutomationElement element)
    {
        try
        {
            return element.IsEnabled;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadControlType(AutomationElement element)
    {
        try
        {
            return element.ControlType.ToString();
        }
        catch
        {
            return ControlType.Custom.ToString();
        }
    }

    private static string? ReadName(AutomationElement element)
    {
        try
        {
            return element.Name;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadAutomationId(AutomationElement element)
    {
        try
        {
            return element.AutomationId;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadClassName(AutomationElement element)
    {
        try
        {
            return element.ClassName;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildInteractionId(string? name, string? automationId, string controlType, string? className)
    {
        return Slug($"{controlType}-{automationId}-{name}-{className}");
    }

    private static bool IsWindowChrome(UiInteraction interaction)
    {
        if (interaction.ClassName?.Contains("TitleBar", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return true;
        }

        return interaction.Name is "Minimize" or "Maximize" or "Restore" or "Move" or "Size" or "System";
    }

    private static int GetSortPriority(UiInteraction interaction)
    {
        return interaction.Kind switch
        {
            "safe_navigation" => 0,
            "safe_dialog_open" => 1,
            "selection" => 2,
            "input_edit" => 3,
            "safe_close" => 4,
            "destructive" => 90,
            "blocked" => 91,
            _ => 50
        };
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Slug(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        return string.Join('-', new string(chars)
            .Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
