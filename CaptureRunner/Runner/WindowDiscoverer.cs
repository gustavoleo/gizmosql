using CaptureRunner.Models;
using CaptureRunner.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace CaptureRunner.Runner;

public sealed class WindowDiscoverer
{
    private readonly WindowService _windowService;
    private readonly TreeService _treeService;

    public WindowDiscoverer(WindowService windowService, TreeService treeService)
    {
        _windowService = windowService;
        _treeService = treeService;
    }

    public WindowDiscoveryReport Discover(AppSession session, UIA3Automation automation, ScannerOptions options)
    {
        var window = _windowService.WaitForWindow(automation, session, null, options.StartupTimeout);
        if (window is null)
        {
            return new WindowDiscoveryReport
            {
                Notes = new List<string> { "No window was found for the target application during discovery." }
            };
        }

        _windowService.TryBringToFront(window);
        Thread.Sleep(750);

        var snapshot = _treeService.CaptureSnapshot(window);
        var report = new WindowDiscoveryReport
        {
            WindowTitle = SafeWindowTitle(window),
            TotalDescendants = snapshot.Elements.Count,
            ControlTypeSummary = _treeService.BuildControlTypeSummary(snapshot),
            LikelyModules = _treeService.ExtractCandidates(snapshot, "TabItem", "MenuItem"),
            LikelyScreens = _treeService.ExtractCandidates(snapshot, "TreeItem", "ListItem", "TabItem"),
            InterestingControls = _treeService.ExtractInterestingControls(snapshot),
            Notes = BuildNotes(snapshot)
        };

        return report;
    }

    private static List<string> BuildNotes(TreeSnapshot snapshot)
    {
        var notes = new List<string>();
        if (snapshot.MissingAutomationIdCount > 0)
        {
            notes.Add($"Detected {snapshot.MissingAutomationIdCount} interactive controls without AutomationId.");
        }

        if (snapshot.PotentialVirtualization)
        {
            notes.Add("Virtualization heuristics triggered; list or tree children may only appear after scrolling or expansion.");
        }

        if (snapshot.PotentialCustomRendering)
        {
            notes.Add("Custom-rendered controls were detected; some screens may not expose complete UIA metadata.");
        }

        if (snapshot.Elements.Count == 0)
        {
            notes.Add("The window contained no UIA descendants.");
        }

        return notes;
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
