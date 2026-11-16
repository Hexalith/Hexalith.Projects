namespace Hexalith.Projects.PackageFixtures.AppHost;

using global::Aspire.Hosting;

/// <summary>Launches one previously compiled project; missing Release output always fails closed.</summary>
/// <typeparam name="TProject">The generated resource-specific project metadata.</typeparam>
public sealed class PrebuiltMetadata<TProject> : IProjectMetadata where TProject : IProjectMetadata, new()
{
    private readonly TProject _project = new();

    /// <summary>Gets the original project path only after its exact Release output is present.</summary>
    public string ProjectPath
    {
        get
        {
            string path = _project.ProjectPath;
            string output = Path.Combine(Path.GetDirectoryName(path)!, "bin", "Release");
            string assembly = Path.GetFileNameWithoutExtension(path) + ".dll";
            if (!Directory.Exists(output) || Directory.GetFiles(output, assembly, SearchOption.AllDirectories).Length != 1)
                throw new InvalidOperationException("Missing or ambiguous prebuilt Release output for " + path);
            return path;
        }
    }

    /// <summary>Gets the mandatory no-build launch policy.</summary>
    public bool SuppressBuild => true;

    /// <summary>Gets the build context of the shared package build.</summary>
    public IReadOnlyDictionary<string, string> BuildEnvironment => new Dictionary<string, string>
    {
        ["Configuration"] = "Release", ["UseHexalithProjectReferences"] = "false", ["DOTNET_WATCH"] = "false",
    };

    /// <summary>Gets the original project directory for SDK selection.</summary>
    public string BuildWorkingDirectory => Path.GetDirectoryName(_project.ProjectPath)!;
}
