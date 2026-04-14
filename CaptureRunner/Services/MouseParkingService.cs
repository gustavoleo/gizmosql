using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class MouseParkingService : IDisposable
{
    private readonly TimeSpan _pollInterval;
    private readonly DisplayRuntimeInfo? _operatorDisplay;
    private System.Threading.Timer? _timer;
    private Point? _operatorCenter;
    private int _isTicking;

    public MouseParkingService(DisplayRuntimeInfo? operatorDisplay, TimeSpan? pollInterval = null)
    {
        _operatorDisplay = operatorDisplay;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(250);
    }

    public void Start()
    {
        if (_timer is not null)
        {
            return;
        }

        if (_operatorDisplay is null)
        {
            Console.WriteLine("Mouse parking skipped: no certified operator display was configured or resolved.");
            return;
        }

        _operatorCenter = CenterInWorkingArea(_operatorDisplay);
        EnsureMouseOnOperatorDisplay(null);
        _timer = new System.Threading.Timer(EnsureMouseOnOperatorDisplay, null, _pollInterval, _pollInterval);
        Console.WriteLine($"Mouse parking active on {_operatorDisplay.DeviceName}; cursor will be re-centered there when it leaves that display.");
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void EnsureMouseOnOperatorDisplay(object? state)
    {
        if (_operatorDisplay is null || _operatorCenter is null)
        {
            return;
        }

        if (Interlocked.Exchange(ref _isTicking, 1) != 0)
        {
            return;
        }

        try
        {
            var currentScreen = Screen.FromPoint(Cursor.Position);
            if (currentScreen.DeviceName.Equals(_operatorDisplay.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Cursor.Position = _operatorCenter.Value;
        }
        catch
        {
            // Best effort only.
        }
        finally
        {
            Interlocked.Exchange(ref _isTicking, 0);
        }
    }

    private static Point CenterInWorkingArea(DisplayRuntimeInfo display)
    {
        return new Point(
            display.WorkingLeft + (display.WorkingWidth / 2),
            display.WorkingTop + (display.WorkingHeight / 2));
    }
}
