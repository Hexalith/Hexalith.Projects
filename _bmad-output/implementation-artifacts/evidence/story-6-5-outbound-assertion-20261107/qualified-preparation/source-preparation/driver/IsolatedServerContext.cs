namespace Story65.OutboundAssertionProof;

using System.Reflection;
using System.Runtime.Loader;

internal sealed class IsolatedServerContext(string name) : AssemblyLoadContext(name, isCollectible: false)
{
    protected override Assembly? Load(AssemblyName name)
    {
        if (name.Name == "Hexalith.EventStore.Server") throw new FileLoadException("Default Server fallback forbidden.");
        return Default.LoadFromAssemblyName(name);
    }
}
