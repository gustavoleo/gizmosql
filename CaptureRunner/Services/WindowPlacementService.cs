using System.Drawing;
using System.Runtime.InteropServices;
using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;

namespace CaptureRunner.Services;

public sealed class WindowPlacementService
{
    private readonly DisplayTopologyService _displayTopologyService;
    private readonly WindowService _windowService;

    public WindowPlacementService(DisplayTopologyService displayTopologyService, WindowService windowService)
    {
        _displayTopologyService = displayTopologyService;
        _windowService = windowService;
    }

    public PlacementResult Place(Window? window, ScannerOptions options)
    {
        if (window is null)
        {
            return new PlacementResult
            {
                Attempted = false,
                Success = false,
                Note = "No window available for placement."
            };
        }

        var target = options.EnvironmentProfile?.DisplayProfile.CaptureDisplay;
        if (target is null)
        {
            return new PlacementResult
            {
                Attempted = false,
                Success = true,
                Note = "No certified capture display was configured."
            };
        }

        var displays = _displayTopologyService.GetDisplays();
        var display = _displayTopologyService.FindMatch(target, displays);
        if (display is null)
        {
            return new PlacementResult
            {
                Attempted = true,
                Success = false,
                Note = "Configured capture display was not found."
            };
        }

        try
        {
            var handle = GetWindowHandle(window);
            if (handle == IntPtr.Zero)
            {
                return new PlacementResult
                {
                    Attempted = true,
                    Success = false,
                    DeviceName = display.DeviceName,
                    Note = "The target window did not expose a native window handle for placement."
                };
            }

            ShowWindow(handle, SwRestore);
            MoveWindow(handle, display.WorkingLeft, display.WorkingTop, display.WorkingWidth, display.WorkingHeight, true);
            ShowWindow(handle, SwMaximize);
            Thread.Sleep(150);
        }
        catch (Exception ex)
        {
            return new PlacementResult
            {
                Attempted = true,
                Success = false,
                DeviceName = display.DeviceName,
                Note = $"Window placement failed: {ex.Message}"
            };
        }

        _windowService.TryBringToFront(window);

        try
        {
            var rect = ToRectangle(window);
            var verification = VerifyPlacement(rect, display, displays);

            return new PlacementResult
            {
                Attempted = true,
                Success = verification.Success,
                DeviceName = display.DeviceName,
                Left = rect.Left,
                Top = rect.Top,
                Width = rect.Width,
                Height = rect.Height,
                CenterOnTarget = verification.CenterOnTarget,
                ContainedInTarget = verification.ContainedInTarget,
                OverlappedOtherDisplay = verification.OverlappedOtherDisplay,
                Note = verification.Note
            };
        }
        catch (Exception ex)
        {
            return new PlacementResult
            {
                Attempted = true,
                Success = false,
                DeviceName = display.DeviceName,
                Note = $"Placement verification failed: {ex.Message}"
            };
        }
    }

    public static PlacementVerification VerifyPlacement(
        Rectangle windowBounds,
        DisplayRuntimeInfo target,
        IEnumerable<DisplayRuntimeInfo> displays,
        int tolerance = 16)
    {
        var targetBounds = ToBounds(target);
        var targetWorkingArea = ToWorkingArea(target);
        var center = new Point(windowBounds.Left + (windowBounds.Width / 2), windowBounds.Top + (windowBounds.Height / 2));
        var expandedWorkingArea = Inflate(targetWorkingArea, tolerance);

        var centerOnTarget = targetBounds.Contains(center);
        var containedInTarget = expandedWorkingArea.Contains(windowBounds);

        var windowArea = Math.Max(1, windowBounds.Width * windowBounds.Height);
        var overlapDeviceNames = displays
            .Where(display => !display.DeviceName.Equals(target.DeviceName, StringComparison.OrdinalIgnoreCase))
            .Select(display => new
            {
                display.DeviceName,
                Area = Rectangle.Intersect(windowBounds, ToBounds(display)).Width * Rectangle.Intersect(windowBounds, ToBounds(display)).Height
            })
            .Where(item => item.Area > 0)
            .ToList();

        var overlappedOtherDisplay = overlapDeviceNames.Any(item => (double)item.Area / windowArea > 0.05d);
        var overlapSummary = overlapDeviceNames.Count == 0
            ? "none"
            : string.Join(", ", overlapDeviceNames.Select(item => $"{item.DeviceName}:{item.Area}px"));

        var success = centerOnTarget && containedInTarget && !overlappedOtherDisplay;
        var note = $"Placement verified against {target.DeviceName}: center_on_target={centerOnTarget}, contained_in_target={containedInTarget}, significant_overlap={overlappedOtherDisplay}, raw_overlap={overlapSummary}.";

        return new PlacementVerification
        {
            Success = success,
            CenterOnTarget = centerOnTarget,
            ContainedInTarget = containedInTarget,
            OverlappedOtherDisplay = overlappedOtherDisplay,
            Note = note
        };
    }

    private static Rectangle Inflate(Rectangle rectangle, int tolerance)
    {
        return Rectangle.FromLTRB(
            rectangle.Left - tolerance,
            rectangle.Top - tolerance,
            rectangle.Right + tolerance,
            rectangle.Bottom + tolerance);
    }

    private static Rectangle ToBounds(DisplayRuntimeInfo display)
    {
        return new Rectangle(display.Left, display.Top, display.Width, display.Height);
    }

    private static Rectangle ToWorkingArea(DisplayRuntimeInfo display)
    {
        return new Rectangle(display.WorkingLeft, display.WorkingTop, display.WorkingWidth, display.WorkingHeight);
    }

    private static Rectangle ToRectangle(Window window)
    {
        var rect = window.BoundingRectangle;
        return Rectangle.FromLTRB(
            (int)Math.Round((double)rect.Left),
            (int)Math.Round((double)rect.Top),
            (int)Math.Round((double)rect.Right),
            (int)Math.Round((double)rect.Bottom));
    }

    private static IntPtr GetWindowHandle(Window window)
    {
        try
        {
            if (window.Properties.NativeWindowHandle.TryGetValue(out var handle))
            {
                return new IntPtr(handle);
            }
        }
        catch
        {
            // Best effort only.
        }

        return IntPtr.Zero;
    }

    private const int SwRestore = 9;
    private const int SwMaximize = 3;

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool repaint);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int command);
}

public sealed class PlacementVerification
{
    public required bool Success { get; init; }

    public required bool CenterOnTarget { get; init; }

    public required bool ContainedInTarget { get; init; }

    public required bool OverlappedOtherDisplay { get; init; }

    public required string Note { get; init; }
}
