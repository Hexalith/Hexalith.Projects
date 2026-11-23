namespace Hexalith.Projects.PackageFixtures.AppHost;

using System.Reflection;
using global::Aspire.Hosting;

/// <summary>Identifies the selected package project and its evaluated Release output.</summary>
internal sealed class PrebuiltMetadata(string projectPath, string targetPath) : IProjectMetadata
{
    internal static PrebuiltMetadata ForProject(string projectPath)
    {
        string key = "PackageFixtureTarget:" + Path.GetFullPath(projectPath);
        string? target = typeof(PrebuiltMetadata).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(metadata => metadata.Key == key)?.Value;
        return new(projectPath, target ?? throw new InvalidOperationException("No evaluated Release target for " + projectPath));
    }

    public string ProjectPath => projectPath;
    public bool SuppressBuild => true;
    public string BuildWorkingDirectory => Path.GetDirectoryName(projectPath)!;

    public IReadOnlyDictionary<string, string> BuildEnvironment => new Dictionary<string, string>
    {
        ["Configuration"] = "Release", ["UseHexalithProjectReferences"] = "false", ["DOTNET_WATCH"] = "false",
    };

    internal string ResolveReleaseAssembly()
    {
        string output = Path.Combine(BuildWorkingDirectory, "bin", "Release");
        string assembly = Path.GetFileName(targetPath);
        if (!File.Exists(targetPath) || !File.Exists(Path.ChangeExtension(targetPath, "runtimeconfig.json"))
            || !File.Exists(Path.ChangeExtension(targetPath, "deps.json")) || !Directory.Exists(output)
            || Directory.EnumerateFiles(output, assembly, SearchOption.AllDirectories).Take(2).Count() != 1)
            throw new InvalidOperationException("Missing or ambiguous prebuilt Release output for " + projectPath);
        _ = AssemblyName.GetAssemblyName(targetPath);
        return targetPath;
    }
}
