namespace Story65.DomainReceiverPhase;
using System.Reflection;
internal static class NativeRunner
{
    internal static readonly IReadOnlyDictionary<string,int> Selections=new Dictionary<string,int>
    {
        ["DevelopmentDomainRouteCatalogTests"]=14,["DomainSidecarCatalogLeaseTests"]=12,
        ["CatalogReceiverScopeTests"]=41,["CatalogReceiverStateBoundaryTests"]=16,
        ["CatalogReceiverVersionTests"]=7,["TechnicalHttpDomainPeerTests"]=12,
        ["HeldMetadataDispatchTests.RealDomainFilterAndResultComposeWithSdkMetadataClient"]=1,
    };
    internal static async Task<int> RunAsync(string selection,string xml,RuntimeInputs inputs)
    {
        Check.Require(Selections.ContainsKey(selection) && Path.IsPathFullyQualified(xml) && !File.Exists(xml),"closed-native-selection");
        Assembly runner=Assembly.LoadFrom(Path.Combine(inputs.Directory,"xunit.v3.runner.inproc.console.dll"));
        Type type=runner.GetType("Xunit.Runner.InProc.SystemConsole.ConsoleRunner",true)!;
        string flag=selection.Contains(".",StringComparison.Ordinal)?"-method":"-class";
        string[] argv=[flag,"Hexalith.EventStore.Server.Tests.Routing."+selection,"-xml",xml];
        using IDisposable instance=(IDisposable)Activator.CreateInstance(type,[argv,inputs.Tests])!;
        var entry=type.GetMethod("EntryPoint",BindingFlags.Instance|BindingFlags.Public,null,[typeof(TextReader),typeof(TextWriter)],null)!;
        int code=await ((Task<int>)entry.Invoke(instance,[null,TextWriter.Null])!).ConfigureAwait(false);
        inputs.VerifyLoaded();return code;
    }
}
