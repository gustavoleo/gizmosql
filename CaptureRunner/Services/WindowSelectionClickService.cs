using System.Drawing;
using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace CaptureRunner.Services;

public sealed class WindowSelectionClickService
{
    public WindowClickResult Click(Window window, CaptureHints captureHints)
    {
        var strategies = new List<WindowClickStrategy> { captureHints.ClickStrategy };
        strategies.AddRange(captureHints.RetryClickStrategies.Where(strategy => !strategies.Contains(strategy)));

        var attempted = new List<string>();

        foreach (var strategy in strategies)
        {
            try
            {
                var point = GetClickPoint(window, strategy);
                Mouse.MoveTo(point);
                Thread.Sleep(75);
                Mouse.Click(MouseButton.Left);
                Thread.Sleep(Math.Max(50, captureHints.SelectionSettleMs.GetValueOrDefault(400)));

                return new WindowClickResult
                {
                    Success = true,
                    Strategy = strategy,
                    Point = point,
                    Note = $"Clicked target window using {strategy} at ({point.X}, {point.Y})."
                };
            }
            catch (Exception ex)
            {
                attempted.Add($"{strategy}: {ex.Message}");
            }
        }

        return new WindowClickResult
        {
            Success = false,
            Strategy = strategies.FirstOrDefault(),
            Point = Point.Empty,
            Note = attempted.Count == 0
                ? "No window-selection click strategies were configured."
                : $"Window-selection click failed. Attempts: {string.Join(" | ", attempted)}"
        };
    }

    private static Point GetClickPoint(Window window, WindowClickStrategy strategy)
    {
        var bounds = window.BoundingRectangle;
        var left = (int)Math.Round((double)bounds.Left);
        var top = (int)Math.Round((double)bounds.Top);
        var right = (int)Math.Round((double)bounds.Right);
        var bottom = (int)Math.Round((double)bounds.Bottom);
        var width = Math.Max(1, right - left);
        var height = Math.Max(1, bottom - top);

        return strategy switch
        {
            WindowClickStrategy.TitleBarCenter => new Point(left + (width / 2), top + Math.Min(24, Math.Max(10, height / 20))),
            WindowClickStrategy.ClientCenter => new Point(left + (width / 2), top + (height / 2)),
            WindowClickStrategy.TopLeftInset => new Point(left + Math.Min(40, Math.Max(10, width / 8)), top + Math.Min(40, Math.Max(10, height / 8))),
            _ => new Point(left + (width / 2), top + (height / 2))
        };
    }
}

public sealed class WindowClickResult
{
    public required bool Success { get; init; }

    public required WindowClickStrategy Strategy { get; init; }

    public required Point Point { get; init; }

    public required string Note { get; init; }
}
