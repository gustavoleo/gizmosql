using CaptureRunner.Models;
using CaptureRunner.Runner;
using CaptureRunner.Services;
using Xunit;

namespace CaptureRunner.Tests;

public sealed class ToolbarNavigationTests
{
    [Theory]
    [InlineData("DWH Wizard", "DWH Wizard", WindowTitleMatchMode.Exact, true)]
    [InlineData("DWH Wizard", "dwh wizard", WindowTitleMatchMode.Exact, true)]
    [InlineData("DWH Wizard - Analytics Creator", "DWH Wizard", WindowTitleMatchMode.Exact, false)]
    [InlineData("DWH Wizard - Analytics Creator", "DWH Wizard", WindowTitleMatchMode.Contains, true)]
    public void MatchesWindowTitle_RespectsConfiguredMatchMode(
        string candidate,
        string titleHint,
        WindowTitleMatchMode matchMode,
        bool expected)
    {
        Assert.Equal(expected, WindowService.MatchesWindowTitle(candidate, titleHint, matchMode));
    }

    [Fact]
    public void BuildNavigationNamesForValidation_ExcludesToolbarEntryForWindowLaunchPlans()
    {
        var plan = new ScreenPlan
        {
            ScreenName = "DWH Wizard",
            Hints =
            {
                TreePath = { "File" }
            },
            Actions =
            {
                new PlanAction
                {
                    Kind = PlanActionKind.WaitForWindow,
                    WindowTitle = "DWH Wizard"
                }
            }
        };

        var names = Validator.BuildNavigationNamesForValidation(plan);

        Assert.Contains("DWH Wizard", names);
        Assert.DoesNotContain("File", names);
    }
}
