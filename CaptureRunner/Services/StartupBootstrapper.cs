using System.Diagnostics;
using System.Windows.Forms;
using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace CaptureRunner.Services;

public sealed class StartupBootstrapper
{
    public StartupStateReport Run(AppSession session, UIA3Automation automation, ScannerOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var profile = BootstrapProfile.CreateEffective(options.BootstrapProfile, options.RepositoryName);
        var report = new StartupStateReport
        {
            RepositoryName = profile.RepositoryName
        };

        try
        {
            var deadline = DateTime.UtcNow + ResolveBootstrapTimeout(options);
            var windows = WaitForSessionWindows(session, automation, deadline);
            AddWindowsSeen(report, windows);

            report.ApplicationStartedOrAttached = windows.Count > 0 || IsSessionProcessRunning(session);
            if (!report.ApplicationStartedOrAttached)
            {
                report.Notes.Add("The target application process was not running after startup.");
                return report;
            }

            if (TryFindRepositoryWindow(windows, profile, out var repositoryWindow))
            {
                report.RepositoryVerified = true;
                report.LoginSuccess = true;
                report.MethodUsed = "already_in_repository";
                report.ActiveWindowTitle = SafeWindowTitle(repositoryWindow);
                report.Notes.Add($"Repository '{profile.RepositoryName}' was already active.");
                return report;
            }

            var loginWindow = FindLoginWindow(windows, profile);
            while (loginWindow is null && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(250);
                windows = FindSessionWindows(session, automation);
                AddWindowsSeen(report, windows);

                if (TryFindRepositoryWindow(windows, profile, out repositoryWindow))
                {
                    report.RepositoryVerified = true;
                    report.LoginSuccess = true;
                    report.MethodUsed = "repository_opened_without_login";
                    report.ActiveWindowTitle = SafeWindowTitle(repositoryWindow);
                    report.Notes.Add($"Repository '{profile.RepositoryName}' became active before login automation was needed.");
                    return report;
                }

                loginWindow = FindLoginWindow(windows, profile);
            }

            if (loginWindow is null)
            {
                report.MethodUsed = "startup_state_unknown";
                report.ActiveWindowTitle = SafeWindowTitle(windows.FirstOrDefault());
                report.Notes.Add("No repository window or login window was detected before the bootstrap timeout.");
                return report;
            }

            report.LoginDetected = true;
            report.ActiveWindowTitle = SafeWindowTitle(loginWindow);

            if (TryDismissStartupError(loginWindow, out var dismissNote))
            {
                report.Notes.Add(dismissNote);
                Thread.Sleep(500);
                windows = FindSessionWindows(session, automation);
                AddWindowsSeen(report, windows);
                loginWindow = FindLoginWindow(windows, profile) ?? loginWindow;
            }

            if (TryCompleteRepositorySelection(windows, profile, report, out var initialSelectionNote))
            {
                report.LoginAttempted = true;
                report.LoginSuccess = true;
                report.MethodUsed = "saved_password_repository_selection";
                report.Notes.Add(initialSelectionNote);

                if (WaitForRepository(session, automation, profile, report, deadline, out repositoryWindow))
                {
                    report.ActiveWindowTitle = SafeWindowTitle(repositoryWindow);
                    report.Notes.Add($"Repository '{profile.RepositoryName}' was verified after repository selection.");
                    return report;
                }
            }

            var loginActions = FindLoginActions(loginWindow, profile).ToList();
            report.CandidateLoginActions = loginActions
                .Select(ToStartupActionState)
                .ToList();

            var loginAction = loginActions.FirstOrDefault();
            if (loginAction is null)
            {
                report.MethodUsed = "login_action_not_found";
                report.Notes.Add("A login window was detected, but no configured saved-password login action was found.");
                return report;
            }

            report.LoginAttempted = true;
            report.MethodUsed = "saved_password_login";
            if (!TryInvoke(loginAction, loginWindow, "login action", out var invokeNote))
            {
                report.Notes.Add(invokeNote);
                return report;
            }

            report.LoginSuccess = true;
            report.Notes.Add(invokeNote);

            var repositorySelectionCompleted = false;
            var repositoryOpenAttempted = false;
            while (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(500);
                windows = FindSessionWindows(session, automation);
                AddWindowsSeen(report, windows);

                if (TryFindRepositoryWindow(windows, profile, out repositoryWindow))
                {
                    report.RepositoryVerified = true;
                    report.ActiveWindowTitle = SafeWindowTitle(repositoryWindow);
                    report.Notes.Add($"Repository '{profile.RepositoryName}' was verified after saved-password login.");
                    return report;
                }

                if (!repositorySelectionCompleted
                    && TryCompleteRepositorySelection(windows, profile, report, out var selectionNote))
                {
                    repositorySelectionCompleted = true;
                    report.Notes.Add(selectionNote);
                }
                else if (!repositoryOpenAttempted
                         && TryOpenRepositorySelector(windows, out var openRepositoryNote))
                {
                    repositoryOpenAttempted = true;
                    report.Notes.Add(openRepositoryNote);
                }
            }

            report.ActiveWindowTitle = SafeWindowTitle(FindSessionWindows(session, automation).FirstOrDefault());
            report.Notes.Add($"Saved-password login was attempted, but repository '{profile.RepositoryName}' was not verified before timeout.");
            return report;
        }
        catch (Exception ex)
        {
            report.Exception = ex.ToString();
            report.Notes.Add($"Bootstrap failed: {ex.Message}");
            return report;
        }
        finally
        {
            stopwatch.Stop();
            report.DurationMs = stopwatch.ElapsedMilliseconds;
        }
    }

    public static int GetExitCode(StartupStateReport report)
    {
        if (!report.ApplicationStartedOrAttached)
        {
            return 1;
        }

        if (report.LoginDetected && (!report.LoginAttempted || !report.LoginSuccess))
        {
            return 1;
        }

        return report.RepositoryVerified ? 0 : 2;
    }

    private static bool WaitForRepository(
        AppSession session,
        UIA3Automation automation,
        BootstrapProfile profile,
        StartupStateReport report,
        DateTime deadline,
        out Window? repositoryWindow)
    {
        repositoryWindow = null;
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(500);
            var windows = FindSessionWindows(session, automation);
            AddWindowsSeen(report, windows);

            if (TryFindRepositoryWindow(windows, profile, out repositoryWindow))
            {
                report.RepositoryVerified = true;
                return true;
            }
        }

        return false;
    }

    private static TimeSpan ResolveBootstrapTimeout(ScannerOptions options)
    {
        var minimum = TimeSpan.FromSeconds(30);
        return options.StartupTimeout > minimum ? options.StartupTimeout : minimum;
    }

    private static List<Window> WaitForSessionWindows(AppSession session, UIA3Automation automation, DateTime deadline)
    {
        while (DateTime.UtcNow < deadline)
        {
            var windows = FindSessionWindows(session, automation);
            if (windows.Count > 0)
            {
                return windows;
            }

            Thread.Sleep(250);
        }

        return FindSessionWindows(session, automation);
    }

    private static bool IsSessionProcessRunning(AppSession session)
    {
        try
        {
            if (!session.Process.HasExited)
            {
                return true;
            }
        }
        catch
        {
            // Fall through to process-name lookup.
        }

        try
        {
            foreach (var process in Process.GetProcessesByName(session.ProcessName))
            {
                using (process)
                {
                    if (!process.HasExited)
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static List<Window> FindSessionWindows(AppSession session, UIA3Automation automation)
    {
        try
        {
            var desktop = automation.GetDesktop();
            return desktop
                .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .Select(element => element.AsWindow())
                .Where(window => BelongsToSession(window, session))
                .OrderByDescending(window => !string.IsNullOrWhiteSpace(SafeWindowTitle(window)))
                .ThenByDescending(window => SafeWindowArea(window))
                .ToList();
        }
        catch
        {
            return new List<Window>();
        }
    }

    private static bool BelongsToSession(Window window, AppSession session)
    {
        try
        {
            if (window.Properties.ProcessId.TryGetValue(out var processId))
            {
                if (session.Process is { HasExited: false } && processId == session.Process.Id)
                {
                    return true;
                }

                using var process = Process.GetProcessById(processId);
                return process.ProcessName.Equals(session.ProcessName, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static Window? FindLoginWindow(IEnumerable<Window> windows, BootstrapProfile profile)
    {
        var titles = profile.LoginWindowTitles
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return windows.FirstOrDefault(window =>
            titles.Any(title => Contains(SafeWindowTitle(window), title) || Contains(SafeName(window), title)));
    }

    private static bool TryFindRepositoryWindow(IEnumerable<Window> windows, BootstrapProfile profile, out Window? repositoryWindow)
    {
        repositoryWindow = null;
        foreach (var window in windows)
        {
            if (IsRepositoryWindow(window, profile))
            {
                repositoryWindow = window;
                return true;
            }
        }

        return false;
    }

    private static bool IsRepositoryWindow(Window window, BootstrapProfile profile)
    {
        var titleNeedle = profile.RepositoryValidation.WindowTitleContains;
        if (Contains(SafeWindowTitle(window), titleNeedle) || Contains(SafeName(window), titleNeedle))
        {
            return true;
        }

        if (ContainsRepositorySelector(window, profile))
        {
            return false;
        }

        var visibleTextNeedle = profile.RepositoryValidation.VisibleTextContains;
        if (string.IsNullOrWhiteSpace(visibleTextNeedle))
        {
            return false;
        }

        try
        {
            return window
                .FindAllDescendants()
                .Take(500)
                .Any(element => Contains(SafeName(element), visibleTextNeedle));
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsRepositorySelector(Window window, BootstrapProfile profile)
    {
        var selectorIds = profile.RepositorySelectorAutomationIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectorIds.Count == 0)
        {
            return false;
        }

        try
        {
            return window
                .FindAllDescendants()
                .Any(element => SafeControlType(element) == "ComboBox"
                                && selectorIds.Contains(SafeAutomationId(element) ?? string.Empty));
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<AutomationElement> FindLoginActions(Window loginWindow, BootstrapProfile profile)
    {
        var actionNames = profile.LoginPositiveActions
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var actionRanks = actionNames
            .Select((name, index) => new { name, index })
            .ToDictionary(item => item.name, item => item.index, StringComparer.OrdinalIgnoreCase);

        try
        {
            var descendants = loginWindow.FindAllDescendants().ToList();
            var hasLoginFields = descendants.Any(element =>
                string.Equals(SafeAutomationId(element), "dfLogin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(SafeAutomationId(element), "dfPassword", StringComparison.OrdinalIgnoreCase));

            return descendants
                .Where(element => IsActionControl(element))
                .Where(element => actionRanks.ContainsKey(SafeName(element) ?? string.Empty))
                .Where(SafeIsEnabled)
                .Where(element => !hasLoginFields || IsLoginDialogAction(element))
                .OrderBy(element => actionRanks[SafeName(element) ?? string.Empty])
                .ToList();
        }
        catch
        {
            return Enumerable.Empty<AutomationElement>();
        }
    }

    private static bool IsLoginDialogAction(AutomationElement element)
    {
        var automationId = SafeAutomationId(element);
        if (string.Equals(automationId, "cmdOK", StringComparison.OrdinalIgnoreCase)
            || string.Equals(automationId, "pbShowDetails", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !(SafeClassName(element)?.Contains("Ribbon", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static bool TryDismissStartupError(Window loginWindow, out string note)
    {
        note = string.Empty;
        List<AutomationElement> descendants;
        try
        {
            descendants = loginWindow.FindAllDescendants().ToList();
        }
        catch
        {
            return false;
        }

        var hasStartupError = descendants.Any(element =>
            string.Equals(SafeAutomationId(element), "dfError", StringComparison.OrdinalIgnoreCase)
            || string.Equals(SafeName(element), "Error message", StringComparison.OrdinalIgnoreCase));
        if (!hasStartupError)
        {
            return false;
        }

        var dismissButton = descendants.FirstOrDefault(element =>
            IsActionControl(element)
            && string.Equals(SafeName(element), "OK", StringComparison.OrdinalIgnoreCase)
            && string.Equals(SafeAutomationId(element), "cmdOK", StringComparison.OrdinalIgnoreCase));
        if (dismissButton is null)
        {
            note = "Startup error was detected, but no OK dismiss button was found.";
            return false;
        }

        return TryInvoke(dismissButton, loginWindow, "startup error dismiss action", out note);
    }

    private static IEnumerable<AutomationElement> FindNamedActionControls(AutomationElement root, IEnumerable<string> configuredNames)
    {
        var actionNames = configuredNames
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var actionRanks = actionNames
            .Select((name, index) => new { name, index })
            .ToDictionary(item => item.name, item => item.index, StringComparer.OrdinalIgnoreCase);

        try
        {
            return root
                .FindAllDescendants()
                .Where(element => IsActionControl(element) && actionRanks.ContainsKey(SafeName(element) ?? string.Empty))
                .Where(SafeIsEnabled)
                .OrderBy(element => actionRanks[SafeName(element) ?? string.Empty])
                .ToList();
        }
        catch
        {
            return Enumerable.Empty<AutomationElement>();
        }
    }

    private static bool TryOpenRepositorySelector(IEnumerable<Window> windows, out string note)
    {
        note = string.Empty;
        foreach (var window in windows)
        {
            var connectAction = FindNamedActionControls(window, new[] { "Connect" })
                .FirstOrDefault(element =>
                    SafeClassName(element)?.Contains("Ribbon", StringComparison.OrdinalIgnoreCase) == true
                    || SafeControlType(element) == "Button");
            if (connectAction is null)
            {
                continue;
            }

            return TryInvoke(connectAction, window, "repository open action", out note);
        }

        return false;
    }

    private static bool TryCompleteRepositorySelection(
        IEnumerable<Window> windows,
        BootstrapProfile profile,
        StartupStateReport report,
        out string note)
    {
        note = string.Empty;
        var windowList = windows.ToList();
        var selectorMatch = FindRepositorySelector(windowList, profile, report);
        if (selectorMatch is null)
        {
            return false;
        }

        report.RepositorySelectionDetected = true;
        report.RepositorySelectionAttempted = true;

        if (!TrySelectRepository(selectorMatch.Window, selectorMatch.Selector, profile.RepositoryName, out var selectNote))
        {
            note = selectNote;
            return false;
        }

        report.RepositorySelected = true;

        var confirmAction = windowList
            .SelectMany(window => FindNamedActionControls(window, profile.RepositoryConfirmActions)
                .Select(action => new WindowActionMatch(window, action)))
            .FirstOrDefault();

        if (confirmAction is null)
        {
            note = $"{selectNote} No repository confirm action was found.";
            return false;
        }

        if (!TryInvoke(confirmAction.Action, confirmAction.Window, "repository confirm action", out var confirmNote))
        {
            note = $"{selectNote} {confirmNote}";
            return false;
        }

        note = $"{selectNote} {confirmNote}";
        return true;
    }

    private static WindowSelectorMatch? FindRepositorySelector(
        IEnumerable<Window> windows,
        BootstrapProfile profile,
        StartupStateReport report)
    {
        var selectorIds = profile.RepositorySelectorAutomationIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectorIds.Count == 0)
        {
            return null;
        }

        foreach (var window in windows)
        {
            List<AutomationElement> selectors;
            try
            {
                selectors = window
                    .FindAllDescendants()
                    .Where(element => SafeControlType(element) == "ComboBox")
                    .Where(element => selectorIds.Contains(SafeAutomationId(element) ?? string.Empty))
                    .ToList();
            }
            catch
            {
                continue;
            }

            foreach (var selector in selectors)
            {
                AddUnique(report.CandidateRepositorySelectors, ToStartupActionState(selector));
            }

            var enabledSelector = selectors.FirstOrDefault(SafeIsEnabled);
            if (enabledSelector is not null)
            {
                return new WindowSelectorMatch(window, enabledSelector);
            }
        }

        return null;
    }

    private static bool TrySelectRepository(Window window, AutomationElement selector, string repositoryName, out string note)
    {
        try
        {
            if (selector.Patterns.ExpandCollapse.TryGetPattern(out var expandPattern))
            {
                expandPattern.Expand();
                Thread.Sleep(250);
            }
        }
        catch
        {
            // The combo box may already be expanded. Continue to item discovery.
        }

        AutomationElement? repositoryItem;
        try
        {
            repositoryItem = window
                .FindAllDescendants()
                .Where(element => SafeControlType(element) == "ListItem")
                .FirstOrDefault(element => string.Equals(SafeName(element), repositoryName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            note = $"Unable to inspect repository selector '{SafeAutomationId(selector)}': {ex.Message}";
            return false;
        }

        if (repositoryItem is null)
        {
            note = $"Repository '{repositoryName}' was not found in selector '{SafeAutomationId(selector)}'.";
            return false;
        }

        try
        {
            if (repositoryItem.Patterns.SelectionItem.TryGetPattern(out var selectionPattern))
            {
                if (!selectionPattern.IsSelected)
                {
                    selectionPattern.Select();
                    Thread.Sleep(250);
                }

                note = $"Selected repository '{repositoryName}' through selector '{SafeAutomationId(selector)}'.";
                return true;
            }
        }
        catch
        {
            // Fall through to focus/Enter selection.
        }

        try
        {
            repositoryItem.Focus();
            SendKeys.SendWait("{ENTER}");
            Thread.Sleep(250);
            note = $"Selected repository '{repositoryName}' with focused Enter fallback.";
            return true;
        }
        catch (Exception ex)
        {
            note = $"Unable to select repository '{repositoryName}': {ex.Message}";
            return false;
        }
    }

    private static bool IsActionControl(AutomationElement element)
    {
        return SafeControlType(element) is "Button" or "MenuItem" or "Hyperlink";
    }

    private static bool TryInvoke(AutomationElement element, Window loginWindow, string actionKind, out string note)
    {
        try
        {
            if (element.Patterns.Invoke.TryGetPattern(out var invokePattern))
            {
                invokePattern.Invoke();
                note = $"Invoked {actionKind} '{SafeName(element)}' through UIA InvokePattern.";
                return true;
            }
        }
        catch
        {
            // Fall through to focused Enter activation.
        }

        try
        {
            element.Focus();
            Thread.Sleep(100);
            SendKeys.SendWait("{ENTER}");
            note = $"Activated {actionKind} '{SafeName(element)}' with focused Enter fallback.";
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                loginWindow.Focus();
                SendKeys.SendWait("{ENTER}");
                note = $"Focused {actionKind} failed ({ex.Message}); sent Enter to the login window.";
                return true;
            }
            catch (Exception windowEx)
            {
                note = $"Unable to invoke {actionKind} '{SafeName(element)}': {windowEx.Message}";
                return false;
            }
        }
    }

    private static void AddUnique(List<StartupActionState> items, StartupActionState state)
    {
        if (items.Any(item =>
                string.Equals(item.Name, state.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.AutomationId, state.AutomationId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.ControlType, state.ControlType, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        items.Add(state);
    }

    private static void AddWindowsSeen(StartupStateReport report, IEnumerable<Window> windows)
    {
        var existing = report.WindowsSeen
            .Select(window => $"{window.ProcessId}|{window.Title}|{window.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var window in windows)
        {
            var state = ToStartupWindowState(window);
            var key = $"{state.ProcessId}|{state.Title}|{state.Name}";
            if (existing.Add(key))
            {
                report.WindowsSeen.Add(state);
            }
        }
    }

    private static StartupWindowState ToStartupWindowState(Window window)
    {
        return new StartupWindowState
        {
            Title = SafeWindowTitle(window),
            Name = SafeName(window),
            ControlType = SafeControlType(window),
            ProcessId = SafeProcessId(window)
        };
    }

    private static StartupActionState ToStartupActionState(AutomationElement element)
    {
        return new StartupActionState
        {
            Name = SafeName(element),
            AutomationId = SafeAutomationId(element),
            ControlType = SafeControlType(element)
        };
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

    private static double SafeWindowArea(Window window)
    {
        try
        {
            return window.BoundingRectangle.Width * window.BoundingRectangle.Height;
        }
        catch
        {
            return 0;
        }
    }

    private static string? SafeName(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return string.IsNullOrWhiteSpace(element.Name) ? null : element.Name;
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeAutomationId(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return string.IsNullOrWhiteSpace(element.AutomationId) ? null : element.AutomationId;
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeClassName(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return string.IsNullOrWhiteSpace(element.ClassName) ? null : element.ClassName;
        }
        catch
        {
            return null;
        }
    }

    private static string? SafeControlType(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return element.ControlType.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static bool SafeIsEnabled(AutomationElement element)
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

    private static int? SafeProcessId(Window window)
    {
        try
        {
            return window.Properties.ProcessId.TryGetValue(out var processId) ? processId : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool Contains(string? candidate, string? expected)
    {
        return !string.IsNullOrWhiteSpace(candidate)
               && !string.IsNullOrWhiteSpace(expected)
               && candidate.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record WindowSelectorMatch(Window Window, AutomationElement Selector);

    private sealed record WindowActionMatch(Window Window, AutomationElement Action);
}
