using System.Text.Json.Serialization;

namespace CaptureRunner.Models;

public sealed class BootstrapProfile
{
    public static readonly IReadOnlyList<string> DefaultLoginWindowTitles =
    [
        "Login",
        "LoginWindow",
        "AnalyticsCreator"
    ];

    public static readonly IReadOnlyList<string> DefaultLoginPositiveActions =
    [
        "Login",
        "Connect",
        "Continue",
        "Sign in",
        "OK"
    ];

    public static readonly IReadOnlyList<string> DefaultRepositorySelectorAutomationIds =
    [
        "cmbName"
    ];

    public static readonly IReadOnlyList<string> DefaultRepositoryConfirmActions =
    [
        "OK",
        "Connect",
        "Login",
        "Continue",
        "Sign in"
    ];

    [JsonPropertyName("repository_name")]
    public string RepositoryName { get; set; } = "Northwind";

    [JsonPropertyName("login_mode")]
    public string LoginMode { get; set; } = "saved-password";

    [JsonPropertyName("login_window_titles")]
    public List<string> LoginWindowTitles { get; set; } = new(DefaultLoginWindowTitles);

    [JsonPropertyName("login_positive_actions")]
    public List<string> LoginPositiveActions { get; set; } = new(DefaultLoginPositiveActions);

    [JsonPropertyName("repository_selector_automation_ids")]
    public List<string> RepositorySelectorAutomationIds { get; set; } = new(DefaultRepositorySelectorAutomationIds);

    [JsonPropertyName("repository_confirm_actions")]
    public List<string> RepositoryConfirmActions { get; set; } = new(DefaultRepositoryConfirmActions);

    [JsonPropertyName("repository_validation")]
    public RepositoryValidationProfile RepositoryValidation { get; set; } = new();

    public static BootstrapProfile CreateEffective(BootstrapProfile? profile, string? repositoryNameOverride)
    {
        var effective = profile ?? new BootstrapProfile();
        if (!string.IsNullOrWhiteSpace(repositoryNameOverride))
        {
            effective.RepositoryName = repositoryNameOverride.Trim();
        }

        if (string.IsNullOrWhiteSpace(effective.RepositoryName))
        {
            effective.RepositoryName = "Northwind";
        }

        if (string.IsNullOrWhiteSpace(effective.LoginMode))
        {
            effective.LoginMode = "saved-password";
        }

        if (effective.LoginWindowTitles.Count == 0)
        {
            effective.LoginWindowTitles.AddRange(DefaultLoginWindowTitles);
        }

        if (effective.LoginPositiveActions.Count == 0)
        {
            effective.LoginPositiveActions.AddRange(DefaultLoginPositiveActions);
        }

        if (effective.RepositorySelectorAutomationIds.Count == 0)
        {
            effective.RepositorySelectorAutomationIds.AddRange(DefaultRepositorySelectorAutomationIds);
        }

        if (effective.RepositoryConfirmActions.Count == 0)
        {
            effective.RepositoryConfirmActions.AddRange(DefaultRepositoryConfirmActions);
        }

        if (string.IsNullOrWhiteSpace(effective.RepositoryValidation.WindowTitleContains))
        {
            effective.RepositoryValidation.WindowTitleContains = effective.RepositoryName;
        }

        if (string.IsNullOrWhiteSpace(effective.RepositoryValidation.VisibleTextContains))
        {
            effective.RepositoryValidation.VisibleTextContains = effective.RepositoryName;
        }

        return effective;
    }
}

public sealed class RepositoryValidationProfile
{
    [JsonPropertyName("window_title_contains")]
    public string? WindowTitleContains { get; set; } = "Northwind";

    [JsonPropertyName("visible_text_contains")]
    public string? VisibleTextContains { get; set; } = "Northwind";
}
