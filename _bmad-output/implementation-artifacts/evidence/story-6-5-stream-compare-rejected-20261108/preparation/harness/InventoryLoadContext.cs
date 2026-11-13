using System.Reflection;
using System.Runtime.Loader;

namespace Story65.ImageMemo.Harness;

/// <summary>Resolves only the ordinary owner's dependency graph, without invoking domain methods.</summary>
internal sealed class InventoryLoadContext(string ownerPath) : AssemblyLoadContext(Path.GetFileNameWithoutExtension(ownerPath) + "-image-inventory", isCollectible: false)
{
    private readonly AssemblyDependencyResolver _resolver = new(ownerPath);

    protected override Assembly? Load(AssemblyName name)
    {
        string? path = _resolver.ResolveAssemblyToPath(name);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
}
