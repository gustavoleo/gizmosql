using CaptureRunner.Models;
using CaptureRunner.Services;
using FlaUI.Core.AutomationElements;

namespace CaptureRunner.Runner;

public sealed class Validator
{
    private readonly TreeService _treeService;

    public Validator(TreeService treeService)
    {
        _treeService = treeService;
    }

    public ValidationResult Validate(Window? window, ScreenPlan plan, UiaDetectionResult detection)
    {
        var titleMatch = MatchesWindowTitle(window, plan);
        var selectedNavigationMatch = window is not null
            && _treeService.HasSelectedNavigationMatch(window, BuildNavigationNamesForValidation(plan));
        var expectedTargetsDefined = TreeService.BuildExpectedNames(plan).Count > 0 || plan.Hints.ExpectedAutomationIds.Count > 0;
        var expectedControlMatch = expectedTargetsDefined && detection.MissingElements.Count == 0;
        var controlMatch = plan.Hints.RequireExpectedControlMatch
            ? (!plan.Hints.RequireSelectedNavigation || selectedNavigationMatch) && expectedControlMatch
            : selectedNavigationMatch || expectedControlMatch;

        if (RequiresWindowEvidence(plan) && !titleMatch && plan.Hints.ExpectedAutomationIds.Count == 0)
        {
            controlMatch = false;
        }

        return new ValidationResult
        {
            TitleMatch = titleMatch,
            SelectedNavigationMatch = selectedNavigationMatch,
            ExpectedControlMatch = expectedControlMatch,
            ControlMatch = controlMatch,
            TargetElementFound = controlMatch || detection.ElementsFound.Count > 0
        };
    }

    public bool QuickMatch(Window? window, ScreenPlan plan)
    {
        if (window is null)
        {
            return false;
        }

        var titleMatch = MatchesWindowTitle(window, plan);
        var selectedNavigationMatch = _treeService.HasSelectedNavigationMatch(window, BuildNavigationNamesForValidation(plan));
        var expectedTargetsDefined = TreeService.BuildExpectedNames(plan).Count > 0 || plan.Hints.ExpectedAutomationIds.Count > 0;

        if (plan.Hints.RequireExpectedControlMatch)
        {
            if (!expectedTargetsDefined)
            {
                return false;
            }

            var expectedTargetMatch = HasExpectedTargetMatch(window, plan);
            if (RequiresWindowEvidence(plan) && !titleMatch && plan.Hints.ExpectedAutomationIds.Count == 0)
            {
                return false;
            }

            return expectedTargetMatch
                   && (!plan.Hints.RequireSelectedNavigation || selectedNavigationMatch);
        }

        if (titleMatch)
        {
            return true;
        }

        if (selectedNavigationMatch && !RequiresWindowEvidence(plan))
        {
            return true;
        }

        if (plan.Hints.RequireSelectedNavigation || !expectedTargetsDefined)
        {
            return false;
        }

        var hasExpectedTargetMatch = HasExpectedTargetMatch(window, plan);
        if (RequiresWindowEvidence(plan))
        {
            return hasExpectedTargetMatch && plan.Hints.ExpectedAutomationIds.Count > 0;
        }

        return hasExpectedTargetMatch;
    }

    public string? SafeWindowTitle(Window? window)
    {
        if (window is null)
        {
            return null;
        }

        try
        {
            return string.IsNullOrWhiteSpace(window.Title) ? window.Name : window.Title;
        }
        catch
        {
            return null;
        }
    }

    private static bool MatchesWindowTitle(Window? window, ScreenPlan plan)
    {
        if (window is null)
        {
            return false;
        }

        var title = string.Empty;
        try
        {
            title = string.IsNullOrWhiteSpace(window.Title) ? window.Name : window.Title;
        }
        catch
        {
            title = string.Empty;
        }

        if (Contains(title, plan.ScreenName))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(plan.Module) && Contains(title, plan.Module);
    }

    private static bool Contains(string? candidate, string value)
    {
        return !string.IsNullOrWhiteSpace(candidate)
               && candidate.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> BuildNavigationNamesForValidation(ScreenPlan plan)
    {
        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(plan.ScreenName))
        {
            names.Add(plan.ScreenName);
        }

        if (!IsWindowLaunchPlan(plan) && plan.Hints.TreePath.Count > 0)
        {
            names.Add(plan.Hints.TreePath[^1]);
        }

        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsWindowLaunchPlan(ScreenPlan plan)
    {
        return plan.Actions.Any(action => action.Kind == PlanActionKind.WaitForWindow);
    }

    private static bool RequiresWindowEvidence(ScreenPlan plan)
    {
        return IsWindowLaunchPlan(plan);
    }

    private bool HasExpectedTargetMatch(Window window, ScreenPlan plan)
    {
        var expectedNames = TreeService.BuildExpectedNames(plan);
        return _treeService.ContainsAllNamedDescendants(window, expectedNames)
               && _treeService.ContainsAllAutomationIdDescendants(window, plan.Hints.ExpectedAutomationIds);
    }
}
