using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace CaptureRunner.Services;

public sealed class TreeService
{
    public TreeSnapshot CaptureSnapshot(AutomationElement root)
    {
        var descendants = root.FindAllDescendants().ToArray();
        var elements = descendants.Select(ToElementInfo).ToList();

        return new TreeSnapshot(
            elements,
            elements.Count(element => element.IsInteractive && string.IsNullOrWhiteSpace(element.AutomationId)),
            DetectVirtualization(elements),
            DetectCustomRendering(elements));
    }

    public List<DetectedElement> FindTargetElements(
        TreeSnapshot snapshot,
        IEnumerable<string> expectedNames,
        IEnumerable<string> expectedAutomationIds,
        IEnumerable<string> expectedControlTypes)
    {
        var names = expectedNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var automationIds = expectedAutomationIds
            .Where(automationId => !string.IsNullOrWhiteSpace(automationId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var controlTypes = expectedControlTypes
            .Where(controlType => !string.IsNullOrWhiteSpace(controlType))
            .Select(controlType => controlType.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return snapshot.Elements
            .Where(element => MatchesAnyName(element.Name, names) || MatchesAnyAutomationId(element.AutomationId, automationIds))
            .Where(element => controlTypes.Count == 0 || controlTypes.Contains(element.ControlType))
            .GroupBy(element => $"{element.Name}|{element.ControlType}|{element.AutomationId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(element => new DetectedElement
            {
                Name = element.Name,
                ControlType = element.ControlType,
                AutomationId = NullIfEmpty(element.AutomationId)
            })
            .Take(20)
            .ToList();
    }

    public List<string> FindMissingTargets(TreeSnapshot snapshot, IEnumerable<string> expectedNames, IEnumerable<string> expectedAutomationIds)
    {
        var names = expectedNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingNames = names
            .Where(name => !snapshot.Elements.Any(element => MatchesName(element.Name, name)))
            .ToList();

        var automationIds = expectedAutomationIds
            .Where(automationId => !string.IsNullOrWhiteSpace(automationId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingIds = automationIds
            .Where(automationId => !snapshot.Elements.Any(element => MatchesAutomationId(element.AutomationId, automationId)))
            .Select(automationId => $"automation_id:{automationId}")
            .ToList();

        return missingNames.Concat(missingIds).ToList();
    }

    public StabilityResult CompareSnapshots(TreeSnapshot first, TreeSnapshot second)
    {
        var firstSignatures = BuildSignatureMap(first.Elements);
        var secondSignatures = BuildSignatureMap(second.Elements);
        var keys = firstSignatures.Keys.Union(secondSignatures.Keys, StringComparer.OrdinalIgnoreCase);

        var differenceCount = 0;
        foreach (var key in keys)
        {
            firstSignatures.TryGetValue(key, out var firstCount);
            secondSignatures.TryGetValue(key, out var secondCount);
            differenceCount += Math.Abs(firstCount - secondCount);
        }

        var consistent = differenceCount == 0;

        return new StabilityResult
        {
            Consistent = consistent,
            FirstScanCount = first.Elements.Count,
            SecondScanCount = second.Elements.Count,
            DifferenceCount = differenceCount
        };
    }

    public List<ControlTypeCount> BuildControlTypeSummary(TreeSnapshot snapshot)
    {
        return snapshot.Elements
            .GroupBy(element => element.ControlType, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ControlTypeCount
            {
                ControlType = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.ControlType, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<DiscoveryElement> ExtractCandidates(TreeSnapshot snapshot, params string[] controlTypes)
    {
        var allowed = controlTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return snapshot.Elements
            .Where(element => !string.IsNullOrWhiteSpace(element.Name))
            .Where(element => allowed.Contains(element.ControlType))
            .GroupBy(
                element => new
                {
                    element.Name,
                    element.ControlType,
                    element.AutomationId,
                    element.ClassName
                })
            .Select(group => new DiscoveryElement
            {
                Name = group.Key.Name,
                ControlType = group.Key.ControlType,
                AutomationId = group.Key.AutomationId,
                ClassName = group.Key.ClassName,
                Count = group.Count()
            })
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ControlType, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<DiscoveryElement> ExtractInterestingControls(TreeSnapshot snapshot)
    {
        var interestingTypes = new[]
        {
            "Button",
            "CheckBox",
            "ComboBox",
            "DataGrid",
            "Edit",
            "Hyperlink",
            "List",
            "ListItem",
            "MenuItem",
            "RadioButton",
            "Tab",
            "TabItem",
            "Text",
            "Tree",
            "TreeItem"
        }.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return snapshot.Elements
            .Where(element => interestingTypes.Contains(element.ControlType))
            .Where(element => !string.IsNullOrWhiteSpace(element.Name) || !string.IsNullOrWhiteSpace(element.AutomationId))
            .Where(element => !IsWindowChrome(element))
            .GroupBy(
                element => new
                {
                    element.Name,
                    element.ControlType,
                    element.AutomationId,
                    element.ClassName
                })
            .Select(group => new DiscoveryElement
            {
                Name = group.Key.Name,
                ControlType = group.Key.ControlType,
                AutomationId = group.Key.AutomationId,
                ClassName = group.Key.ClassName,
                Count = group.Count()
            })
            .OrderBy(item => GetInterestingControlPriority(item))
            .ThenByDescending(item => !string.IsNullOrWhiteSpace(item.AutomationId))
            .ThenBy(item => item.AutomationId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(150)
            .ToList();
    }

    public bool ContainsAnyNamedDescendant(AutomationElement root, IEnumerable<string> candidateNames)
    {
        var names = candidateNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return false;
        }

        return root
            .FindAllDescendants()
            .Any(element => MatchesAnyName(ReadName(element), names));
    }

    public bool ContainsAllNamedDescendants(AutomationElement root, IEnumerable<string> candidateNames)
    {
        var names = candidateNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return true;
        }

        var descendants = root.FindAllDescendants().ToList();
        return names.All(name => descendants.Any(element => MatchesName(ReadName(element), name)));
    }

    public bool ContainsAnyAutomationIdDescendant(AutomationElement root, IEnumerable<string> candidateAutomationIds)
    {
        var automationIds = candidateAutomationIds
            .Where(automationId => !string.IsNullOrWhiteSpace(automationId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (automationIds.Count == 0)
        {
            return false;
        }

        return root
            .FindAllDescendants()
            .Any(element => MatchesAnyAutomationId(ReadAutomationId(element), automationIds));
    }

    public bool ContainsAllAutomationIdDescendants(AutomationElement root, IEnumerable<string> candidateAutomationIds)
    {
        var automationIds = candidateAutomationIds
            .Where(automationId => !string.IsNullOrWhiteSpace(automationId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (automationIds.Count == 0)
        {
            return true;
        }

        var descendants = root.FindAllDescendants().ToList();
        return automationIds.All(automationId => descendants.Any(element => MatchesAutomationId(ReadAutomationId(element), automationId)));
    }

    public bool HasSelectedNavigationMatch(AutomationElement root, IEnumerable<string> candidateNames)
    {
        var names = candidateNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return false;
        }

        var matches = root
            .FindAllDescendants()
            .Where(element => MatchesAnyName(ReadName(element), names))
            .Where(element => ReadControlType(element) is "TreeItem" or "TabItem" or "ListItem")
            .ToList();

        return matches.Any(IsSelected);
    }

    public AutomationElement? FindFirstMatchingDescendant(AutomationElement root, IEnumerable<string> candidateNames, params string[] preferredControlTypes)
    {
        var names = candidateNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return null;
        }

        var preferredOrder = preferredControlTypes
            .Where(controlType => !string.IsNullOrWhiteSpace(controlType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((controlType, index) => new { controlType, index })
            .ToDictionary(item => item.controlType, item => item.index, StringComparer.OrdinalIgnoreCase);

        var matches = root
            .FindAllDescendants()
            .Where(element => MatchesAnyName(ReadName(element), names))
            .Select(element => new
            {
                Element = element,
                ControlType = ReadControlType(element)
            })
            .OrderBy(item => preferredOrder.TryGetValue(item.ControlType, out var index) ? index : int.MaxValue)
            .ThenBy(item => item.ControlType)
            .ToList();

        return matches.FirstOrDefault()?.Element;
    }

    public AutomationElement? FindFirstActionTarget(AutomationElement root, PlanAction action)
    {
        var descendants = root.FindAllDescendants().ToList();

        var controlTypeFilter = string.IsNullOrWhiteSpace(action.ControlType)
            ? null
            : action.ControlType.Trim();

        var matches = descendants
            .Where(element => MatchesActionTarget(element, action, controlTypeFilter))
            .Select(element => new
            {
                Element = element,
                ControlType = ReadControlType(element)
            })
            .OrderBy(item => controlTypeFilter is not null && item.ControlType.Equals(controlTypeFilter, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(item => item.ControlType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return matches.FirstOrDefault()?.Element;
    }

    public bool ContainsActionTarget(AutomationElement root, PlanAction action)
    {
        return FindFirstActionTarget(root, action) is not null;
    }

    public static IReadOnlyList<string> BuildExpectedNames(ScreenPlan plan)
    {
        var names = new List<string>();

        if (plan.Hints.UseOnlyExpectedControls)
        {
            names.AddRange(plan.Hints.ExpectedControls);
            if (names.Count == 0 && !string.IsNullOrWhiteSpace(plan.ScreenName))
            {
                names.Add(plan.ScreenName);
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(plan.ScreenName))
            {
                names.Add(plan.ScreenName);
            }

            if (!string.IsNullOrWhiteSpace(plan.Module))
            {
                names.Add(plan.Module);
            }

            if (plan.Hints.TreePath.Count > 0)
            {
                names.Add(plan.Hints.TreePath[^1]);
            }

            names.AddRange(plan.Hints.ExpectedControls);
        }

        return names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, int> BuildSignatureMap(IEnumerable<TreeElementInfo> elements)
    {
        return elements
            .GroupBy(element => element.Signature, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static TreeElementInfo ToElementInfo(AutomationElement element)
    {
        var name = ReadName(element);
        var controlType = ReadControlType(element);
        var automationId = ReadAutomationId(element);
        var className = ReadClassName(element);

        return new TreeElementInfo(
            name,
            controlType,
            automationId,
            className,
            IsInteractiveControl(controlType));
    }

    private static bool DetectVirtualization(IReadOnlyList<TreeElementInfo> elements)
    {
        var hasVirtualizableContainer = elements.Any(element =>
            element.ControlType is "Tree" or "List" or "DataGrid");
        var hasRealizedItems = elements.Any(element =>
            element.ControlType is "TreeItem" or "ListItem" or "DataItem");

        return hasVirtualizableContainer && !hasRealizedItems;
    }

    private static bool DetectCustomRendering(IReadOnlyList<TreeElementInfo> elements)
    {
        return elements.Any(element =>
            element.ControlType == "Custom"
            || (element.ClassName?.Contains("Canvas", StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.ClassName?.Contains("Diagram", StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.ClassName?.Contains("Drawing", StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.ClassName?.Contains("Skia", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private static bool MatchesAnyName(string? candidate, IEnumerable<string> expectedNames)
    {
        return expectedNames.Any(name => MatchesName(candidate, name));
    }

    private static bool MatchesActionTarget(AutomationElement element, PlanAction action, string? controlTypeFilter)
    {
        if (controlTypeFilter is not null
            && !ReadControlType(element).Equals(controlTypeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nameMatch = !string.IsNullOrWhiteSpace(action.Name)
                        && MatchesName(ReadName(element), action.Name);
        var automationIdMatch = !string.IsNullOrWhiteSpace(action.AutomationId)
                                && MatchesAutomationId(ReadAutomationId(element), action.AutomationId);

        if (!string.IsNullOrWhiteSpace(action.Name) || !string.IsNullOrWhiteSpace(action.AutomationId))
        {
            return nameMatch || automationIdMatch;
        }

        return controlTypeFilter is not null;
    }

    private static bool MatchesAnyAutomationId(string? candidate, IEnumerable<string> expectedAutomationIds)
    {
        return expectedAutomationIds.Any(automationId => MatchesAutomationId(candidate, automationId));
    }

    private static bool MatchesName(string? candidate, string expected)
    {
        return !string.IsNullOrWhiteSpace(candidate)
               && candidate.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAutomationId(string? candidate, string expected)
    {
        return !string.IsNullOrWhiteSpace(candidate)
               && candidate.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInteractiveControl(string controlType)
    {
        return controlType is
            "Button" or
            "CheckBox" or
            "ComboBox" or
            "DataGrid" or
            "DataItem" or
            "Edit" or
            "Hyperlink" or
            "List" or
            "ListItem" or
            "MenuItem" or
            "RadioButton" or
            "Tab" or
            "TabItem" or
            "Tree" or
            "TreeItem";
    }

    private static string ReadControlType(AutomationElement element)
    {
        try
        {
            return element.ControlType.ToString();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static bool IsSelected(AutomationElement element)
    {
        try
        {
            return ReadControlType(element) switch
            {
                "TreeItem" => element.AsTreeItem().IsSelected,
                "TabItem" => element.AsTabItem().IsSelected,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadName(AutomationElement element)
    {
        try
        {
            return NullIfEmpty(element.Name);
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
            return NullIfEmpty(element.AutomationId);
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
            return NullIfEmpty(element.ClassName);
        }
        catch
        {
            return null;
        }
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int GetInterestingControlPriority(DiscoveryElement element)
    {
        var priority = element.ControlType switch
        {
            "DataGrid" => 0,
            "DataItem" => 1,
            "Edit" => 2,
            "ComboBox" => 3,
            "CheckBox" => 4,
            "RadioButton" => 5,
            "List" => 6,
            "ListItem" => 7,
            "TabItem" => 8,
            "TreeItem" => 9,
            "Button" => 10,
            "Text" => 11,
            _ => 50
        };

        if (string.IsNullOrWhiteSpace(element.AutomationId))
        {
            priority += 20;
        }

        if (element.ClassName?.Contains("Ribbon", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            priority += 20;
        }

        return priority;
    }

    private static bool IsWindowChrome(TreeElementInfo element)
    {
        if (string.Equals(element.AutomationId, "Expander", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (element.Name is "Close" or "Minimize" or "Restore")
        {
            return true;
        }

        return element.ClassName?.Contains("TitleBar", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}

public sealed record TreeSnapshot(
    IReadOnlyList<TreeElementInfo> Elements,
    int MissingAutomationIdCount,
    bool PotentialVirtualization,
    bool PotentialCustomRendering);

public sealed record TreeElementInfo(
    string? Name,
    string ControlType,
    string? AutomationId,
    string? ClassName,
    bool IsInteractive)
{
    public string Signature => $"{Name}|{ControlType}|{AutomationId}";
}
