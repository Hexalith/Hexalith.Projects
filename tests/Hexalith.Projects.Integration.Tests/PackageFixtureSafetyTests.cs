extern alias PackageFixtureHost;

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;

using global::Aspire.Hosting;

using Hexalith.Projects.PackageFixtures;
using PackageHost = PackageFixtureHost::Hexalith.Projects.PackageFixtures.AppHost;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

/// <summary>Checks fixture refusal, bounded readiness and owned cleanup without a live backend.</summary>
public sealed class PackageFixtureSafetyTests
{
    /// <summary>Aggregate bytes and stale or foreign administrator models cannot satisfy bootstrap readiness.</summary>
    [Theory]
    [InlineData("projection:global-administrators:singleton", "actor", "sequence:1", true)]
    [InlineData("AggregateActor||global-administrators", "actor", "sequence:1", false)]
    [InlineData("projection:global-administrators:singleton", "foreign", "sequence:1", false)]
    [InlineData("projection:global-administrators:singleton", "actor", null, false)]
    public void BootstrapRequiresItsPersistedAdministratorModel(string key, string actor, string? version, bool expected)
    {
        string data = JsonSerializer.Serialize(new { administrators = new[] { actor }, projectionVersion = version,
            projectedAt = DateTimeOffset.UtcNow });
        string observation = JsonSerializer.Serialize(new[] { new { key, data } });
        FixtureProductChecks.HasBootstrapProjection(observation, "actor").ShouldBe(expected);
    }

    /// <summary>Known redacted failure categories remain useful without exposing arbitrary diagnostic text.</summary>
    [Theory]
    [InlineData("Protected data diagnostic details were redacted. ReasonCode=protected-data-diagnostic-redacted; Stage=Processing.",
        "failureReasonCode=protected-data-diagnostic-redacted,failureStage=Processing")]
    [InlineData("Protected data diagnostic details were redacted. ReasonCode=logical-event-read-rejected; Stage=EventsStored.",
        "failureReasonCode=logical-event-read-rejected,failureStage=EventsStored")]
    [InlineData("Protected data diagnostic details were redacted. ReasonCode=private-token; Stage=Processing.", "")]
    [InlineData("Protected data diagnostic details were redacted. ReasonCode=protected-data-diagnostic-redacted; Stage=private-token.", "")]
    [InlineData("Protected data diagnostic details were redacted. ReasonCode=protected-data-diagnostic-redacted; Stage=Processing. private-token", "")]
    [InlineData("private-token with diagnostic details", "")]
    public void FailureDiagnosticsOnlyExposeKnownCategories(string reason, string expected)
    {
        FixtureDiagnostics.Status(new JsonObject { ["failureReason"] = reason }, ["private-token"]).ShouldBe(expected);
    }

    /// <summary>A fixture-protected value is suppressed even when it matches a known category.</summary>
    [Fact]
    public void FailureDiagnosticsKeepProtectedValuesSuppressed()
    {
        FixtureDiagnostics.Status(new JsonObject
        {
            ["failureReason"] = "Protected data diagnostic details were redacted. ReasonCode=protected-data-diagnostic-redacted; Stage=Processing.",
        }, ["Processing"]).ShouldBeEmpty();
    }

    /// <summary>A package fixture cannot activate in a production AppHost.</summary>
    [Fact]
    public void ProductionActivationIsRefusedBeforeReadingCredentials()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { Args = ["--environment", "Production"], DisableDashboard = true });
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [PackageHost.PackageCompatibilityComposition.EnabledConfigurationKey] = "true" });
        Should.Throw<InvalidOperationException>(() => PackageHost.PackageCompatibilityComposition.Configure(builder))
            .Message.ShouldContain("Development");
    }

    /// <summary>A missing Release output is an error rather than a child build.</summary>
    [Fact]
    public void MissingReleaseOutputIsRefused()
    {
        PackageHost.PrebuiltMetadata<MissingPackageFixtureMetadata> metadata = new();
        metadata.SuppressBuild.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => _ = metadata.ProjectPath).Message.ShouldContain("Release output");
    }

    /// <summary>Unready resources terminate within the selected deadline and identify the failed stage.</summary>
    [Fact]
    public async Task ReadinessDeadlineFailsClosed()
    {
        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            FixtureReadiness.AwaitAsync(_ => Task.FromResult(false), "durable bootstrap", TimeSpan.FromMilliseconds(10), CancellationToken.None));
        failure.Message.ShouldContain("durable bootstrap");
        failure.InnerException.ShouldBeAssignableTo<OperationCanceledException>();
    }

    /// <summary>Failed starts still clean the exact recorded container, while cleanup failures make the lane red.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartFailureRetainsCustodyAndCleanupFailureIsReported(bool cleanupFails)
    {
        const string id = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        FixtureScratch? scratch = null;
        List<string[]> commands = [];
        async Task<string> Docker(string[] arguments, CancellationToken cancellationToken)
        {
            commands.Add(arguments);
            switch (arguments[0])
            {
                case "create":
                    await File.WriteAllTextAsync(arguments[Array.IndexOf(arguments, "--cidfile") + 1], id, cancellationToken).ConfigureAwait(false);
                    return id;
                case "start": throw new InvalidOperationException("controlled start failure");
                case "inspect": return JsonSerializer.Serialize(new Dictionary<string, string>
                    { ["hexalith.projects-package-fixtures"] = scratch!.Isolation });
                case "rm" when cleanupFails: throw new InvalidOperationException("controlled cleanup failure");
                default: return string.Empty;
            }
        }
        scratch = new FixtureScratch(actualOwners: false, Docker);
        string directory = scratch.Directory;
        try
        {
            await Should.ThrowAsync<InvalidOperationException>(() => scratch.ContainerAsync("redis", ["redis:6"], CancellationToken.None));
            if (cleanupFails) await Should.ThrowAsync<AggregateException>(async () => await scratch.DisposeAsync().ConfigureAwait(false));
            else await scratch.DisposeAsync();
            commands.Single(arguments => arguments[0] == "rm").ShouldBe(new[] { "rm", "--force", "--volumes", id });
            Directory.Exists(directory).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
