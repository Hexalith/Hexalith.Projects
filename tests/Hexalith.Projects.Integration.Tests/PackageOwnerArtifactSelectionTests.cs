extern alias PackageFixtureHost;

namespace Hexalith.Projects.Integration.Tests;

using System.Reflection;

using Hexalith.Projects.PackageFixtures;
using PackageHost = PackageFixtureHost::Hexalith.Projects.PackageFixtures.AppHost;

using Shouldly;

using Xunit;

/// <summary>Preserves exact selected-owner output discovery without an executable fallback.</summary>
public sealed class PackageOwnerArtifactSelectionTests
{
    private const string Artifact = "Hexalith.Folders.Server.dll";

    /// <summary>One selected owner image is sufficient without the obsolete EventStore executable.</summary>
    [Fact]
    public async Task SelectsTheExactOwnerArtifactWithoutAnotherExecutable()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        string directory = scratch.File("Release");
        Directory.CreateDirectory(Path.Combine(directory, "net11.0"));
        string selected = Path.Combine(directory, "net11.0", Artifact);
        File.WriteAllText(selected, "selected artifact");

        Select(directory).ShouldBe(selected);
        File.Exists(Path.Combine(directory, "Hexalith.Folders.EventStore.dll")).ShouldBeFalse();
    }

    /// <summary>Missing selected output is diagnosed without searching a different owner's directory.</summary>
    [Fact]
    public async Task MissingSelectedOwnerDoesNotUseAnUnrelatedOutput()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        Directory.CreateDirectory(scratch.File("other-owner"));
        File.WriteAllText(scratch.File("other-owner/" + Artifact), "unrelated artifact");

        TargetInvocationException error = Should.Throw<TargetInvocationException>(() => Select(scratch.File("missing")));
        InvalidOperationException failure = error.InnerException.ShouldBeOfType<InvalidOperationException>();
        failure.Message.ShouldContain(Artifact);
        failure.Message.ShouldContain("found 0");
        failure.Message.ShouldContain(scratch.File("missing"));
        failure.Message.ShouldContain("Build the selected");
    }

    /// <summary>Multiple framework/output candidates remain a refusal rather than choosing the first.</summary>
    [Fact]
    public async Task AmbiguousSelectedOutputIsRefused()
    {
        FixtureScratch scratch = new(false);
        await using var cleanup = scratch.ConfigureAwait(true);
        string directory = scratch.File("Release");
        foreach (string framework in new[] { "net10.0", "net11.0" })
        {
            Directory.CreateDirectory(Path.Combine(directory, framework));
            File.WriteAllText(Path.Combine(directory, framework, Artifact), "candidate artifact");
        }

        TargetInvocationException error = Should.Throw<TargetInvocationException>(() => Select(directory));
        string message = error.InnerException.ShouldBeOfType<InvalidOperationException>().Message;
        message.ShouldContain("found 2");
        message.ShouldContain(directory);
        message.ShouldContain(Path.Combine("net10.0", Artifact));
        message.ShouldContain(Path.Combine("net11.0", Artifact));
        message.ShouldContain("Remove duplicate");
    }

    private static string Select(string directory)
    {
        Type infrastructure = typeof(PackageHost.PackageActualOwnerComposition).Assembly.GetType(
            "Hexalith.Projects.PackageFixtures.AppHost.PackageActualOwnerInfrastructure", throwOnError: true)!;
        MethodInfo method = infrastructure.GetMethod("FindArtifact", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (string)method.Invoke(null, [directory, Artifact])!;
    }
}
