using System.Diagnostics;
using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace CaptureRunner.Services;

public sealed class WindowService
{
    public AppSession EnsureSession(string executablePath, TimeSpan startupTimeout)
    {
        var fullPath = Path.GetFullPath(executablePath);
        var processName = Path.GetFileNameWithoutExtension(fullPath);

        var existingProcesses = Process.GetProcessesByName(processName)
            .Where(process => !process.HasExited)
            .ToList();

        if (existingProcesses.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple running instances of {processName}.exe were found ({existingProcesses.Count}). Close the extras before starting CaptureRunner.");
        }

        var existingProcess = existingProcesses.FirstOrDefault();
        if (existingProcess is not null)
        {
            return new AppSession(fullPath, processName, existingProcess, startedByScanner: false);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fullPath,
            WorkingDirectory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };

        var launchedProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start {fullPath}");

        try
        {
            launchedProcess.WaitForInputIdle((int)Math.Min(startupTimeout.TotalMilliseconds, 5000));
        }
        catch
        {
            // ClickOnce and some WPF apps do not always expose an input-idle state. The explicit window wait below is the real gate.
        }

        var deadline = DateTime.UtcNow + startupTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Process.GetProcessesByName(processName).Any(process => !process.HasExited))
            {
                break;
            }

            Thread.Sleep(250);
        }

        return new AppSession(fullPath, processName, launchedProcess, startedByScanner: true);
    }

    public Window? WaitForWindow(
        UIA3Automation automation,
        AppSession session,
        string? titleHint,
        TimeSpan timeout,
        WindowTitleMatchMode matchMode = WindowTitleMatchMode.Contains)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var window = FindBestWindow(automation, session, titleHint, matchMode);
            if (window is not null)
            {
                return window;
            }

            Thread.Sleep(250);
        }

        return FindBestWindow(automation, session, titleHint, matchMode);
    }

    public void TryBringToFront(Window? window)
    {
        if (window is null)
        {
            return;
        }

        try
        {
            window.Focus();
        }
        catch
        {
            // Best effort only.
        }
    }

    public void TryCloseSession(AppSession session)
    {
        if (session.Process is null)
        {
            return;
        }

        try
        {
            if (session.Process.HasExited)
            {
                return;
            }

            if (session.Process.CloseMainWindow())
            {
                if (session.Process.WaitForExit(5000))
                {
                    return;
                }
            }

            session.Process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Cleanup is best effort and should never hide the scan result.
        }
    }

    public static bool MatchesWindowTitle(string? candidate, string? titleHint, WindowTitleMatchMode matchMode)
    {
        if (string.IsNullOrWhiteSpace(titleHint))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return matchMode switch
        {
            WindowTitleMatchMode.Exact => candidate.Equals(titleHint, StringComparison.OrdinalIgnoreCase),
            _ => candidate.Contains(titleHint, StringComparison.OrdinalIgnoreCase)
        };
    }

    private Window? FindBestWindow(UIA3Automation automation, AppSession session, string? titleHint, WindowTitleMatchMode matchMode)
    {
        var desktop = automation.GetDesktop();
        var windows = desktop
            .FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .Select(element => element.AsWindow())
            .Where(window => BelongsToSession(window, session))
            .ToList();

        if (windows.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(titleHint))
        {
            var titleMatch = windows.FirstOrDefault(window => MatchesWindowTitle(window.Title, titleHint, matchMode)
                                                           || MatchesWindowTitle(window.Name, titleHint, matchMode));
            if (titleMatch is not null)
            {
                return titleMatch;
            }
        }

        return windows
            .OrderByDescending(window => !string.IsNullOrWhiteSpace(window.Title))
            .ThenByDescending(window => window.BoundingRectangle.Width * window.BoundingRectangle.Height)
            .FirstOrDefault();
    }

    private static bool BelongsToSession(Window window, AppSession session)
    {
        try
        {
            if (session.Process is not null && !session.Process.HasExited && window.Properties.ProcessId.TryGetValue(out var processId))
            {
                if (processId == session.Process.Id)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Fall through to process-name matching.
        }

        try
        {
            if (window.Properties.ProcessId.TryGetValue(out var processId))
            {
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

}

public sealed class AppSession
{
    public AppSession(string executablePath, string processName, Process process, bool startedByScanner)
    {
        ExecutablePath = executablePath;
        ProcessName = processName;
        Process = process;
        StartedByScanner = startedByScanner;
    }

    public string ExecutablePath { get; }

    public string ProcessName { get; }

    public Process Process { get; }

    public bool StartedByScanner { get; }
}
