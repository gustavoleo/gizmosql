using System.Diagnostics;
using CaptureRunner.Models;
using CaptureRunner.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace CaptureRunner.Runner;

public sealed class ScreenScanner
{
    private readonly WindowService _windowService;
    private readonly UiaExplorer _explorer;
    private readonly NavigationEngine _navigationEngine;
    private readonly Validator _validator;
    private readonly SnagitCaptureService _snagitCaptureService;
    private readonly NativeWindowCaptureService _nativeWindowCaptureService;
    private readonly WindowPlacementService _windowPlacementService;

    public ScreenScanner(
        WindowService windowService,
        UiaExplorer explorer,
        NavigationEngine navigationEngine,
        Validator validator,
        SnagitCaptureService snagitCaptureService,
        NativeWindowCaptureService nativeWindowCaptureService,
        WindowPlacementService windowPlacementService)
    {
        _windowService = windowService;
        _explorer = explorer;
        _navigationEngine = navigationEngine;
        _validator = validator;
        _snagitCaptureService = snagitCaptureService;
        _nativeWindowCaptureService = nativeWindowCaptureService;
        _windowPlacementService = windowPlacementService;
    }

    public ScreenScanResult Scan(AppSession session, UIA3Automation automation, ScreenPlan plan, ScannerOptions options)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var result = new ScreenScanResult
        {
            RowId = plan.RowId,
            ScreenName = plan.ScreenName,
            Module = plan.Module
        };

        string? fallbackUsed = null;
        var retryCount = 0;

        try
        {
            var mainWindow = _windowService.WaitForWindow(automation, session, null, options.StartupTimeout);
            _windowService.TryBringToFront(mainWindow);
            result.Placement = _windowPlacementService.Place(mainWindow, options);

            var navigationStopwatch = Stopwatch.StartNew();
            var openSuccess = false;
            var methodUsed = "none";
            var activeWindow = mainWindow;

            if (mainWindow is not null && _validator.QuickMatch(mainWindow, plan))
            {
                openSuccess = true;
                methodUsed = "already_open";
            }
            else if (mainWindow is not null)
            {
                var methods = _navigationEngine.GetMethods(plan);
                for (var index = 0; index < methods.Count; index++)
                {
                    var method = methods[index];
                    if (index > 0)
                    {
                        retryCount++;
                    }

                    var attempt = _navigationEngine.TryNavigate(session, automation, mainWindow, plan, method);
                    activeWindow = attempt.ActiveWindow
                                   ?? _windowService.WaitForWindow(automation, session, plan.ScreenName, options.NavigationTimeout)
                                   ?? _windowService.WaitForWindow(automation, session, null, TimeSpan.FromSeconds(1))
                                   ?? mainWindow;

                    if (!attempt.Success)
                    {
                        continue;
                    }

                    methodUsed = NavigationEngine.ToReportMethod(method);
                    if (method is not NavigationMethod.TreeNavigation)
                    {
                        fallbackUsed = methodUsed;
                    }

                    if (_validator.QuickMatch(activeWindow, plan))
                    {
                        openSuccess = true;
                        break;
                    }
                }
            }

            navigationStopwatch.Stop();

            result.OpenResult = new OpenResult
            {
                Success = openSuccess,
                MethodUsed = methodUsed,
                DurationMs = navigationStopwatch.ElapsedMilliseconds
            };

            var exploration = _explorer.Explore(activeWindow, plan);
            result.UiaDetection = exploration.Detection;
            result.Validation = _validator.Validate(activeWindow, plan, exploration.Detection);
            result.Classification = Classify(result.OpenResult, result.UiaDetection, result.Validation, exploration);
            result.Limitations = Deduplicate(exploration.Limitations);
            result.Recommendation = BuildRecommendation(result.OpenResult.MethodUsed, result.Classification.Automatable, result.Limitations);
            result.Capture = openSuccess
                ? Capture(activeWindow, plan, options)
                : new CaptureResult
                {
                    Enabled = options.IsCaptureEnabled(plan),
                    Attempted = false,
                    Success = false,
                    Method = options.CaptureBackend == CaptureBackend.Native ? "native_window_png" : options.IsSnagitCaptureEnabled(plan) ? "snagit_hotkey" : "none",
                    Note = options.IsCaptureEnabled(plan)
                        ? "Capture skipped because the screen did not open successfully."
                        : "Capture is disabled."
                };
            result.Diagnostics = new DiagnosticsResult
            {
                DurationMs = overallStopwatch.ElapsedMilliseconds,
                FallbackUsed = fallbackUsed,
                RetryCount = retryCount
            };
        }
        catch (Exception ex)
        {
            result.OpenResult = new OpenResult
            {
                Success = false,
                MethodUsed = "none",
                DurationMs = result.OpenResult.DurationMs
            };
            result.UiaDetection ??= new UiaDetectionResult
            {
                WindowFound = false
            };
            result.Validation ??= new ValidationResult();
            result.Classification = new ClassificationResult
            {
                Automatable = "none",
                Confidence = "low"
            };
            result.Limitations = Deduplicate(result.Limitations.Append($"Unhandled exception during scan: {ex.Message}"));
            result.Recommendation = "Retry after the application is stable, or inspect the failure details in diagnostics.";
            result.Capture ??= new CaptureResult
            {
                Enabled = options.IsCaptureEnabled(plan),
                Attempted = false,
                Success = false,
                Method = options.CaptureBackend == CaptureBackend.Native ? "native_window_png" : options.IsSnagitCaptureEnabled(plan) ? "snagit_hotkey" : "none",
                Note = options.IsCaptureEnabled(plan)
                    ? "Capture skipped because the scan failed before capture could start."
                    : "Capture is disabled."
            };
            result.Diagnostics = new DiagnosticsResult
            {
                DurationMs = overallStopwatch.ElapsedMilliseconds,
                FallbackUsed = fallbackUsed,
                RetryCount = retryCount,
                Exception = ex.ToString()
            };
        }
        finally
        {
            overallStopwatch.Stop();
            result.Diagnostics.DurationMs = overallStopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private CaptureResult Capture(Window? activeWindow, ScreenPlan plan, ScannerOptions options)
    {
        return options.CaptureBackend switch
        {
            CaptureBackend.Native => _nativeWindowCaptureService.Capture(activeWindow, plan, options),
            _ => _snagitCaptureService.Capture(activeWindow, plan, options)
        };
    }

    private static ClassificationResult Classify(
        OpenResult openResult,
        UiaDetectionResult detection,
        ValidationResult validation,
        ExplorationOutcome exploration)
    {
        var validationPassed = validation.TitleMatch || validation.ControlMatch;

        if (detection.WindowFound
            && openResult.Success
            && exploration.HasStableTargetControl
            && exploration.HasTargetAutomationId
            && validationPassed)
        {
            return new ClassificationResult
            {
                Automatable = "full",
                Confidence = "high"
            };
        }

        if (openResult.Success || detection.ElementsFound.Count > 0 || validationPassed)
        {
            return new ClassificationResult
            {
                Automatable = "partial",
                Confidence = openResult.Success ? "medium" : "low"
            };
        }

        return new ClassificationResult
        {
            Automatable = "none",
            Confidence = "low"
        };
    }

    private static string BuildRecommendation(string methodUsed, string classification, IEnumerable<string> limitations)
    {
        var limitationList = limitations.ToList();

        if (classification == "full")
        {
            return methodUsed switch
            {
                "tree_navigation" => "Use UIA tree navigation with element re-validation after the screen opens.",
                "keyboard" => "Use the keyboard shortcut to activate the screen, then use UIA lookups on stable controls.",
                "focus_change" => "Use focus-based navigation sparingly; the screen is reachable but less explicit than tree or shortcut navigation.",
                _ => "Use the current UIA anchors directly; the screen already exposes stable controls."
            };
        }

        if (classification == "partial")
        {
            if (limitationList.Any(item => item.Contains("AutomationId", StringComparison.OrdinalIgnoreCase)))
            {
                return "Navigation is possible, but adding stable AutomationIds would make the flow reliable enough for production automation.";
            }

            if (limitationList.Any(item => item.Contains("delayed", StringComparison.OrdinalIgnoreCase)
                                           || item.Contains("timing", StringComparison.OrdinalIgnoreCase)))
            {
                return "Use the current navigation path with explicit waits and repeated UIA probes before taking action.";
            }

            return "Use the discovered path as a fallback automation route and tighten the app's accessibility surface before scaling it.";
        }

        if (limitationList.Any(item => item.Contains("custom-rendered", StringComparison.OrdinalIgnoreCase)))
        {
            return "This screen likely needs product-side accessibility changes or a non-UIA fallback because the important controls are custom-rendered.";
        }

        return "UIA does not expose enough stable state for this screen. Prefer product instrumentation or accessibility improvements before automating it.";
    }

    private static List<string> Deduplicate(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
