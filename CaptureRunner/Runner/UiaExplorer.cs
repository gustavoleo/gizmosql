using CaptureRunner.Models;
using CaptureRunner.Services;
using FlaUI.Core.AutomationElements;

namespace CaptureRunner.Runner;

public sealed class UiaExplorer
{
    private readonly TreeService _treeService;

    public UiaExplorer(TreeService treeService)
    {
        _treeService = treeService;
    }

    public ExplorationOutcome Explore(Window? window, ScreenPlan plan)
    {
        if (window is null)
        {
            return new ExplorationOutcome
            {
                Detection = new UiaDetectionResult
                {
                    WindowFound = false
                },
                Limitations = new List<string> { "No application window was available for UIA scanning." }
            };
        }

        var firstSnapshot = _treeService.CaptureSnapshot(window);
        Thread.Sleep(500);
        var secondSnapshot = _treeService.CaptureSnapshot(window);

        var expectedNames = TreeService.BuildExpectedNames(plan);
        var elementsFound = _treeService.FindTargetElements(
            secondSnapshot,
            expectedNames,
            plan.Hints.ExpectedAutomationIds,
            plan.Hints.ExpectedControlTypes);
        var missingElements = _treeService.FindMissingTargets(secondSnapshot, expectedNames, plan.Hints.ExpectedAutomationIds);
        var stability = _treeService.CompareSnapshots(firstSnapshot, secondSnapshot);

        var limitations = new List<string>();
        if (secondSnapshot.Elements.Count == 0)
        {
            limitations.Add("UIA tree scan returned no descendants.");
        }

        if (secondSnapshot.MissingAutomationIdCount > 0)
        {
            limitations.Add($"Detected {secondSnapshot.MissingAutomationIdCount} interactive controls without AutomationId.");
        }

        if (!stability.Consistent)
        {
            limitations.Add("Element set changed between repeated scans; controls appear dynamic or timing-sensitive.");
        }

        if (stability.SecondScanCount > stability.FirstScanCount)
        {
            limitations.Add("Second scan contained more elements than the first; delayed UI loading is likely.");
        }

        if (secondSnapshot.PotentialVirtualization)
        {
            limitations.Add("Tree/List/DataGrid controls appear virtualized; not all items may be realized in the UIA tree.");
        }

        if (secondSnapshot.PotentialCustomRendering)
        {
            limitations.Add("Custom-rendered or canvas-like controls were detected; UIA coverage may be incomplete.");
        }

        if (elementsFound.Count == 0)
        {
            limitations.Add("No target controls were found for the requested screen.");
        }

        return new ExplorationOutcome
        {
            Detection = new UiaDetectionResult
            {
                WindowFound = true,
                WindowTitle = SafeWindowTitle(window),
                TotalDescendants = secondSnapshot.Elements.Count,
                ElementsFound = elementsFound,
                CandidateControls = _treeService.ExtractInterestingControls(secondSnapshot)
                    .Take(120)
                    .Select(item => new DetectedElement
                    {
                        Name = item.Name,
                        ControlType = item.ControlType,
                        AutomationId = item.AutomationId
                    })
                    .ToList(),
                MissingElements = missingElements,
                Stability = stability
            },
            Limitations = limitations,
            HasStableTargetControl = elementsFound.Count > 0 && stability.Consistent,
            HasTargetAutomationId = elementsFound.Any(element => !string.IsNullOrWhiteSpace(element.AutomationId))
        };
    }

    private static string? SafeWindowTitle(Window window)
    {
        try
        {
            return string.IsNullOrWhiteSpace(window.Title) ? window.Name : window.Title;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class ExplorationOutcome
{
    public UiaDetectionResult Detection { get; set; } = new();

    public List<string> Limitations { get; set; } = new();

    public bool HasStableTargetControl { get; set; }

    public bool HasTargetAutomationId { get; set; }
}
