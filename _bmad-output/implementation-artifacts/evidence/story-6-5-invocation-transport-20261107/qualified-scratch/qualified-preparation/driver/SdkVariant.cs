namespace Story65.InvocationTransportProof;
using System.Reflection;
using System.Runtime.Loader;
internal sealed class SdkVariant
{
    internal readonly IsolatedSdkContext Context;
    internal readonly Assembly Assembly, Plugin;
    private readonly MethodInfo run;
    internal readonly Type[] FactTypes;
    internal SdkVariant(string name, string path)
    {
        Context = new(name);
        Assembly = Context.LoadFromAssemblyPath(Path.GetFullPath(path)); Context.Sdk = Assembly;
        Plugin = Context.LoadFromAssemblyPath(Paths.Plugin);
        Type fixture = Plugin.GetType("Story65.InvocationFixture.InvocationFixture", true)!;
        run = fixture.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Static)!;
        FactTypes = ((object[])fixture.GetMethod("Facts", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!).Cast<Type>().ToArray();
        Check.Require(FactTypes.Length == 7 && FactTypes.Take(5).All(t => ReferenceEquals(t.Assembly, Assembly)), "SDK typed fixtures escaped variant.");
        Check.Require(ReferenceEquals(FactTypes[5].Assembly, SharedDependencies.Load(FactTypes[5].Assembly.GetName())) && FactTypes[6] == typeof(HttpResponseMessage), "Shared framework/contracts were cloned.");
        Check.Require(ReferenceEquals(AssemblyLoadContext.GetLoadContext(Plugin), Context), "Plugin did not load into owned context.");
        foreach (Assembly owner in new[] { Assembly, Plugin })
            foreach (AssemblyName dependency in owner.GetReferencedAssemblies().Where(n => n.Name != Paths.SdkName && n.Name != Paths.PluginName))
                Check.Require(ReferenceEquals(Context.LoadFromAssemblyName(dependency), SharedDependencies.Load(dependency)), "Actual nonSDK dependency was cloned.");
        SharedDependencies.AssertNoDefaultVariant();
    }
    internal async Task<object[]> InvokeAsync(string test, Action<bool>? firstProbe = null, Func<Task>? fakeAwait = null, bool suppressEntry = false)
    {
        Task<object[]> pending;
        try { pending = (Task<object[]>)run.Invoke(null, [new object[] { test, Paths.Catalog }, firstProbe, fakeAwait, suppressEntry])!; }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException is not null) { throw SafeFailure.From(wrapped.InnerException); }
        try { object[] result = await pending.ConfigureAwait(false); ResultComparison.Validate(result); return result; }
        catch (Exception failure) { throw SafeFailure.From(failure); }
    }
    internal static void CompareIdentities(SdkVariant ordinary, SdkVariant candidate)
    {
        Check.Require(ordinary.Assembly.FullName == candidate.Assembly.FullName && ordinary.Plugin.FullName == candidate.Plugin.FullName, "Same public identity required.");
        Check.Require(!ReferenceEquals(ordinary.Assembly, candidate.Assembly) && !ReferenceEquals(ordinary.Plugin, candidate.Plugin), "Independent variant instances required.");
        Check.Require(ordinary.FactTypes.Take(5).Zip(candidate.FactTypes).All(pair => !ReferenceEquals(pair.First, pair.Second)), "Private SDK types were shared.");
        Check.Require(ReferenceEquals(ordinary.FactTypes[5], candidate.FactTypes[5]) && ReferenceEquals(ordinary.FactTypes[6], candidate.FactTypes[6]), "Shared dependency types differ.");
        SharedDependencies.AssertNoDefaultVariant();
    }
}
