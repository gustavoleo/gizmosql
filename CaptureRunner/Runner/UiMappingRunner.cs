using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using CaptureRunner.Models;
using CaptureRunner.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace CaptureRunner.Runner;

public sealed class UiMappingRunner
{
    private readonly WindowService _windowService;
    private readonly UiInteractionExtractor _interactionExtractor;
    private readonly NativeWindowCaptureService _nativeWindowCaptureService;
    private readonly PngEvidenceValidator _pngEvidenceValidator;

    public UiMappingRunner(
        WindowService windowService,
        UiInteractionExtractor interactionExtractor,
        NativeWindowCaptureService nativeWindowCaptureService,
        PngEvidenceValidator pngEvidenceValidator)
    {
        _windowService = windowService;
        _interactionExtractor = interactionExtractor;
        _nativeWindowCaptureService = nativeWindowCaptureService;
        _pngEvidenceValidator = pngEvidenceValidator;
    }

    public UiMap Run(
        AppSession session,
        UIA3Automation automation,
        ScannerOptions options,
        StartupStateReport startupState,
        JsonSerializerOptions jsonOptions)
    {
        var repositoryName = string.IsNullOrWhiteSpace(startupState.RepositoryName)
            ? options.RepositoryName ?? "Northwind"
            : startupState.RepositoryName;
        var map = new UiMap
        {
            RepositoryName = repositoryName,
            AcceptedThreshold = options.AcceptedThreshold,
            StartupStateOutput = options.StartupStateOutputPath
        };
        var coverageNotes = new List<string>();

        if (!startupState.RepositoryVerified)
        {
            map.Screens.Add(CreateBlockedBootstrapScreen(startupState));
            map.Coverage = BuildCoverage(map.Screens, options.AcceptedThreshold, coverageNotes);
            return map;
        }

        var mainWindow = _windowService.WaitForWindow(
                             automation,
                             session,
                             repositoryName,
                             options.StartupTimeout)
                         ?? _windowService.WaitForWindow(automation, session, null, TimeSpan.FromSeconds(2));
        if (mainWindow is null)
        {
            map.Screens.Add(CreateBlockedBootstrapScreen(startupState, "No target window was available after bootstrap verification."));
            map.Coverage = BuildCoverage(map.Screens, options.AcceptedThreshold, coverageNotes);
            return map;
        }

        _windowService.TryBringToFront(mainWindow);
        Thread.Sleep(500);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queuedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new UiTraversalQueue();
        var currentScreen = CaptureScreen(
            mainWindow,
            BuildCurrentRoute(),
            "current-shell",
            repositoryName,
            options,
            jsonOptions);
        if (!AddIfNew(map.Screens, currentScreen, visited))
        {
            TryDeleteEvidenceBundle(currentScreen);
        }

        var shellRouteInteractionKeys = currentScreen.AvailableInteractions
            .Where(IsSafeRouteCandidate)
            .SelectMany(BuildShellRouteKeys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        EnqueueConfiguredRoutes(queue, queuedRoutes, options.RouteRecipes, options);
        EnqueueNextRoutes(queue, queuedRoutes, shellRouteInteractionKeys, currentScreen, depth: 0, options);

        var traversalDeadline = DateTime.UtcNow + options.MapTraversalTimeout;
        while (queue.Count > 0
               && map.Screens.Count < options.MapMaxScreens
               && DateTime.UtcNow < traversalDeadline)
        {
            var node = queue.Dequeue();
            Console.WriteLine($"Mapping route depth {node.Depth}: {node.Route.RouteText}");
            var resetWindow = TryResetToShell(session, automation, repositoryName, options);
            if (resetWindow is null)
            {
                AddIfNew(
                    map.Screens,
                    CreateRejectedRoute(node.Route, "Unable to reset to a verified Northwind shell before replaying the route.", repositoryName),
                    visited);
                continue;
            }

            if (!TryReplayRoute(session, automation, node.Route, repositoryName, options, out var activeWindow, out var replayNote))
            {
                AddIfNew(
                    map.Screens,
                    CreateRejectedRoute(node.Route, replayNote, repositoryName),
                    visited);
                continue;
            }

            activeWindow ??= _windowService.WaitForWindow(automation, session, null, TimeSpan.FromSeconds(2)) ?? resetWindow;
            var screen = CaptureScreen(
                activeWindow,
                node.Route,
                BuildScreenIdSeed(node.Route),
                repositoryName,
                options,
                jsonOptions);

            if (AddIfNew(map.Screens, screen, visited))
            {
                EnqueueNextRoutes(queue, queuedRoutes, shellRouteInteractionKeys, screen, node.Depth, options);
            }
            else
            {
                TryDeleteEvidenceBundle(screen);
            }

            var cleanupNote = TryCloseCapturedSurface(activeWindow, screen);
            if (!string.IsNullOrWhiteSpace(cleanupNote))
            {
                screen.Readiness.Reasons.Add(cleanupNote);
                Console.WriteLine(cleanupNote);
            }

            TryCloseUnexpectedModal(activeWindow, repositoryName);
        }

        if (queue.Count > 0 && DateTime.UtcNow >= traversalDeadline && map.Screens.Count < options.MapMaxScreens)
        {
            coverageNotes.Add($"UI traversal stopped after {options.MapTraversalTimeout.TotalSeconds:0} seconds with {queue.Count} queued routes remaining.");
        }
        else if (queue.Count > 0 && map.Screens.Count >= options.MapMaxScreens)
        {
            coverageNotes.Add($"UI traversal stopped at the configured screen cap of {options.MapMaxScreens} with {queue.Count} queued routes remaining.");
        }

        map.RemainingQueuedRoutes = queue.SnapshotRoutes();
        map.Coverage = BuildCoverage(map.Screens, options.AcceptedThreshold, coverageNotes);
        return map;
    }

    private UiScreen CaptureScreen(
        Window window,
        UiRoute route,
        string screenIdSeed,
        string repositoryName,
        ScannerOptions options,
        JsonSerializerOptions jsonOptions)
    {
        var interactions = _interactionExtractor.Extract(window);
        var title = SafeWindowTitle(window);
        var signature = BuildSignature(title, interactions);
        var screen = new UiScreen
        {
            ScreenId = Slug($"{screenIdSeed}-{title}-{signature}"),
            ScreenName = string.IsNullOrWhiteSpace(route.RouteText) ? title ?? screenIdSeed : route.RouteText,
            Module = InferModule(route, interactions),
            Family = InferFamily(route),
            WindowTitle = title,
            Signature = signature,
            Route = route,
            ValidationAnchors = BuildValidationAnchors(title, repositoryName, interactions),
            AvailableInteractions = interactions,
            ScreenshotStrategy = new UiScreenshotStrategy
            {
                Backend = "native",
                Area = options.NativeCaptureArea.ToString().ToLowerInvariant(),
                Dpi = options.NativePngDpi ?? 600
            }
        };

        var validationPassed = ValidateScreen(screen, repositoryName);
        var bucket = validationPassed ? "accepted" : "review";
        var bundleDir = Path.Combine(options.ScreenshotOutputRoot, bucket, screen.ScreenId);
        Directory.CreateDirectory(bundleDir);

        var originalCaptureDirectory = options.CaptureOutputDirectory;
        options.CaptureOutputDirectory = bundleDir;
        try
        {
            var capturePlan = ToCapturePlan(screen);
            var capture = _nativeWindowCaptureService.Capture(window, capturePlan, options);
            var pngValidation = _pngEvidenceValidator.Validate(capture.OutputFile, options.NativePngDpi ?? 600);
            if (!capture.Success || !pngValidation.Valid)
            {
                bucket = capture.Attempted ? "rejected" : "blocked";
                screen.Readiness.Status = bucket;
                screen.Readiness.Confidence = "low";
                screen.Readiness.Reasons.Add(capture.Note ?? pngValidation.Note ?? "Native PNG evidence failed.");
            }
            else if (validationPassed)
            {
                screen.Readiness.Status = "accepted";
                screen.Readiness.Confidence = "high";
                screen.Readiness.Reasons.Add("Bootstrap, validation anchors, and native PNG evidence passed.");
                screen.LastVerifiedUtc = DateTimeOffset.UtcNow;
            }
            else
            {
                screen.Readiness.Status = "review";
                screen.Readiness.Confidence = "medium";
                screen.Readiness.Reasons.Add("Native PNG exists, but validation anchors are incomplete.");
            }

            screen.ScreenshotStrategy.Bucket = screen.Readiness.Status;
            screen.ScreenshotStrategy.OutputFile = capture.OutputFile;
            screen.Evidence.ScreenshotPng = capture.OutputFile;

            WriteEvidenceBundle(bundleDir, screen, capture, pngValidation, jsonOptions);
        }
        finally
        {
            options.CaptureOutputDirectory = originalCaptureDirectory;
        }

        return screen;
    }

    private static void WriteEvidenceBundle(
        string bundleDir,
        UiScreen screen,
        CaptureResult capture,
        PngValidationResult pngValidation,
        JsonSerializerOptions jsonOptions)
    {
        var routePath = Path.Combine(bundleDir, "route.json");
        var snapshotPath = Path.Combine(bundleDir, "uia-snapshot.json");
        var reportPath = Path.Combine(bundleDir, "report.json");

        File.WriteAllText(routePath, JsonSerializer.Serialize(screen.Route, jsonOptions));
        File.WriteAllText(snapshotPath, JsonSerializer.Serialize(screen.AvailableInteractions, jsonOptions));

        var report = new UiScreenEvidenceReport
        {
            ScreenId = screen.ScreenId,
            Route = screen.Route,
            ValidationPassed = screen.Readiness.Status == "accepted",
            Capture = capture,
            PngValidation = pngValidation,
            Readiness = screen.Readiness
        };
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, jsonOptions));

        screen.Evidence.RouteJson = routePath;
        screen.Evidence.SnapshotJson = snapshotPath;
        screen.Evidence.ReportJson = reportPath;
        screen.Evidence.FailureNote = screen.Readiness.Status == "accepted"
            ? null
            : string.Join(" ", screen.Readiness.Reasons);
    }

    private static ScreenPlan ToCapturePlan(UiScreen screen)
    {
        return new ScreenPlan
        {
            RowId = screen.ScreenId,
            ScreenName = screen.ScreenName,
            Module = screen.Module,
            Hints =
            {
                ExpectedControls = screen.ValidationAnchors
                    .Where(anchor => anchor.AnchorType is "control_name")
                    .Select(anchor => anchor.Value)
                    .Take(5)
                    .ToList(),
                ExpectedAutomationIds = screen.ValidationAnchors
                    .Where(anchor => anchor.AnchorType is "automation_id")
                    .Select(anchor => anchor.Value)
                    .Take(5)
                    .ToList()
            }
        };
    }

    private static UiScreen CreateBlockedBootstrapScreen(StartupStateReport startupState, string? reason = null)
    {
        return new UiScreen
        {
            ScreenId = "bootstrap-blocked",
            ScreenName = "Bootstrap",
            Family = "startup",
            WindowTitle = startupState.ActiveWindowTitle,
            Signature = "bootstrap-blocked",
            RequiredState = startupState.RepositoryName,
            Readiness = new UiReadiness
            {
                Status = "blocked",
                Confidence = "low",
                BlockerType = "application-state",
                Reasons =
                {
                    reason ?? "Northwind repository was not verified; UI mapping was not attempted."
                }
            }
        };
    }

    private static UiScreen CreateRejectedCandidate(UiInteraction candidate, string reason, string repositoryName)
    {
        return CreateRejectedRoute(BuildCandidateRoute(candidate), reason, repositoryName);
    }

    private static UiScreen CreateRejectedRoute(UiRoute route, string reason, string repositoryName)
    {
        return new UiScreen
        {
            ScreenId = Slug($"rejected-{BuildRouteSignature(route)}"),
            ScreenName = route.RouteText,
            Family = "navigation-candidate",
            RequiredState = repositoryName,
            Signature = BuildRouteSignature(route),
            Route = route,
            Readiness = new UiReadiness
            {
                Status = "rejected",
                Confidence = "low",
                BlockerType = "source-code",
                Reasons = { reason }
            }
        };
    }

    private static UiRoute BuildCurrentRoute()
    {
        return new UiRoute
        {
            RouteType = "current",
            RouteText = "Current Northwind shell",
            Steps =
            {
                new UiRouteStep
                {
                    Kind = "bootstrap",
                    Name = "Northwind",
                    Action = "verify"
                }
            }
        };
    }

    private static UiRoute BuildCandidateRoute(UiInteraction interaction)
    {
        return new UiRoute
        {
            RouteType = interaction.Kind,
            RouteText = interaction.Name ?? interaction.AutomationId ?? interaction.InteractionId,
            Steps =
            {
                new UiRouteStep
                {
                    Kind = interaction.ControlType,
                    Name = interaction.Name,
                    AutomationId = interaction.AutomationId,
                    ControlType = interaction.ControlType,
                    Action = ResolveAction(interaction)
                }
            }
        };
    }

    private static UiRoute ExtendRoute(UiRoute route, UiInteraction interaction)
    {
        var step = ToRouteStep(interaction);
        var extended = new UiRoute
        {
            RouteType = interaction.Kind,
            RouteText = BuildExtendedRouteText(route, step),
            Steps = route.Steps
                .Select(CloneRouteStep)
                .Append(step)
                .ToList()
        };

        return extended;
    }

    private static UiRouteStep ToRouteStep(UiInteraction interaction)
    {
        return new UiRouteStep
        {
            Kind = interaction.ControlType,
            Name = interaction.Name,
            AutomationId = interaction.AutomationId,
            ControlType = interaction.ControlType,
            Action = ResolveAction(interaction)
        };
    }

    private static UiRouteStep CloneRouteStep(UiRouteStep step)
    {
        return new UiRouteStep
        {
            Kind = step.Kind,
            Name = step.Name,
            AutomationId = step.AutomationId,
            ControlType = step.ControlType,
            Action = step.Action
        };
    }

    private static string BuildExtendedRouteText(UiRoute route, UiRouteStep step)
    {
        var label = step.Name ?? step.AutomationId ?? step.ControlType ?? "Unnamed";
        if (route.RouteType == "current" || route.Steps.All(IsBootstrapStep))
        {
            return label;
        }

        return $"{route.RouteText} > {label}";
    }

    private static string ResolveAction(UiInteraction interaction)
    {
        if (interaction.SupportedPatterns.Contains("SelectionItem", StringComparer.OrdinalIgnoreCase))
        {
            return "select";
        }

        if (interaction.SupportedPatterns.Contains("ExpandCollapse", StringComparer.OrdinalIgnoreCase))
        {
            return "expand";
        }

        if (interaction.SupportedPatterns.Contains("Invoke", StringComparer.OrdinalIgnoreCase))
        {
            return "invoke";
        }

        return "focus_enter";
    }

    private static void EnqueueNextRoutes(
        UiTraversalQueue queue,
        HashSet<string> queuedRoutes,
        HashSet<string> shellRouteInteractionKeys,
        UiScreen screen,
        int depth,
        ScannerOptions options)
    {
        if (depth >= options.MapMaxDepth || !CanExpandScreen(screen))
        {
            return;
        }

        var candidates = screen.AvailableInteractions.Where(IsSafeRouteCandidate);
        if (depth > 0)
        {
            candidates = candidates.Where(interaction => !IsShellRouteCandidate(interaction, shellRouteInteractionKeys));
        }

        foreach (var interaction in candidates.Take(options.MapBranchingFactor))
        {
            if (RouteContainsStep(screen.Route, interaction))
            {
                continue;
            }

            var route = ResolveConfiguredRoute(ExtendRoute(screen.Route, interaction), options.RouteRecipes);
            var routeSignature = BuildRouteSignature(route);
            if (queuedRoutes.Add(routeSignature))
            {
                queue.Enqueue(new UiTraversalNode(route, depth + 1));
            }
        }
    }

    private static void EnqueueConfiguredRoutes(
        UiTraversalQueue queue,
        HashSet<string> queuedRoutes,
        IEnumerable<UiRoute> routes,
        ScannerOptions options)
    {
        foreach (var route in routes)
        {
            var depth = GetRouteDepth(route);
            if (depth == 0 || depth > options.MapMaxDepth || !IsConfiguredRouteSafe(route))
            {
                continue;
            }

            var routeSignature = BuildRouteSignature(route);
            if (string.IsNullOrWhiteSpace(routeSignature) || !queuedRoutes.Add(routeSignature))
            {
                continue;
            }

            queue.Enqueue(new UiTraversalNode(route, depth));
        }
    }

    private static int GetRouteDepth(UiRoute route)
    {
        return route.Steps.Count(step => !IsBootstrapStep(step));
    }

    private static bool IsConfiguredRouteSafe(UiRoute route)
    {
        return route.Steps
            .Where(step => !IsBootstrapStep(step))
            .All(step => step.Action is "select" or "expand" or "invoke" or "focus_enter");
    }

    private static UiRoute ResolveConfiguredRoute(UiRoute route, IEnumerable<UiRoute> configuredRoutes)
    {
        var routeDepth = GetRouteDepth(route);
        if (routeDepth != 1)
        {
            return route;
        }

        var routeLeaf = route.Steps.LastOrDefault(step => !IsBootstrapStep(step));
        if (routeLeaf is null)
        {
            return route;
        }

        return configuredRoutes
                   .Where(candidate => GetRouteDepth(candidate) > routeDepth)
                   .OrderByDescending(GetRouteDepth)
                   .FirstOrDefault(candidate => StepsMatch(candidate.Steps.LastOrDefault(step => !IsBootstrapStep(step)), routeLeaf))
               ?? route;
    }

    private static bool StepsMatch(UiRouteStep? left, UiRouteStep? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(left.Name ?? string.Empty, right.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && string.Equals(
                   left.ControlType ?? left.Kind ?? string.Empty,
                   right.ControlType ?? right.Kind ?? string.Empty,
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.Action ?? string.Empty, right.Action ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> BuildShellRouteKeys(UiInteraction interaction)
    {
        yield return interaction.InteractionId;

        var nameKey = BuildInteractionNameKey(interaction);
        if (!string.IsNullOrWhiteSpace(nameKey))
        {
            yield return nameKey;
        }
    }

    private static bool IsShellRouteCandidate(UiInteraction interaction, HashSet<string> shellRouteInteractionKeys)
    {
        return shellRouteInteractionKeys.Contains(interaction.InteractionId)
               || shellRouteInteractionKeys.Contains(BuildInteractionNameKey(interaction));
    }

    private static string BuildInteractionNameKey(UiInteraction interaction)
    {
        return string.IsNullOrWhiteSpace(interaction.Name)
            ? string.Empty
            : $"name:{interaction.Name.Trim().ToLowerInvariant()}";
    }

    private static bool CanExpandScreen(UiScreen screen)
    {
        return screen.Readiness.Status == "accepted"
               && screen.Family is not "dialog";
    }

    private static bool RouteContainsStep(UiRoute route, UiInteraction interaction)
    {
        return route.Steps.Any(step =>
            !IsBootstrapStep(step)
            && string.Equals(step.ControlType, interaction.ControlType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(step.AutomationId ?? string.Empty, interaction.AutomationId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && string.Equals(step.Name ?? string.Empty, interaction.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryReplayRoute(
        AppSession session,
        UIA3Automation automation,
        UiRoute route,
        string repositoryName,
        ScannerOptions options,
        out Window? activeWindow,
        out string note)
    {
        activeWindow = null;
        var notes = new List<string>();
        foreach (var step in route.Steps.Where(step => !IsBootstrapStep(step)))
        {
            var window = _windowService.WaitForWindow(automation, session, repositoryName, options.NavigationTimeout)
                         ?? _windowService.WaitForWindow(automation, session, null, TimeSpan.FromSeconds(1));
            if (window is null)
            {
                note = $"Unable to find an AnalyticsCreator window before route step '{step.Name ?? step.AutomationId}'.";
                return false;
            }

            _windowService.TryBringToFront(window);
            if (!TryActivateStep(window, step, out var stepNote))
            {
                note = stepNote;
                return false;
            }

            notes.Add(stepNote);
            Thread.Sleep(750);
            activeWindow = _windowService.WaitForWindow(automation, session, null, TimeSpan.FromSeconds(1)) ?? window;
        }

        note = string.Join(" ", notes);
        return true;
    }

    private Window? TryResetToShell(
        AppSession session,
        UIA3Automation automation,
        string repositoryName,
        ScannerOptions options)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var repositoryWindow = _windowService.WaitForWindow(automation, session, repositoryName, TimeSpan.FromSeconds(1));
            if (repositoryWindow is not null)
            {
                _windowService.TryBringToFront(repositoryWindow);
                TryInvokeHome(repositoryWindow);
                Thread.Sleep(500);
                return _windowService.WaitForWindow(automation, session, repositoryName, options.NavigationTimeout) ?? repositoryWindow;
            }

            var activeWindow = _windowService.WaitForWindow(automation, session, null, TimeSpan.FromSeconds(1));
            if (activeWindow is not null)
            {
                TryCloseUnexpectedModal(activeWindow, repositoryName);
            }
        }

        return null;
    }

    private static bool TryActivateStep(Window window, UiRouteStep step, out string note)
    {
        var candidate = new UiInteraction
        {
            Name = step.Name,
            AutomationId = step.AutomationId,
            ControlType = step.ControlType ?? step.Kind,
            IsEnabled = true,
            Risk = "safe",
            Readiness = "ready",
            RouteCandidate = true
        };

        return TryActivate(window, candidate, out note);
    }

    private static string? TryCloseCapturedSurface(Window activeWindow, UiScreen screen)
    {
        if (!RequiresExplicitClose(screen))
        {
            return null;
        }

        if (TryDismissWindow(activeWindow, out var note))
        {
            return $"Closed '{screen.Route.RouteText}' after capture. {note}";
        }

        return $"Unable to close '{screen.Route.RouteText}' after capture. {note}";
    }

    private static bool RequiresExplicitClose(UiScreen screen)
    {
        return screen.Family is "dialog" or "detail" or "wizard";
    }

    private static bool TryInvokeHome(Window window)
    {
        AutomationElement? home;
        try
        {
            home = window
                .FindAllDescendants()
                .FirstOrDefault(element =>
                    string.Equals(SafeAutomationId(element), "btnHome", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(SafeName(element), "Home", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }

        if (home is null)
        {
            return false;
        }

        try
        {
            if (home.Patterns.Invoke.TryGetPattern(out var invokePattern))
            {
                invokePattern.Invoke();
                return true;
            }
        }
        catch
        {
            // Fall through to focus/Enter reset.
        }

        try
        {
            home.Focus();
            SendKeys.SendWait("{ENTER}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryActivate(Window window, UiInteraction candidate, out string note)
    {
        note = string.Empty;
        if (!IsSafeRouteCandidate(candidate))
        {
            note = $"Candidate '{candidate.Name ?? candidate.AutomationId}' is not safe for automatic traversal.";
            return false;
        }

        var element = FindCandidate(window, candidate);
        if (element is null)
        {
            note = $"Candidate '{candidate.Name ?? candidate.AutomationId}' was not found in the active UIA tree.";
            return false;
        }

        try
        {
            if (element.Patterns.SelectionItem.TryGetPattern(out var selectionPattern))
            {
                selectionPattern.Select();
                note = $"Selected '{candidate.Name ?? candidate.AutomationId}' with SelectionItemPattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to other patterns.
        }

        try
        {
            if (element.Patterns.ExpandCollapse.TryGetPattern(out var expandPattern))
            {
                expandPattern.Expand();
                note = $"Expanded '{candidate.Name ?? candidate.AutomationId}' with ExpandCollapsePattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to invoke/focus.
        }

        try
        {
            if (element.Patterns.Invoke.TryGetPattern(out var invokePattern))
            {
                invokePattern.Invoke();
                note = $"Invoked '{candidate.Name ?? candidate.AutomationId}' with InvokePattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to active-window scoped keyboard fallback.
        }

        try
        {
            element.Focus();
            SendKeys.SendWait("{ENTER}");
            note = $"Activated '{candidate.Name ?? candidate.AutomationId}' with scoped Enter fallback.";
            return true;
        }
        catch (Exception ex)
        {
            note = $"Unable to activate '{candidate.Name ?? candidate.AutomationId}': {ex.Message}";
            return false;
        }
    }

    private static bool TryDismissWindow(Window window, out string note)
    {
        foreach (var candidate in FindDismissalCandidates(window))
        {
            var label = SafeName(candidate) ?? SafeAutomationId(candidate) ?? "active-surface";
            if (TryActivateElement(candidate, label, out note))
            {
                Thread.Sleep(500);
                return true;
            }
        }

        try
        {
            window.Focus();
            SendKeys.SendWait("{ESC}");
            Thread.Sleep(250);
            note = "Dismissed the active surface with scoped Esc fallback.";
            return true;
        }
        catch (Exception ex)
        {
            note = $"Unable to dismiss the active surface: {ex.Message}";
            return false;
        }
    }

    private static List<AutomationElement> FindDismissalCandidates(Window window)
    {
        try
        {
            return window
                .FindAllDescendants()
                .Where(IsDismissalCandidate)
                .OrderBy(GetDismissalPriority)
                .ThenBy(element => SafeName(element), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<AutomationElement>();
        }
    }

    private static bool IsDismissalCandidate(AutomationElement element)
    {
        var name = SafeName(element);
        if (!IsDismissalName(name))
        {
            return false;
        }

        if (SafeClassName(element)?.Contains("TitleBar", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return false;
        }

        var controlType = SafeControlType(element);
        if (controlType is not "Button" and not "MenuItem" and not "Hyperlink")
        {
            return false;
        }

        return IsEnabled(element);
    }

    private static bool IsDismissalName(string? name)
    {
        return name is "Cancel" or "Close" or "Back";
    }

    private static int GetDismissalPriority(AutomationElement element)
    {
        return SafeName(element) switch
        {
            "Cancel" => 0,
            "Close" => 1,
            "Back" => 2,
            _ => 10
        };
    }

    private static bool TryActivateElement(AutomationElement element, string label, out string note)
    {
        try
        {
            if (element.Patterns.SelectionItem.TryGetPattern(out var selectionPattern))
            {
                selectionPattern.Select();
                note = $"Selected '{label}' with SelectionItemPattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to other patterns.
        }

        try
        {
            if (element.Patterns.ExpandCollapse.TryGetPattern(out var expandPattern))
            {
                expandPattern.Collapse();
                note = $"Collapsed '{label}' with ExpandCollapsePattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to invoke/focus.
        }

        try
        {
            if (element.Patterns.Invoke.TryGetPattern(out var invokePattern))
            {
                invokePattern.Invoke();
                note = $"Invoked '{label}' with InvokePattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to active-window scoped keyboard fallback.
        }

        try
        {
            element.Focus();
            SendKeys.SendWait("{ENTER}");
            note = $"Activated '{label}' with scoped Enter fallback.";
            return true;
        }
        catch (Exception ex)
        {
            note = $"Unable to activate '{label}': {ex.Message}";
            return false;
        }
    }

    private static AutomationElement? FindCandidate(Window window, UiInteraction candidate)
    {
        try
        {
            var descendants = window
                .FindAllDescendants()
                .ToList();
            var exact = descendants.FirstOrDefault(element =>
                string.Equals(SafeControlType(element), candidate.ControlType, StringComparison.OrdinalIgnoreCase)
                && MatchesCandidateIdentity(element, candidate));
            if (exact is not null)
            {
                return exact;
            }

            if (string.IsNullOrWhiteSpace(candidate.AutomationId) && !string.IsNullOrWhiteSpace(candidate.Name))
            {
                return descendants.FirstOrDefault(element =>
                    IsReplayFallbackControlType(SafeControlType(element))
                    && string.Equals(SafeName(element), candidate.Name, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsReplayFallbackControlType(string controlType)
    {
        return controlType is "Button" or "MenuItem" or "TabItem" or "TreeItem" or "Hyperlink";
    }

    private static bool MatchesCandidateIdentity(AutomationElement element, UiInteraction candidate)
    {
        var automationId = SafeAutomationId(element);
        if (!string.IsNullOrWhiteSpace(candidate.AutomationId)
            && string.Equals(automationId, candidate.AutomationId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var name = SafeName(element);
        return !string.IsNullOrWhiteSpace(candidate.Name)
               && string.Equals(name, candidate.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AddIfNew(List<UiScreen> screens, UiScreen screen, HashSet<string> visited)
    {
        if (visited.Add(screen.Signature))
        {
            screens.Add(screen);
            return true;
        }

        var existing = screens.FirstOrDefault(candidate =>
            string.Equals(candidate.Signature, screen.Signature, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            AddAlternateRoute(existing, screen.Route);
        }

        return false;
    }

    private static void AddAlternateRoute(UiScreen screen, UiRoute route)
    {
        var routeSignature = BuildRouteSignature(route);
        if (string.IsNullOrWhiteSpace(routeSignature)
            || string.Equals(BuildRouteSignature(screen.Route), routeSignature, StringComparison.OrdinalIgnoreCase)
            || screen.AlternateRoutes.Any(existing =>
                string.Equals(BuildRouteSignature(existing), routeSignature, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        screen.AlternateRoutes.Add(route);
    }

    private static void TryDeleteEvidenceBundle(UiScreen screen)
    {
        try
        {
            var evidencePath = screen.Evidence.ReportJson
                               ?? screen.Evidence.SnapshotJson
                               ?? screen.Evidence.RouteJson
                               ?? screen.Evidence.ScreenshotPng;
            var directory = string.IsNullOrWhiteSpace(evidencePath)
                ? null
                : Path.GetDirectoryName(evidencePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for duplicate discovery evidence.
        }
    }

    private static bool IsSafeRouteCandidate(UiInteraction interaction)
    {
        return interaction.RouteCandidate
               && interaction.IsEnabled
               && interaction.Risk == "safe"
               && interaction.Readiness == "ready";
    }

    private static bool ValidateScreen(UiScreen screen, string repositoryName)
    {
        if (!screen.ValidationAnchors.Any(anchor => anchor.Stable))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(screen.WindowTitle)
            && screen.WindowTitle.Contains(repositoryName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return screen.Family is "dialog" or "detail" or "wizard"
               && !string.IsNullOrWhiteSpace(screen.WindowTitle)
               && screen.Route.Steps.Any(step => !IsBootstrapStep(step));
    }

    private static List<UiValidationAnchor> BuildValidationAnchors(
        string? title,
        string repositoryName,
        IEnumerable<UiInteraction> interactions)
    {
        var anchors = new List<UiValidationAnchor>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            anchors.Add(new UiValidationAnchor
            {
                AnchorType = "window_title_contains",
                Value = repositoryName,
                Stable = title.Contains(repositoryName, StringComparison.OrdinalIgnoreCase)
            });
        }

        anchors.AddRange(interactions
            .Where(interaction => !string.IsNullOrWhiteSpace(interaction.AutomationId))
            .Select(interaction => interaction.AutomationId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(automationId => new UiValidationAnchor
            {
                AnchorType = "automation_id",
                Value = automationId,
                Stable = true
            }));

        anchors.AddRange(interactions
            .Where(interaction => !string.IsNullOrWhiteSpace(interaction.Name))
            .Select(interaction => interaction.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(name => new UiValidationAnchor
            {
                AnchorType = "control_name",
                Value = name,
                Stable = true
            }));

        return anchors;
    }

    private static string BuildSignature(string? title, IEnumerable<UiInteraction> interactions)
    {
        var selected = interactions
            .Where(interaction => interaction.IsSelected == true)
            .Select(interaction => $"{interaction.ControlType}:{interaction.AutomationId}:{interaction.Name}")
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
        var anchors = interactions
            .Where(interaction => !string.IsNullOrWhiteSpace(interaction.AutomationId))
            .Select(interaction => interaction.AutomationId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(25);

        return Slug($"{title}|{string.Join(",", selected)}|{string.Join(",", anchors)}");
    }

    private static string BuildRouteSignature(UiRoute route)
    {
        return string.Join(">", route.Steps
            .Where(step => !IsBootstrapStep(step))
            .Select(step => $"{step.ControlType}|{step.AutomationId}|{step.Name}|{step.Action}"));
    }

    private static string BuildScreenIdSeed(UiRoute route)
    {
        var labels = route.Steps
            .Where(step => !IsBootstrapStep(step))
            .Select(step => step.Name ?? step.AutomationId ?? step.ControlType)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .TakeLast(3);

        var seed = string.Join('-', labels);
        return string.IsNullOrWhiteSpace(seed) ? route.RouteText : seed;
    }

    private static bool IsBootstrapStep(UiRouteStep step)
    {
        return string.Equals(step.Kind, "bootstrap", StringComparison.OrdinalIgnoreCase)
               || string.Equals(step.Action, "verify", StringComparison.OrdinalIgnoreCase);
    }

    private static string? InferModule(UiRoute route, IEnumerable<UiInteraction> interactions)
    {
        var routeName = route.Steps.LastOrDefault()?.Name ?? route.RouteText;
        if (!string.IsNullOrWhiteSpace(routeName))
        {
            return routeName;
        }

        return interactions.FirstOrDefault(interaction => interaction.IsSelected == true)?.Name;
    }

    private static string InferFamily(UiRoute route)
    {
        return route.RouteType switch
        {
            "current" => "shell",
            "safe_dialog_open" => "dialog",
            "safe_detail_open" => "detail",
            "safe_wizard_entry" => "wizard",
            "safe_navigation" => "navigation",
            _ => "discovered"
        };
    }

    private static UiCoverageSummary BuildCoverage(
        IEnumerable<UiScreen> screens,
        double acceptedThreshold,
        IEnumerable<string> notes)
    {
        var list = screens.ToList();
        var accepted = list.Count(screen => screen.Readiness.Status == "accepted");
        var discovered = list.Count;
        return new UiCoverageSummary
        {
            DiscoveredCount = discovered,
            AcceptedCount = accepted,
            ReviewCount = list.Count(screen => screen.Readiness.Status == "review"),
            RejectedCount = list.Count(screen => screen.Readiness.Status == "rejected"),
            BlockedCount = list.Count(screen => screen.Readiness.Status == "blocked"),
            AcceptedRatio = discovered == 0 ? 0 : accepted / (double)discovered,
            AcceptedThreshold = acceptedThreshold,
            ThresholdMet = discovered > 0 && accepted / (double)discovered >= acceptedThreshold,
            Notes = notes
                .Where(note => !string.IsNullOrWhiteSpace(note))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public static List<UiRejectedRoute> BuildRejectedRoutesArtifact(IEnumerable<UiScreen> screens)
    {
        return screens
            .Where(screen => screen.Readiness.Status == "rejected")
            .Select(screen => new UiRejectedRoute
            {
                ScreenId = screen.ScreenId,
                ScreenName = screen.ScreenName,
                Route = screen.Route,
                RequiredState = screen.RequiredState,
                Signature = screen.Signature,
                BlockerType = screen.Readiness.BlockerType,
                Reasons = screen.Readiness.Reasons.ToList()
            })
            .ToList();
    }

    private static void TryCloseUnexpectedModal(Window activeWindow, string repositoryName)
    {
        var title = SafeWindowTitle(activeWindow);
        if (!string.IsNullOrWhiteSpace(title)
            && title.Contains(repositoryName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TryDismissWindow(activeWindow, out _);
    }

    private static string? SafeWindowTitle(Window? window)
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

    private static string? SafeName(AutomationElement element)
    {
        try
        {
            return string.IsNullOrWhiteSpace(element.Name) ? null : element.Name;
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeAutomationId(AutomationElement element)
    {
        try
        {
            return string.IsNullOrWhiteSpace(element.AutomationId) ? null : element.AutomationId;
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeClassName(AutomationElement element)
    {
        try
        {
            return string.IsNullOrWhiteSpace(element.ClassName) ? null : element.ClassName;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsEnabled(AutomationElement element)
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

    private static string SafeControlType(AutomationElement element)
    {
        try
        {
            return element.ControlType.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Slug(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = string.Join('-', new string(chars)
            .Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "screen";
        }

        if (slug.Length <= 72)
        {
            return slug;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(slug)))
            .ToLowerInvariant()[..12];
        return $"{slug[..56].Trim('-')}-{hash}";
    }

    private sealed class UiTraversalQueue
    {
        private readonly SortedDictionary<int, Queue<UiTraversalNode>> _queues = new();
        private int _count;

        public int Count => _count;

        public void Enqueue(UiTraversalNode node)
        {
            if (!_queues.TryGetValue(node.Depth, out var queue))
            {
                queue = new Queue<UiTraversalNode>();
                _queues[node.Depth] = queue;
            }

            queue.Enqueue(node);
            _count++;
        }

        public UiTraversalNode Dequeue()
        {
            foreach (var key in _queues.Keys.ToList())
            {
                var queue = _queues[key];
                if (queue.Count == 0)
                {
                    continue;
                }

                var node = queue.Dequeue();
                _count--;
                if (queue.Count == 0)
                {
                    _queues.Remove(key);
                }

                return node;
            }

            throw new InvalidOperationException("Traversal queue is empty.");
        }

        public List<UiRoute> SnapshotRoutes()
        {
            return _queues
                .OrderBy(entry => entry.Key)
                .SelectMany(entry => entry.Value)
                .Select(node => node.Route)
                .ToList();
        }
    }

    private sealed record UiTraversalNode(UiRoute Route, int Depth);
}
