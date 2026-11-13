using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.Canonical.Harness;
/// <summary>Requires unoptimized Debug, natural14 .NET10.0.14 and actual producer identities.</summary>
internal static class RuntimeFacts
{
    internal static object Validate()
    {
        Check.That(Environment.Version == new Version(10,0,14) && Environment.ProcessorCount == 14, "runtime-and-natural-fourteen");
        Check.That(Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") is null && GCSettings.IsServerGC, "natural-cpu-server-gc");
        Check.That(Datasets.Sha(typeof(RouteCatalogCodec).Assembly.Location) == InputPins.ContractsSha256,"actual-new-contracts-owner-hash");
        Check.That(Datasets.Sha(typeof(ObservedRouteCatalogHost).Assembly.Location) == InputPins.SdkSha256,"actual-new-sdk-owner-hash");
        Dataset dataset = Datasets.Load().Single(value => value.Name == "compiled-sample-owner");
        using (var owner = new ProductionVariant())
        {
            for (int index = 0; index < 2; index++)
            {
                using IDisposable scope = owner.Enter();
                GC.KeepAlive(owner.Capture(dataset.Fresh(), "identity"));
            }
            owner.VerifyParticipation();
        }
        return new { Runtime=Environment.Version.ToString(),Environment.ProcessorCount,GCSettings.IsServerGC,
            Stopwatch.Frequency,Harness=AssemblyFacts(Assembly.GetExecutingAssembly()),
            ActualContracts=AssemblyFacts(typeof(RouteCatalogCodec).Assembly),ActualSdk=AssemblyFacts(typeof(ObservedRouteCatalogHost).Assembly),
            ForcedGcCalls=0,NewRuntimeEnvironmentValues=0,ActualAuthorityOwnerSelectionProved=true,InputPins.NativeHandoffSha256,
            OwnerInitializationAndDisposalOutsideCapture=true,ProductionOriginalMemos=3,ProductionNewHashMemos=1,ControlByteMemos=1,CopiedPrototypeMemos=2 };
    }
    private static object AssemblyFacts(Assembly assembly)
    {
        var flags=assembly.GetCustomAttribute<DebuggableAttribute>();
        Check.That(flags?.IsJITOptimizerDisabled == true, "debug-disable-optimizations");
        return new {assembly.FullName,Sha256=Datasets.Sha(assembly.Location),Debuggable=flags!.DebuggingFlags.ToString()};
    }
}
