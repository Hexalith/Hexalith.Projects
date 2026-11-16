namespace Hexalith.Projects.UI.Tests.Components;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Projects.Client;
using Hexalith.Projects.UI.Components.Pages;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Shouldly;
using Xunit;

public sealed class PrimaryReadRoutingTests : FrontComposerTestBase
{
    [Fact]
    public void MissingEvidenceRendersUnavailableWithoutCallingEitherReader()
    {
        Services.AddSingleton(ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions { Mode = "Supported" }));
        var page = Render<PrimaryProjectDiagnostics>(parameters => parameters.Add(component => component.ProjectId, "opaque-id"));
        page.Markup.ShouldContain("Project reads are unavailable");
        page.FindComponents<ProjectDiagnostics>().ShouldBeEmpty();
        page.FindComponents<LegacyProjectDiagnostics>().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Supported")]
    [InlineData("LegacyRollback")]
    public void SelectedRouteForwardsOpaqueProjectAndInspectionParameters(string mode)
    {
        Services.AddSingleton(Selection(mode));
        IProjectDetailSource supported = Substitute.For<IProjectDetailSource>();
        supported.InspectAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ProjectDetailLoadResult.FromFeedback(ProjectConsoleFeedback.FailClosed("safe_denial")));
        ILegacyProjectDetailSource legacy = Substitute.For<ILegacyProjectDetailSource>();
        legacy.GetProjectDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ProjectDetailLoadResult.FromFeedback(ProjectConsoleFeedback.FailClosed("safe_denial")));
        Services.AddSingleton(supported);
        Services.AddSingleton(legacy);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/projects/opaque%2Fhistorical-id?descriptive=true&quarantined=true");
        var page = Render<PrimaryProjectDiagnostics>(parameters => parameters
            .Add(component => component.ProjectId, "opaque/historical-id"));
        if (mode == "Supported")
        {
            ProjectDiagnostics child = page.FindComponent<ProjectDiagnostics>().Instance;
            child.ProjectId.ShouldBe("opaque/historical-id");
            child.Descriptive.ShouldBeTrue(); child.Quarantined.ShouldBeTrue();
            supported.Received(1).InspectAsync("opaque/historical-id", true, true, Arg.Any<CancellationToken>());
            legacy.ReceivedCalls().ShouldBeEmpty();
        }
        else
        {
            page.FindComponent<LegacyProjectDiagnostics>().Instance.ProjectId.ShouldBe("opaque/historical-id");
            legacy.Received(1).GetProjectDetailAsync("opaque/historical-id", Arg.Any<CancellationToken>());
            supported.ReceivedCalls().ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("Supported", "/")]
    [InlineData("Supported", "/projects")]
    [InlineData("Supported", "/projects/warnings")]
    [InlineData("LegacyRollback", "/")]
    [InlineData("LegacyRollback", "/projects")]
    [InlineData("LegacyRollback", "/projects/warnings")]
    [InlineData("Unavailable", "/")]
    [InlineData("Unavailable", "/projects")]
    [InlineData("Unavailable", "/projects/warnings")]
    public void PrimaryEntriesAndLayoutCallOnlyTheSelectedReaders(string mode, string path)
    {
        Services.AddSingleton(mode == "Unavailable" ? ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions()) : Selection(mode));
        var supported = Substitute.For<IProjectInventorySource>();
        var legacy = Substitute.For<ILegacyProjectInventorySource>();
        supported.ListProjectsAsync(null, Arg.Any<CancellationToken>()).Returns(ProjectInventoryLoadResult.FromRows([]));
        legacy.ListProjectsAsync(null, Arg.Any<CancellationToken>()).Returns(ProjectInventoryLoadResult.FromRows([]));
        Services.AddSingleton(supported); Services.AddSingleton(legacy);
        ComponentFactories.AddStub<Home>(); ComponentFactories.AddStub<LegacyHome>();
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        var home = Render<PrimaryHome>();
        home.FindComponents<Bunit.TestDoubles.Stub<Home>>().Count.ShouldBe(mode == "Supported" ? 1 : 0);
        home.FindComponents<Bunit.TestDoubles.Stub<LegacyHome>>().Count.ShouldBe(mode == "LegacyRollback" ? 1 : 0);
        if (mode == "Unavailable") home.Markup.ShouldContain("Project reads are unavailable");
        var layout = Render<Hexalith.Projects.UI.Components.Layout.MainLayout>();
        if (mode == "Supported")
        {
            layout.FindComponent<Hexalith.Projects.UI.Components.Shared.ProjectNavigation>();
            supported.Received(1).ListProjectsAsync(null, Arg.Any<CancellationToken>());
            legacy.ReceivedCalls().ShouldBeEmpty();
        }
        else if (mode == "LegacyRollback")
        {
            layout.FindComponent<Hexalith.Projects.UI.Components.Shared.LegacyProjectNavigation>();
            legacy.Received(1).ListProjectsAsync(null, Arg.Any<CancellationToken>());
            supported.ReceivedCalls().ShouldBeEmpty();
        }
        else { supported.ReceivedCalls().ShouldBeEmpty(); legacy.ReceivedCalls().ShouldBeEmpty(); }
    }

    private static ProjectsReadRouteSelection Selection(string mode)
    {
        string directory = Path.Combine(Path.GetTempPath(), "story67-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            byte[] xml = Encoding.UTF8.GetBytes("<assemblies><assembly total=\"1\" passed=\"1\" failed=\"0\" skipped=\"0\" errors=\"0\" not-run=\"0\"><collection><test name=\"routing-control\" result=\"Pass\" /></collection></assembly></assemblies>");
            File.WriteAllBytes(Path.Combine(directory, "native.xml"), xml);
            string hash = Convert.ToHexString(SHA256.HashData(xml));
            string revision = new('a', 40), contract = new('b', 64);
            JsonObject receipt = new()
            {
                ["sourceRevision"] = revision, ["contractHash"] = contract,
                ["normalizationRevision"] = ProjectReadCutoverGate.NormalizationRevision,
                ["contractParity"] = true, ["identityCompatibility"] = true,
                ["nativeReceipts"] = new JsonArray(new JsonObject { ["path"] = "native.xml", ["sha256"] = hash }),
                ["cases"] = new JsonArray(ProjectReadCutoverGate.Operations.SelectMany(operation => ProjectReadCutoverGate.Scenarios.Select(scenario => (JsonNode)new JsonObject
                {
                    ["operation"] = operation, ["scenario"] = scenario, ["executed"] = true, ["passed"] = true, ["skipped"] = false,
                    ["canonicalOutput"] = true, ["independentKeys"] = true, ["exactPositions"] = true, ["cursorScope"] = true,
                    ["isolation"] = true, ["supportedOnlyAssertions"] = true, ["noDomainWrites"] = true, ["nativeXmlHash"] = hash, ["nativeTestName"] = "routing-control",
                })).ToArray()),
                ["legacySecurity"] = new JsonArray(ProjectReadCutoverGate.Operations.Select(operation => (JsonNode)new JsonObject
                {
                    ["operation"] = operation, ["executed"] = true, ["passed"] = true, ["currentAuthority"] = true,
                    ["safeMetadata"] = true, ["safeDenial"] = true, ["noDomainWrites"] = true, ["nativeXmlHash"] = hash, ["nativeTestName"] = "routing-control",
                }).ToArray()),
            };
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(receipt);
            string path = Path.Combine(directory, "receipt.json");
            File.WriteAllBytes(path, bytes);
            ProjectsReadRouteSelection selected = ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions
            {
                Mode = mode, EvidencePath = path, EvidenceHash = Convert.ToHexString(SHA256.HashData(bytes)), SourceRevision = revision, ContractHash = contract,
            });
            selected.Mode.ShouldBe(mode);
            return selected;
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
