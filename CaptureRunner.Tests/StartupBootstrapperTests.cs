using CaptureRunner.Models;
using CaptureRunner.Services;
using Xunit;

namespace CaptureRunner.Tests;

public sealed class StartupBootstrapperTests
{
    [Fact]
    public void CreateEffective_UsesRepositoryOverrideForValidationDefaults()
    {
        var profile = new BootstrapProfile
        {
            RepositoryName = string.Empty,
            LoginMode = string.Empty,
            LoginWindowTitles = new List<string>(),
            LoginPositiveActions = new List<string>(),
            RepositorySelectorAutomationIds = new List<string>(),
            RepositoryConfirmActions = new List<string>(),
            RepositoryValidation = new RepositoryValidationProfile
            {
                WindowTitleContains = string.Empty,
                VisibleTextContains = string.Empty
            }
        };

        var effective = BootstrapProfile.CreateEffective(profile, "Northwind");

        Assert.Equal("Northwind", effective.RepositoryName);
        Assert.Equal("saved-password", effective.LoginMode);
        Assert.Equal("Northwind", effective.RepositoryValidation.WindowTitleContains);
        Assert.Equal("Northwind", effective.RepositoryValidation.VisibleTextContains);
        Assert.Contains("Login", effective.LoginWindowTitles);
        Assert.Contains("OK", effective.LoginPositiveActions);
        Assert.True(
            effective.LoginPositiveActions.IndexOf("Connect") < effective.LoginPositiveActions.IndexOf("OK"),
            "Specific login actions should be tried before generic OK buttons.");
        Assert.Contains("cmbName", effective.RepositorySelectorAutomationIds);
        Assert.Contains("OK", effective.RepositoryConfirmActions);
    }

    [Theory]
    [InlineData(true, false, false, false, true, 0)]
    [InlineData(true, false, false, false, false, 2)]
    [InlineData(false, false, false, false, false, 1)]
    [InlineData(true, true, false, false, false, 1)]
    [InlineData(true, true, true, false, false, 1)]
    [InlineData(true, true, true, true, false, 2)]
    public void GetExitCode_MapsBootstrapResultToContract(
        bool applicationStartedOrAttached,
        bool loginDetected,
        bool loginAttempted,
        bool loginSuccess,
        bool repositoryVerified,
        int expectedExitCode)
    {
        var report = new StartupStateReport
        {
            ApplicationStartedOrAttached = applicationStartedOrAttached,
            LoginDetected = loginDetected,
            LoginAttempted = loginAttempted,
            LoginSuccess = loginSuccess,
            RepositoryVerified = repositoryVerified
        };

        Assert.Equal(expectedExitCode, StartupBootstrapper.GetExitCode(report));
    }
}
