using System.Drawing;
using CaptureRunner.Models;
using CaptureRunner.Services;
using Xunit;

namespace CaptureRunner.Tests;

public sealed class WindowPlacementServiceTests
{
    [Fact]
    public void VerifyPlacement_ReturnsSuccess_WhenWindowIsContainedOnTarget()
    {
        var target = CreateDisplay(@"\\.\DISPLAY5", 0, 0, 3840, 2160, 0, 0, 3840, 2100);
        var other = CreateDisplay(@"\\.\DISPLAY2", -1080, 0, 1080, 1920, -1080, 0, 1080, 1872);
        var window = new Rectangle(50, 50, 3700, 2000);

        var verification = WindowPlacementService.VerifyPlacement(window, target, new[] { target, other });

        Assert.True(verification.Success);
        Assert.True(verification.CenterOnTarget);
        Assert.True(verification.ContainedInTarget);
        Assert.False(verification.OverlappedOtherDisplay);
    }

    [Fact]
    public void VerifyPlacement_ReturnsFailure_WhenWindowOverlapsAnotherDisplay()
    {
        var target = CreateDisplay(@"\\.\DISPLAY5", 0, 0, 3840, 2160, 0, 0, 3840, 2100);
        var other = CreateDisplay(@"\\.\DISPLAY3", 3840, 0, 1920, 1080, 3840, 0, 1920, 1032);
        var window = new Rectangle(3000, 50, 1600, 1000);

        var verification = WindowPlacementService.VerifyPlacement(window, target, new[] { target, other });

        Assert.False(verification.Success);
        Assert.True(verification.CenterOnTarget);
        Assert.False(verification.ContainedInTarget);
        Assert.True(verification.OverlappedOtherDisplay);
    }

    [Fact]
    public void VerifyPlacement_ReturnsFailure_WhenWindowCenterIsOffTarget()
    {
        var target = CreateDisplay(@"\\.\DISPLAY5", 0, 0, 3840, 2160, 0, 0, 3840, 2100);
        var other = CreateDisplay(@"\\.\DISPLAY2", -1080, 0, 1080, 1920, -1080, 0, 1080, 1872);
        var window = new Rectangle(-700, 50, 1200, 1000);

        var verification = WindowPlacementService.VerifyPlacement(window, target, new[] { target, other });

        Assert.False(verification.Success);
        Assert.False(verification.CenterOnTarget);
    }

    private static DisplayRuntimeInfo CreateDisplay(
        string deviceName,
        int left,
        int top,
        int width,
        int height,
        int workingLeft,
        int workingTop,
        int workingWidth,
        int workingHeight)
    {
        return new DisplayRuntimeInfo
        {
            DeviceName = deviceName,
            Primary = deviceName == @"\\.\DISPLAY5",
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            WorkingLeft = workingLeft,
            WorkingTop = workingTop,
            WorkingWidth = workingWidth,
            WorkingHeight = workingHeight,
            ScalePercent = 100
        };
    }
}
