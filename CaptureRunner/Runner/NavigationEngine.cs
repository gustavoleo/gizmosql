using System.Windows.Forms;
using CaptureRunner.Models;
using CaptureRunner.Services;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace CaptureRunner.Runner;

public sealed class NavigationEngine
{
    private static readonly string[] TreePreferredControls =
    {
        "TreeItem",
        "MenuItem",
        "TabItem",
        "ListItem",
        "Button",
        "Hyperlink",
        "Text"
    };

    private readonly TreeService _treeService;
    private readonly Validator _validator;
    private readonly WindowService _windowService;

    public NavigationEngine(TreeService treeService, Validator validator, WindowService windowService)
    {
        _treeService = treeService;
        _validator = validator;
        _windowService = windowService;
    }

    public IReadOnlyList<NavigationMethod> GetMethods(ScreenPlan plan)
    {
        var methods = new List<NavigationMethod>();
        if (plan.Hints.TreePath.Count > 0)
        {
            methods.Add(NavigationMethod.TreeNavigation);
        }

        if (plan.Hints.Shortcut.Count > 0)
        {
            methods.Add(NavigationMethod.KeyboardShortcut);
        }

        methods.Add(NavigationMethod.FocusChange);
        return methods;
    }

    public NavigationAttemptResult TryNavigate(AppSession session, UIA3Automation automation, Window window, ScreenPlan plan, NavigationMethod method)
    {
        var navigated = method switch
        {
            NavigationMethod.TreeNavigation => TryTreeNavigation(window, plan),
            NavigationMethod.KeyboardShortcut => TryShortcutNavigation(window, plan),
            NavigationMethod.FocusChange => TryFocusFallback(window, plan),
            _ => false
        };

        if (!navigated)
        {
            return new NavigationAttemptResult(false, window);
        }

        return ExecuteActions(session, automation, window, plan.Actions);
    }

    public static string ToReportMethod(NavigationMethod method)
    {
        return method switch
        {
            NavigationMethod.TreeNavigation => "tree_navigation",
            NavigationMethod.KeyboardShortcut => "keyboard",
            NavigationMethod.FocusChange => "focus_change",
            _ => "none"
        };
    }

    private bool TryTreeNavigation(Window window, ScreenPlan plan)
    {
        var path = plan.Hints.TreePath
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();

        if (path.Count == 0)
        {
            return false;
        }

        for (var index = 0; index < path.Count; index++)
        {
            var segment = path[index];
            var target = _treeService.FindFirstMatchingDescendant(window, new[] { segment }, TreePreferredControls);
            if (target is null)
            {
                return false;
            }

            if (!ActivateNavigationElement(target, index < path.Count - 1))
            {
                return false;
            }

            if (index < path.Count - 1)
            {
                SendKeys.SendWait("{RIGHT}");
            }

            Thread.Sleep(400);
        }

        return true;
    }

    private bool TryShortcutNavigation(Window window, ScreenPlan plan)
    {
        var shortcut = plan.Hints.Shortcut.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return false;
        }

        window.Focus();
        Thread.Sleep(150);
        SendKeys.SendWait(ShortcutEncoding.ToSendKeys(shortcut));
        Thread.Sleep(600);
        return true;
    }

    private bool TryFocusFallback(Window window, ScreenPlan plan)
    {
        window.Focus();
        Thread.Sleep(150);

        var candidateNames = TreeService.BuildExpectedNames(plan);
        var target = _treeService.FindFirstMatchingDescendant(window, candidateNames, TreePreferredControls);
        if (target is not null)
        {
            FocusAndClick(target);
            Thread.Sleep(400);
            return true;
        }

        SendKeys.SendWait("{TAB}");
        Thread.Sleep(200);
        SendKeys.SendWait("{TAB}");
        Thread.Sleep(300);

        return _validator.QuickMatch(window, plan);
    }

    private NavigationAttemptResult ExecuteActions(AppSession session, UIA3Automation automation, Window window, IEnumerable<PlanAction> actions)
    {
        var currentWindow = window;

        foreach (var action in actions)
        {
            var actionResult = ExecuteAction(session, automation, currentWindow, action);
            if (!actionResult.Success)
            {
                if (action.Required)
                {
                    return new NavigationAttemptResult(false, actionResult.ActiveWindow ?? currentWindow);
                }
            }

            currentWindow = actionResult.ActiveWindow ?? currentWindow;
        }

        return new NavigationAttemptResult(true, currentWindow);
    }

    private NavigationActionResult ExecuteAction(AppSession session, UIA3Automation automation, Window window, PlanAction action)
    {
        return action.Kind switch
        {
            PlanActionKind.Invoke => TryInvokeAction(window, action),
            PlanActionKind.WaitForElement => TryWaitForElement(window, action),
            PlanActionKind.WaitForWindow => TryWaitForWindow(session, automation, window, action),
            PlanActionKind.SendKeys => TrySendKeysAction(window, action),
            PlanActionKind.Delay => TryDelay(action),
            _ => new NavigationActionResult(!action.Required, window)
        };
    }

    private NavigationActionResult TryInvokeAction(Window window, PlanAction action)
    {
        var target = FindActionTargetWithWait(window, action);
        if (target is null)
        {
            return new NavigationActionResult(false, window);
        }

        FocusAndClick(target);
        Thread.Sleep(ResolvePostActionDelay(action));
        return new NavigationActionResult(true, window);
    }

    private NavigationActionResult TryWaitForElement(Window window, PlanAction action)
    {
        return new NavigationActionResult(FindActionTargetWithWait(window, action) is not null, window);
    }

    private NavigationActionResult TryWaitForWindow(AppSession session, UIA3Automation automation, Window currentWindow, PlanAction action)
    {
        if (string.IsNullOrWhiteSpace(action.WindowTitle))
        {
            return new NavigationActionResult(false, currentWindow);
        }

        var timeout = TimeSpan.FromMilliseconds(action.TimeoutMs.GetValueOrDefault(4000));
        if (timeout < TimeSpan.Zero)
        {
            timeout = TimeSpan.Zero;
        }

        var window = _windowService.WaitForWindow(automation, session, action.WindowTitle, timeout, action.MatchMode);
        if (window is null)
        {
            return new NavigationActionResult(false, currentWindow);
        }

        _windowService.TryBringToFront(window);
        Thread.Sleep(ResolvePostActionDelay(action));
        return new NavigationActionResult(true, window);
    }

    private static NavigationActionResult TrySendKeysAction(Window window, PlanAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Keys))
        {
            return new NavigationActionResult(false, window);
        }

        window.Focus();
        Thread.Sleep(150);
        SendKeys.SendWait(action.Keys);
        Thread.Sleep(ResolvePostActionDelay(action));
        return new NavigationActionResult(true, window);
    }

    private static NavigationActionResult TryDelay(PlanAction action)
    {
        var delayMs = action.TimeoutMs.GetValueOrDefault(400);
        if (delayMs < 0)
        {
            delayMs = 0;
        }

        Thread.Sleep(delayMs);
        return new NavigationActionResult(true, null);
    }

    private static int ResolvePostActionDelay(PlanAction action)
    {
        var delayMs = action.PostActionDelayMs.GetValueOrDefault(400);
        return delayMs < 0 ? 0 : delayMs;
    }

    private AutomationElement? FindActionTargetWithWait(Window window, PlanAction action)
    {
        var timeout = TimeSpan.FromMilliseconds(action.TimeoutMs.GetValueOrDefault(4000));
        if (timeout < TimeSpan.Zero)
        {
            timeout = TimeSpan.Zero;
        }

        var deadline = DateTime.UtcNow + timeout;
        AutomationElement? found = null;

        do
        {
            found = _treeService.FindFirstActionTarget(window, action);
            if (found is not null)
            {
                return found;
            }

            Thread.Sleep(200);
        }
        while (DateTime.UtcNow < deadline);

        return null;
    }

    private static void FocusAndClick(AutomationElement element)
    {
        try
        {
            element.Focus();
            Thread.Sleep(100);
        }
        catch
        {
            // Some WPF elements refuse SetFocus even though they still expose a clickable point.
        }

        try
        {
            var point = element.GetClickablePoint();
            Mouse.MoveTo(point);
            Mouse.Click(MouseButton.Left);
            return;
        }
        catch
        {
            // Fall through to keyboard activation.
        }

        SendKeys.SendWait("{ENTER}");
    }

    private static bool ActivateNavigationElement(AutomationElement element, bool expandAfterSelection)
    {
        try
        {
            element.Focus();
        }
        catch
        {
            // Continue with pattern or click fallback.
        }

        try
        {
            if (element.ControlType.ToString() == "TreeItem")
            {
                var treeItem = element.AsTreeItem();
                try
                {
                    treeItem.Select();
                }
                catch
                {
                    // Fall back to click activation below.
                }

                if (expandAfterSelection)
                {
                    try
                    {
                        treeItem.Expand();
                    }
                    catch
                    {
                        // Not all tree items are expandable.
                    }
                }

                Thread.Sleep(300);
                return true;
            }
        }
        catch
        {
            // Fall through to click fallback.
        }

        FocusAndClick(element);
        return true;
    }

}

public enum NavigationMethod
{
    TreeNavigation,
    KeyboardShortcut,
    FocusChange
}

public sealed record NavigationAttemptResult(bool Success, Window? ActiveWindow);

public sealed record NavigationActionResult(bool Success, Window? ActiveWindow);
