using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using CaptureRunner.Models;
using CaptureRunner.Runner;
using CaptureRunner.Services;
using Xunit;

namespace CaptureRunner.Tests;

public sealed class UiMappingTests
{
    [Theory]
    [InlineData("File", null, "TabItem", true, "safe_navigation", "safe", "ready", true)]
    [InlineData("Delete", null, "Button", true, "destructive", "destructive", "blocked", false)]
    [InlineData(null, "txtName", "Edit", true, "input_edit", "data_entry", "review", false)]
    [InlineData("Help", null, "MenuItem", true, "safe_navigation", "safe", "ready", true)]
    [InlineData("Connect", null, "Button", false, "blocked", "blocked", "blocked", false)]
    [InlineData("Edit", null, "Button", true, "safe_detail_open", "safe", "ready", true)]
    [InlineData("DWH Wizard", null, "Button", true, "safe_wizard_entry", "safe", "ready", true)]
    [InlineData("Move", null, "MenuItem", true, "destructive", "destructive", "blocked", false)]
    public void Classify_ReturnsExpectedAutomationReadiness(
        string? name,
        string? automationId,
        string controlType,
        bool isEnabled,
        string expectedKind,
        string expectedRisk,
        string expectedReadiness,
        bool expectedRouteCandidate)
    {
        var supportedPatterns = new[] { "Invoke" };

        var classified = UiInteractionClassifier.Classify(
            name,
            automationId,
            controlType,
            isEnabled,
            supportedPatterns);

        Assert.Equal(expectedKind, classified.Kind);
        Assert.Equal(expectedRisk, classified.Risk);
        Assert.Equal(expectedReadiness, classified.Readiness);
        Assert.Equal(expectedRouteCandidate, classified.RouteCandidate);
    }

    [Fact]
    public void UiMap_RoundTripsCoreFields()
    {
        var map = new UiMap
        {
            RepositoryName = "Northwind",
            AcceptedThreshold = 0.98,
            RemainingQueuedRoutes =
            {
                new UiRoute
                {
                    RouteType = "safe_dialog_open",
                    RouteText = "Help > About"
                }
            },
            Screens =
            {
                new UiScreen
                {
                    ScreenId = "shell",
                    ScreenName = "Current Northwind shell",
                    Readiness = new UiReadiness
                    {
                        Status = "accepted",
                        Confidence = "high"
                    },
                    AvailableInteractions =
                    {
                        new UiInteraction
                        {
                            InteractionId = "tab-file",
                            Name = "File",
                            ControlType = "TabItem",
                            Risk = "safe",
                            Readiness = "ready",
                            RouteCandidate = true
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(map);
        var roundTrip = JsonSerializer.Deserialize<UiMap>(json);

        Assert.NotNull(roundTrip);
        Assert.Equal("Northwind", roundTrip.RepositoryName);
        Assert.Single(roundTrip.RemainingQueuedRoutes);
        Assert.Equal("Help > About", roundTrip.RemainingQueuedRoutes[0].RouteText);
        Assert.Single(roundTrip.Screens);
        Assert.Equal("accepted", roundTrip.Screens[0].Readiness.Status);
        Assert.True(roundTrip.Screens[0].AvailableInteractions[0].RouteCandidate);
    }

    [Fact]
    public void BuildRejectedRoutesArtifact_ProjectsRejectedScreens()
    {
        var screens = new[]
        {
            new UiScreen
            {
                ScreenId = "accepted-screen",
                ScreenName = "Accepted",
                Signature = "accepted-signature",
                Route = new UiRoute
                {
                    RouteText = "Accepted"
                },
                Readiness = new UiReadiness
                {
                    Status = "accepted"
                }
            },
            new UiScreen
            {
                ScreenId = "rejected-screen",
                ScreenName = "Find on diagram",
                Signature = "find-on-diagram",
                RequiredState = "Northwind repository",
                Route = new UiRoute
                {
                    RouteText = "Find on diagram"
                },
                Readiness = new UiReadiness
                {
                    Status = "rejected",
                    BlockerType = "source-code",
                    Reasons =
                    {
                        "Candidate 'Find on diagram' was not found in the active UIA tree."
                    }
                }
            }
        };

        var artifact = UiMappingRunner.BuildRejectedRoutesArtifact(screens);

        Assert.Single(artifact);
        Assert.Equal("rejected-screen", artifact[0].ScreenId);
        Assert.Equal("Find on diagram", artifact[0].Route.RouteText);
        Assert.Equal("source-code", artifact[0].BlockerType);
        Assert.Equal("Northwind repository", artifact[0].RequiredState);
        Assert.Contains("not found", artifact[0].Reasons[0]);
    }

    [Fact]
    public void RoutePromotionSummary_PrefersRejectedRoutesOverQueuedRoutes()
    {
        var service = new RoutePromotionSummaryService();
        var map = new UiMap
        {
            RemainingQueuedRoutes =
            {
                new UiRoute
                {
                    RouteType = "safe_navigation",
                    RouteText = "Queued Route",
                    Steps =
                    {
                        new UiRouteStep
                        {
                            Kind = "Button",
                            Name = "Queued Route",
                            ControlType = "Button",
                            Action = "invoke"
                        }
                    }
                }
            }
        };
        var rejectedRoutes = new[]
        {
            new UiRejectedRoute
            {
                ScreenId = "rejected-screen",
                ScreenName = "Find on diagram",
                Route = new UiRoute
                {
                    RouteText = "Find on diagram",
                    Steps =
                    {
                        new UiRouteStep
                        {
                            Kind = "Button",
                            Name = "Find on diagram",
                            ControlType = "Button",
                            Action = "invoke"
                        }
                    }
                },
                Reasons = { "not found" }
            }
        };

        var summary = service.Build(map, rejectedRoutes, Array.Empty<UiRoute>());

        Assert.Equal("rejected_routes", summary.PromotionSource);
        Assert.Single(summary.CandidateGroups);
        Assert.Equal("Find on diagram", summary.CandidateGroups[0].SourceRouteTexts[0]);
    }

    [Fact]
    public void RoutePromotionSummary_SuggestsConfiguredAndAcceptedParentRoutes()
    {
        var service = new RoutePromotionSummaryService();
        var map = new UiMap
        {
            Screens =
            {
                new UiScreen
                {
                    ScreenId = "file-screen",
                    ScreenName = "File",
                    Route = new UiRoute
                    {
                        RouteType = "safe_navigation",
                        RouteText = "File"
                    },
                    Readiness = new UiReadiness
                    {
                        Status = "accepted"
                    },
                    AvailableInteractions =
                    {
                        new UiInteraction
                        {
                            Name = "Find on diagram",
                            ControlType = "Button",
                            IsEnabled = true,
                            RouteCandidate = true,
                            Risk = "safe",
                            Readiness = "ready"
                        }
                    }
                }
            }
        };
        var rejectedRoutes = new[]
        {
            new UiRejectedRoute
            {
                ScreenId = "rejected-screen",
                ScreenName = "Find on diagram",
                Route = new UiRoute
                {
                    RouteText = "Find on diagram",
                    Steps =
                    {
                        new UiRouteStep
                        {
                            Kind = "Button",
                            Name = "Find on diagram",
                            ControlType = "Button",
                            Action = "invoke"
                        }
                    }
                },
                Reasons = { "not found" }
            }
        };
        var configuredRoutes = new[]
        {
            new UiRoute
            {
                RouteType = "safe_dialog_open",
                RouteText = "File > Find on diagram",
                Steps =
                {
                    new UiRouteStep
                    {
                        Kind = "bootstrap",
                        Name = "Northwind",
                        Action = "verify"
                    },
                    new UiRouteStep
                    {
                        Kind = "TabItem",
                        Name = "File",
                        ControlType = "TabItem",
                        Action = "select"
                    },
                    new UiRouteStep
                    {
                        Kind = "Button",
                        Name = "Find on diagram",
                        ControlType = "Button",
                        Action = "invoke"
                    }
                }
            }
        };

        var summary = service.Build(map, rejectedRoutes, configuredRoutes);

        Assert.Single(summary.CandidateGroups);
        Assert.Equal("Find on diagram", summary.CandidateGroups[0].LeafName);
        Assert.Contains(
            summary.CandidateGroups[0].SuggestedRecipeRoutes,
            suggestion => suggestion.CandidateRouteText == "File > Find on diagram" && suggestion.AlreadyConfigured);
    }

    [Fact]
    public void PngEvidenceValidator_AcceptsExpectedDpiMetadata()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CaptureRunnerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "sample.png");

        try
        {
            using (var bitmap = new Bitmap(4, 4))
            {
                bitmap.SetResolution(600, 600);
                bitmap.Save(path, ImageFormat.Png);
            }

            var result = new PngEvidenceValidator().Validate(path, 600);

            Assert.True(result.Exists);
            Assert.True(result.Valid, result.Note);
            Assert.Equal(4, result.Width);
            Assert.Equal(4, result.Height);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
