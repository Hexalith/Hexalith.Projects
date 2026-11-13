namespace Story65.InvocationTransportProof;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
internal static class SharedDependencies
{
    private static readonly Dictionary<string, string> Inputs = new(StringComparer.OrdinalIgnoreCase);
    internal static void Install()
    {
        var pins = JsonDocument.Parse(File.ReadAllText(Path.Combine(Paths.Prep, "plugin-reference-pins.json"))).RootElement.GetProperty("inputsSha256");
        foreach (var entry in pins.EnumerateObject())
        {
            string name = Path.GetFileNameWithoutExtension(entry.Name);
            if (name == Paths.SdkName) continue;
            Check.Require(!name.StartsWith("Hexalith.", StringComparison.Ordinal) || name is "Hexalith.EventStore.Client" or "Hexalith.EventStore.Contracts" or "Hexalith.Commons.UniqueIds", "Unexpected application dependency.");
            Check.Require(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(entry.Name))) == entry.Value.GetString(), "Shared dependency changed.");
            Inputs.Add(name, entry.Name);
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        AssertNoDefaultVariant();
    }
    private static Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
    {
        if (name.Name is Paths.SdkName or Paths.PluginName) throw new FileLoadException("ForbiddenDefaultVariant");
        return name.Name is not null && Inputs.TryGetValue(name.Name, out string? path) ? context.LoadFromAssemblyPath(path) : null;
    }
    internal static Assembly Load(AssemblyName name)
    {
        Check.Require(name.Name is not Paths.SdkName and not Paths.PluginName, "Variant fallback forbidden.");
        return AssemblyLoadContext.Default.LoadFromAssemblyName(name);
    }
    internal static void AssertNoDefaultVariant() => Check.Require(!AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name is Paths.SdkName or Paths.PluginName), "Variant entered Default.");
}
