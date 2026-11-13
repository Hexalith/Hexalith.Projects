namespace Story65.DaprInvokerProof;

using System.Reflection;
using System.Runtime.Loader;

internal sealed class IsolatedGatewayContext(string name) : AssemblyLoadContext(name, isCollectible: false)
{
    protected override Assembly? Load(AssemblyName name)
    {
        if (name.Name == "Hexalith.EventStore.Gateway") throw new FileLoadException("Gateway fallback is forbidden.");
        // Every dependency is the exact default-context Assembly instance; no dependency copy is loaded here.
        return Default.LoadFromAssemblyName(name);
    }
}
