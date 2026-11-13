namespace Story65.InvocationTransportProof;
using System.Reflection;
using System.Runtime.Loader;
internal sealed class IsolatedSdkContext(string name) : AssemblyLoadContext(name, isCollectible: false)
{
    internal Assembly? Sdk;
    protected override Assembly? Load(AssemblyName name)
    {
        if (name.Name == Paths.SdkName) return Sdk ?? throw new FileLoadException("SdkNotBound");
        if (name.Name == Paths.PluginName) throw new FileLoadException("PluginMustBeExplicit");
        return SharedDependencies.Load(name);
    }
}
