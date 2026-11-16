using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Security.Cryptography;

namespace Hexalith.Projects.PackageFixtures.AppHost;

/// <summary>Loads one prebuilt owner without substituting the AppHost's same-name package assemblies.</summary>
internal sealed class PackageOwnerLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _directory;
    private readonly Dictionary<string, byte[]> _loadedImages = new(OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>Uses the selected executable's existing dependency graph without building or restoring.</summary>
    internal PackageOwnerLoadContext(string executable) : base(isCollectible: true)
    {
        _directory = Path.GetDirectoryName(Path.GetFullPath(executable))! + Path.DirectorySeparatorChar;
        _resolver = new AssemblyDependencyResolver(executable);
    }

    /// <summary>Requires the exact selected file even when its assembly identity already exists elsewhere.</summary>
    internal Assembly LoadOwner(string path)
    {
        path = SelectedPath(path);
        byte[] selectedHash = SHA256.HashData(File.ReadAllBytes(path));
        if (_loadedImages.TryGetValue(path, out byte[]? frozenHash)
            && !CryptographicOperations.FixedTimeEquals(selectedHash, frozenHash))
            throw new InvalidOperationException("The selected executable artifact changed after loading: " + Path.GetFileName(path));
        Assembly loaded = LoadFromAssemblyPath(path);
        byte[] loadedHash = LoadedHash(loaded);
        if (!string.Equals(Path.GetFullPath(loaded.Location), path, _pathComparison)
            || !CryptographicOperations.FixedTimeEquals(selectedHash, loadedHash))
            throw new InvalidOperationException($"The loaded owner differs from the selected executable artifact: {Path.GetFileName(path)}; selected={Convert.ToHexStringLower(selectedHash)}; loaded={Convert.ToHexStringLower(loadedHash)}.");
        _loadedImages.TryAdd(path, selectedHash);
        return loaded;
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is null && assemblyName.Name is not null)
        {
            string adjacent = Path.Combine(_directory, assemblyName.Name + ".dll");
            if (File.Exists(adjacent)) path = adjacent;
        }
        Assembly? shared = Default.Assemblies.SingleOrDefault(assembly => assembly.GetName().Name == assemblyName.Name);
        bool contract = assemblyName.Name?.StartsWith("Hexalith.EventStore.", StringComparison.Ordinal) == true
            || assemblyName.Name is "System.Text.Json" or "System.Text.Encodings.Web" or "System.IO.Pipelines";
        // Attribute and declaration interfaces must use the fixture's exact SDK types,
        // even when this owner is the first code to request that contract assembly.
        if (contract) shared ??= Default.LoadFromAssemblyName(assemblyName);
        if (path is not null)
        {
            path = SelectedPath(path);
            if (contract)
            {
                shared ??= Default.LoadFromAssemblyName(assemblyName);
                if (shared.GetName().FullName != assemblyName.FullName
                    || AssemblyName.GetAssemblyName(path).FullName != assemblyName.FullName
                    || string.IsNullOrEmpty(shared.Location)
                    || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(File.ReadAllBytes(path)), LoadedHash(shared)))
                    throw new InvalidOperationException("Selected owner SDK contract differs from the AppHost: " + assemblyName.Name);
                return shared;
            }
            return LoadOwner(path);
        }
        Assembly framework = Default.LoadFromAssemblyName(assemblyName);
        string frameworkRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(object).Assembly.Location)))!
            + Path.DirectorySeparatorChar;
        if (string.IsNullOrEmpty(framework.Location) || !framework.Location.StartsWith(frameworkRoot, _pathComparison)
            || contract && framework.GetName().FullName != assemblyName.FullName)
            throw new InvalidOperationException("Selected owner dependency is absent from its output: " + assemblyName.Name);
        return framework;
    }

    private string SelectedPath(string path)
    {
        string full = Path.GetFullPath(path);
        if (!full.StartsWith(_directory, _pathComparison))
            throw new InvalidOperationException("Selected owner dependency escaped its output directory.");
        var file = new FileInfo(full);
        if (!file.Exists || file.LinkTarget is not null || (file.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Selected owner artifact must be a regular output file.");
        for (DirectoryInfo? directory = file.Directory; directory is not null; directory = directory.Parent)
        {
            if (directory.LinkTarget is not null || (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Selected owner output cannot contain linked directories.");
            if (string.Equals(directory.FullName + Path.DirectorySeparatorChar, _directory, _pathComparison)) break;
        }
        return full;
    }

    private static byte[] LoadedHash(Assembly assembly)
    {
        byte[] bytes = File.ReadAllBytes(assembly.Location);
        using var image = new PEReader(new MemoryStream(bytes, writable: false));
        var metadata = image.GetMetadataReader();
        if (metadata.GetGuid(metadata.GetModuleDefinition().Mvid) != assembly.ManifestModule.ModuleVersionId)
            throw new InvalidOperationException("Loaded assembly file differs from its actual module: " + assembly.GetName().Name);
        return SHA256.HashData(bytes);
    }
}
