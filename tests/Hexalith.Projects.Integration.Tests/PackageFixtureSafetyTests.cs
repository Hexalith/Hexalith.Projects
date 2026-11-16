extern alias PackageFixtureHost;

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;

using global::Aspire.Hosting;

using Hexalith.Projects.PackageFixtures;
using PackageHost = PackageFixtureHost::Hexalith.Projects.PackageFixtures.AppHost;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

/// <summary>Checks fixture refusal, bounded readiness and owned cleanup without a live backend.</summary>
public sealed class PackageFixtureSafetyTests
{
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
