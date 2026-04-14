using CaptureRunner.Models;
using CaptureRunner.Services;
using Xunit;

namespace CaptureRunner.Tests;

public sealed class DisplayTopologyServiceTests
{
    [Fact]
    public void MatchesTarget_ReturnsTrue_WhenDeviceSizeAndScaleMatch()
    {
        var display = new DisplayRuntimeInfo
        {
            DeviceName = @"\\.\DISPLAY5",
            Primary = true,
            Left = 0,
            Top = 0,
            Width = 3840,
            Height = 2160,
            WorkingLeft = 0,
            WorkingTop = 0,
            WorkingWidth = 3840,
            WorkingHeight = 2100,
            ScalePercent = 125
        };

        var target = new DisplayTarget
        {
            Role = DisplayRole.Capture,
            DeviceName = @"\\.\DISPLAY5",
            Primary = true,
            Width = 3840,
            Height = 2160,
            ScalePercent = 125
        };

        Assert.True(DisplayTopologyService.MatchesTarget(display, target));
    }

    [Fact]
    public void MatchesTarget_ReturnsFalse_WhenScaleDiffers()
    {
        var display = new DisplayRuntimeInfo
        {
            DeviceName = @"\\.\DISPLAY5",
            Primary = true,
            Left = 0,
            Top = 0,
            Width = 3840,
            Height = 2160,
            WorkingLeft = 0,
            WorkingTop = 0,
            WorkingWidth = 3840,
            WorkingHeight = 2100,
            ScalePercent = 125
        };

        var target = new DisplayTarget
        {
            Role = DisplayRole.Capture,
            DeviceName = @"\\.\DISPLAY5",
            ScalePercent = 100
        };

        Assert.False(DisplayTopologyService.MatchesTarget(display, target));
    }
}
