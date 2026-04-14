using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class DisplayTopologyService
{
    public IReadOnlyList<DisplayRuntimeInfo> GetDisplays()
    {
        return Screen.AllScreens
            .Select(screen => new DisplayRuntimeInfo
            {
                DeviceName = screen.DeviceName,
                Primary = screen.Primary,
                Left = screen.Bounds.Left,
                Top = screen.Bounds.Top,
                Width = screen.Bounds.Width,
                Height = screen.Bounds.Height,
                WorkingLeft = screen.WorkingArea.Left,
                WorkingTop = screen.WorkingArea.Top,
                WorkingWidth = screen.WorkingArea.Width,
                WorkingHeight = screen.WorkingArea.Height,
                ScalePercent = GetScalePercent(screen)
            })
            .ToList();
    }

    public DisplayRuntimeInfo? FindMatch(DisplayTarget? target)
    {
        return FindMatch(target, GetDisplays());
    }

    public DisplayRuntimeInfo? FindMatch(DisplayTarget? target, IReadOnlyList<DisplayRuntimeInfo> displays)
    {
        if (target is null)
        {
            return null;
        }

        return displays.FirstOrDefault(display => MatchesTarget(display, target));
    }

    public static bool MatchesTarget(DisplayRuntimeInfo display, DisplayTarget target)
    {
        return (string.IsNullOrWhiteSpace(target.DeviceName)
                || display.DeviceName.Equals(target.DeviceName, StringComparison.OrdinalIgnoreCase))
               && (!target.Primary.HasValue || display.Primary == target.Primary.Value)
               && (!target.Width.HasValue || display.Width == target.Width.Value)
               && (!target.Height.HasValue || display.Height == target.Height.Value)
               && (!target.ScalePercent.HasValue || display.ScalePercent == target.ScalePercent.Value);
    }

    public static string DescribeDisplays(IEnumerable<DisplayRuntimeInfo> displays)
    {
        var builder = new StringBuilder();
        foreach (var display in displays)
        {
            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append(display.DeviceName)
                .Append(" [")
                .Append(display.Left)
                .Append(',')
                .Append(display.Top)
                .Append(' ')
                .Append(display.Width)
                .Append('x')
                .Append(display.Height)
                .Append("] working ")
                .Append(display.WorkingWidth)
                .Append('x')
                .Append(display.WorkingHeight)
                .Append(" scale ")
                .Append(display.ScalePercent)
                .Append('%');

            if (display.Primary)
            {
                builder.Append(" primary");
            }
        }

        return builder.ToString();
    }

    private static int GetScalePercent(Screen screen)
    {
        try
        {
            var point = new POINT
            {
                X = screen.Bounds.Left + (screen.Bounds.Width / 2),
                Y = screen.Bounds.Top + (screen.Bounds.Height / 2)
            };

            var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
            {
                return 0;
            }

            var result = GetDpiForMonitor(monitor, MonitorDpiType.EffectiveDpi, out var dpiX, out _);
            if (result != 0 || dpiX == 0)
            {
                return 0;
            }

            return (int)Math.Round((dpiX / 96d) * 100d);
        }
        catch
        {
            return 0;
        }
    }

    private const uint MonitorDefaultToNearest = 2;

    private enum MonitorDpiType
    {
        EffectiveDpi = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);
}
