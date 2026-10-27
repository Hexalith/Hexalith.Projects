using System.Text.Json;
using System.Reflection;

using Hexalith.Builds.Tooling.Runtime;
using Hexalith.Builds.Tooling.Workspace;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>Exercises the actual consumer project XML through Builds' public mapping imports.</summary>
public sealed class PlatformEnrollmentMappingTest
{
    private static readonly IReadOnlyDictionary<string, string[]> Dependencies = new Dictionary<string, string[]>
    {
        ["src/Hexalith.Conversations.Contracts"] = ["Hexalith.Commons.Serialization"],
        ["src/Hexalith.Conversations.Client"] = ["Hexalith.Commons.Http", "Hexalith.EventStore.Contracts"],
        ["src/Hexalith.Conversations"] = ["Hexalith.EventStore.Client", "Hexalith.EventStore.Contracts"],
        ["src/Hexalith.Conversations.Server"] = ["Hexalith.Commons.Diagnostics", "Hexalith.Commons.Publication", "Hexalith.Commons.Serialization", "Hexalith.Commons.TenantAccess", "Hexalith.EventStore.Client", "Hexalith.EventStore.Contracts", "Hexalith.EventStore.DomainService", "Hexalith.Tenants.Client", "Hexalith.Tenants.Contracts"],
        ["tests/Hexalith.Conversations.Contracts.Tests"] = ["Hexalith.Builds.Tooling", "Hexalith.Commons.Serialization"],
        ["tests/Hexalith.Conversations.Tests"] = ["Hexalith.EventStore.Client", "Hexalith.EventStore.Contracts"],
        ["tests/Hexalith.Conversations.Server.Tests"] = ["Hexalith.Commons.ServiceDefaults", "Hexalith.EventStore.Client", "Hexalith.EventStore.Contracts", "Hexalith.EventStore.Testing", "Hexalith.Tenants.Client", "Hexalith.Tenants.Contracts"],
        ["tests/Hexalith.Conversations.IntegrationTests"] = ["Hexalith.EventStore.Client", "Hexalith.EventStore.Server", "Hexalith.EventStore.Testing", "Hexalith.EventStore.Testing.Integration"],
    };

    /// <summary>Evaluates every eligible project in source/Debug and package/Release without compiling Release assets.</summary>
    /// <param name="mode">The mode selected by the materialized mapping.</param>
    /// <returns>A task for the evaluations.</returns>
    [Theory]
    [InlineData(WorkspaceMode.Source)]
    [InlineData(WorkspaceMode.Package)]
    public async Task EvaluatedReferencesUseOnlyMappedIdentities(WorkspaceMode mode)
    {
        string root = CreateWorkspace();
        try
        {
            SourceMapping mapping = Mapping(root, mode);
            string run = Path.Combine(root, "run");
            await SourceMappingMaterializer.WriteAsync(mapping, run, TestContext.Current.CancellationToken);
            foreach ((string directory, string[] expected) in Dependencies)
            {
                CompositionProcessResult result = await EvaluateAsync(root, directory, mapping, run);
                result.ExitCode.ShouldBe(0, result.Output + result.ErrorOutput);
                using JsonDocument output = JsonDocument.Parse(result.Output);
                JsonElement properties = output.RootElement.GetProperty("Properties");
                properties.GetProperty("Configuration").GetString().ShouldBe(mode == WorkspaceMode.Source ? "Debug" : "Release");
                string[] projects = Items(output, "ProjectReference");
                string[] packages = Items(output, "PackageReference");
                string[] externalProjects = projects.Where(path => !Path.GetFileNameWithoutExtension(path).StartsWith("Hexalith.Conversations", StringComparison.Ordinal)).ToArray();
                if (mode == WorkspaceMode.Source)
                {
                    externalProjects.ShouldBe(expected.Select(identity => DependencyProject(root, identity, "selected")), ignoreOrder: true);
                    packages.ShouldNotContain(package => expected.Contains(package, StringComparer.Ordinal));
                }
                else
                {
                    externalProjects.ShouldBeEmpty();
                    packages.Where(package => package.StartsWith("Hexalith.", StringComparison.Ordinal))
                        .ShouldBe(expected.Where(identity => identity != "Hexalith.Builds.Tooling"), ignoreOrder: true);
                }

                if (directory == "src/Hexalith.Conversations.Client")
                    projects.ShouldContain(path => Path.GetFileNameWithoutExtension(path) == "Hexalith.Conversations.Contracts");
            }
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, true);
        }
    }

    /// <summary>An omitted dependency refuses instead of selecting either competing sibling or a package.</summary>
    /// <returns>A task for the evaluation.</returns>
    [Fact]
    public async Task MissingDependencyMappingFailsExplicitly()
    {
        string root = CreateWorkspace();
        try
        {
            SourceMapping original = Mapping(root, WorkspaceMode.Source);
            SourceMapping mapping = original with { Entries = original.Entries.Where(entry => entry.Identity != "Hexalith.Commons").ToArray() };
            string run = Path.Combine(root, "run");
            await SourceMappingMaterializer.WriteAsync(mapping, run, TestContext.Current.CancellationToken);
            CompositionProcessResult result = await EvaluateAsync(root, "src/Hexalith.Conversations.Contracts", mapping, run);
            result.ExitCode.ShouldNotBe(0);
            (result.Output + result.ErrorOutput).ShouldContain("HXW");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, true);
        }
    }

    /// <summary>A caller cannot replace an exact mapped root with a plausible competing checkout.</summary>
    /// <returns>A task for the evaluation.</returns>
    [Fact]
    public async Task MismatchedDependencyMappingFailsExplicitly()
    {
        string root = CreateWorkspace();
        try
        {
            SourceMapping mapping = Mapping(root, WorkspaceMode.Source);
            string run = Path.Combine(root, "run");
            await SourceMappingMaterializer.WriteAsync(mapping, run, TestContext.Current.CancellationToken);
            CompositionProcessResult result = await EvaluateAsync(root, "src/Hexalith.Conversations.Contracts", mapping, run,
                "-p:HexalithCommonsRoot=" + Path.Combine(root, "references", "Hexalith.Commons"));
            result.ExitCode.ShouldNotBe(0);
            (result.Output + result.ErrorOutput).ShouldContain("HXW");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, true);
        }
    }

    /// <summary>Even a complete conventional sibling tree cannot supply an absent tool mapping.</summary>
    /// <returns>A task for the evaluation.</returns>
    [Fact]
    public async Task AbsentMappingDoesNotFallBackToPackages()
    {
        string root = CreateWorkspace();
        try
        {
            CompositionProcessResult result = await CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
                ["msbuild", Project(root, "src/Hexalith.Conversations.Contracts"), "-target:PrepareForBuild", "-nologo"], root, null),
                TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
            result.ExitCode.ShouldNotBe(0);
            (result.Output + result.ErrorOutput).ShouldContain("requires the Platform tool-selected source mapping");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, true);
        }
    }

    /// <summary>Selection properties and a props-only import cannot replace the producer's validation imports.</summary>
    /// <param name="mode">The selected source or package mode.</param>
    /// <param name="propsOnly">Whether real generated selection props are supplied without validation targets.</param>
    /// <returns>A task for the rejected partial-environment check.</returns>
    [Theory]
    [InlineData(WorkspaceMode.Source, false)]
    [InlineData(WorkspaceMode.Package, false)]
    [InlineData(WorkspaceMode.Source, true)]
    [InlineData(WorkspaceMode.Package, true)]
    public async Task PartialMappingEnvironmentCannotBypassProducerValidation(WorkspaceMode mode, bool propsOnly)
    {
        string root = CreateWorkspace();
        try
        {
            SourceMapping mapping = Mapping(root, mode);
            string run = Path.Combine(root, "run");
            await SourceMappingMaterializer.WriteAsync(mapping, run, TestContext.Current.CancellationToken);
            Dictionary<string, string> environment = new()
            {
                ["Configuration"] = mode == WorkspaceMode.Source ? "Debug" : "Release",
                ["CI"] = mode == WorkspaceMode.Package ? "true" : "false",
            };
            if (propsOnly)
                environment["CustomAfterMicrosoftCommonProps"] = SourceMappingMaterializer.PropsPath(run);
            else
            {
                environment["HexalithSourceMappingHash"] = "placeholder";
                environment["HexalithSourceMappingApplies"] = "true";
                environment["UseHexalithProjectReferences"] = mode == WorkspaceMode.Source ? "true" : "false";
            }

            CompositionProcessResult result = await CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
                ["msbuild", Project(root, "src/Hexalith.Conversations.Contracts"), "-target:PrepareForBuild", "-nologo"], root, environment),
                TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
            result.ExitCode.ShouldNotBe(0);
            (result.Output + result.ErrorOutput).ShouldContain("HexalithVerifySourceMappingSelection");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, true);
        }
    }

    /// <summary>The isolated active root and real adjacent siblings use the repository's pinned SDK selection.</summary>
    /// <returns>A task for SDK policy and selection assertions.</returns>
    [Fact]
    public async Task ScratchWorkspaceUsesRepositorySdkPolicyAndAdjacentSiblings()
    {
        string root = CreateWorkspace();
        try
        {
            File.ReadAllBytes(Path.Combine(root, "global.json")).ShouldBe(File.ReadAllBytes(Path.Combine(RepositoryRoot(), "global.json")));
            File.Exists(DependencyProject(root, "Hexalith.Commons.Serialization", "..")).ShouldBeTrue();
            Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(
                DependencyProject(root, "Hexalith.Commons.Serialization", "..")))))!
                .ShouldBe(Path.Combine(Path.GetDirectoryName(root)!, "Hexalith.Commons"));
            CompositionProcessResult repositorySdk = await CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
                ["--version"], RepositoryRoot(), null), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
            CompositionProcessResult scratchSdk = await CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
                ["--version"], root, null), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
            repositorySdk.ExitCode.ShouldBe(0, repositorySdk.Output + repositorySdk.ErrorOutput);
            scratchSdk.ExitCode.ShouldBe(0, scratchSdk.Output + scratchSdk.ErrorOutput);
            scratchSdk.Output.Trim().ShouldBe(repositorySdk.Output.Trim());
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, true);
        }
    }

    /// <summary>Packs the actual Contracts inventory in mapped Debug source mode.</summary>
    internal static string PackSourceInventory(string repositoryRoot, string packageOutput)
    {
        string workspace = Path.Combine(packageOutput, "mapping");
        string commons = MappedRoot("Commons");
        SourceMapping mapping = new(WorkspaceMode.Source, repositoryRoot, "Hexalith.Conversations",
            [new("Hexalith.Conversations", "source", repositoryRoot), new("Hexalith.Commons", "source", commons),
             new("Hexalith.Builds", "source", MappedRoot("Builds"))], true);
        SourceMappingMaterializer.WriteAsync(mapping, workspace, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        CompositionProcessResult Run(params string[] arguments) => CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet", arguments,
            repositoryRoot, SourceMappingMaterializer.Environment(mapping, workspace)), TimeSpan.FromMinutes(3), TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        string commonsProject = Path.Combine(commons, "src", "libraries", "Hexalith.Commons.Serialization", "Hexalith.Commons.Serialization.csproj");
        CompositionProcessResult version = Run("msbuild", commonsProject, "-getProperty:PackageVersion");
        version.ExitCode.ShouldBe(0, version.Output + version.ErrorOutput);
        CompositionProcessResult pack = Run("pack", Project(repositoryRoot, "src/Hexalith.Conversations.Contracts"), "-c", "Debug", "-o", packageOutput,
            "-p:GeneratePackageOnBuild=false");
        pack.ExitCode.ShouldBe(0, pack.Output + pack.ErrorOutput);
        return version.Output.Trim();
    }

    private static string MappedRoot(string identity) => typeof(PlatformEnrollmentMappingTest).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "Enrolled" + identity + "Root").Value
        ?? throw new InvalidOperationException("The test build did not retain its selected mapping.");

    private static string[] Items(JsonDocument output, string name)
        => output.RootElement.GetProperty("Items").GetProperty(name).EnumerateArray().Select(item => item.GetProperty("Identity").GetString()!).ToArray();

    private static Task<CompositionProcessResult> EvaluateAsync(string root, string directory, SourceMapping mapping, string run, params string[] extra)
        => CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
            ["msbuild", Project(root, directory), "-target:HexalithReconcileSourceMapping", "-getProperty:Configuration", "-getItem:ProjectReference,PackageReference", .. extra],
            root, SourceMappingMaterializer.Environment(mapping, run)), TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

    private static SourceMapping Mapping(string root, WorkspaceMode mode) => new(mode, root, "Hexalith.Conversations",
        [new("Hexalith.Conversations", "source", root), .. new[] { "Builds", "Commons", "EventStore", "Tenants" }.Select(identity =>
            new SourceMappingEntry("Hexalith." + identity, mode == WorkspaceMode.Source ? "source" : "package",
                mode == WorkspaceMode.Source ? Path.Combine(root, "selected", "Hexalith." + identity) : null))], true);

    private static string CreateWorkspace()
    {
        string repository = RepositoryRoot();
        string root = Path.Combine(Path.GetTempPath(), "conversations-enrollment-mapping", Guid.NewGuid().ToString("N"), "active");
        Directory.CreateDirectory(root);
        foreach (string path in new[] { "global.json", "Directory.Build.props", "Directory.Packages.props", "tests/Directory.Build.props" }
            .Concat(Directory.GetFiles(Path.Combine(repository, "src"), "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(repository, "tests"), "*.csproj", SearchOption.AllDirectories))
                .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(repository, path))))
        {
            string destination = Path.Combine(root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(repository, path), destination);
        }

        foreach (string layout in new[] { "selected", "references", ".." })
        {
            foreach (string identity in Dependencies.Values.SelectMany(value => value).Distinct(StringComparer.Ordinal))
            {
                string project = DependencyProject(root, identity, layout);
                Directory.CreateDirectory(Path.GetDirectoryName(project)!);
                File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><PackageId>" + identity + "</PackageId></PropertyGroup></Project>");
            }

            string props = Path.Combine(root, layout, "Hexalith.Builds", "Props", "Directory.Packages.props");
            Directory.CreateDirectory(Path.GetDirectoryName(props)!);
            File.Copy(Path.Combine(MappedRoot("Builds"), "Props", "Directory.Packages.props"), props);
        }

        return root;
    }

    private static string DependencyProject(string root, string identity, string layout)
    {
        string owner = "Hexalith." + identity.Split('.')[1];
        return Path.GetFullPath(owner == "Hexalith.Commons"
            ? Path.Combine(root, layout, owner, "src", "libraries", identity, identity + ".csproj")
            : owner == "Hexalith.Builds"
                ? Path.Combine(root, layout, owner, "src", "libraries", identity, identity + ".csproj")
                : Path.Combine(root, layout, owner, "src", identity, identity + ".csproj"));
    }

    private static string Project(string root, string directory) => Path.Combine(root, directory, Path.GetFileName(directory) + ".csproj");

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Conversations repository root unavailable.");
    }
}
