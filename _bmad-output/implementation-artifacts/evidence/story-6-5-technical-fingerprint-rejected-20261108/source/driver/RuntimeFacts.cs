using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Checks actual current typed assemblies and the original Debug/natural CPU runtime shape.</summary>
internal static class RuntimeFacts
{
    internal static object Validate()
    {
        Check.That(Environment.Version == new Version(10, 0, 14) && Environment.ProcessorCount == 14, "runtime-natural-fourteen");
        Check.That(Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") is null && GCSettings.IsServerGC, "natural-cpu-server-gc");
        Assembly sdk = typeof(ObservedRouteCatalogHost).Assembly;
        Assembly contracts = typeof(RouteCatalogCodec).Assembly;
        Assembly gateway = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Hexalith.EventStore.Gateway");
        Assembly admin = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Hexalith.EventStore.Admin.Server");
        Check.That(InputPins.Sha(sdk.Location) == InputPins.SdkSha256 && InputPins.Sha(contracts.Location) == InputPins.ContractsSha256,
            "actual-current-sdk-contracts-loaded");
        Check.That(Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "BenchmarkSDKVersion").Value == "10.0.403", "actual-build-sdk-version");
        return new
        {
            Runtime = Environment.Version.ToString(), Environment.ProcessorCount, GCSettings.IsServerGC, Stopwatch.Frequency,
            Harness = Facts(Assembly.GetExecutingAssembly()), ActualSdk = Facts(sdk), ActualContracts = Facts(contracts), ActualGateway = Facts(gateway), ActualAdminServer = Facts(admin),
            NoStartRunOrNetworkCallsBySourceConstruction = true, NetworkOrLifecycleIndependentlyInstrumented = false,
            ForcedGcCalls = 0, RuntimeCpuOverrideAbsent = true,
            OwnerSetupAndDisposalOutsideCapture = true, FullHostOrDeadlineClaim = false,
        };
    }

    private static object Facts(Assembly assembly)
    {
        Check.That(ReferenceEquals(AssemblyLoadContext.GetLoadContext(assembly), AssemblyLoadContext.Default), "typed-default-assembly-identity");
        Check.That(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == assembly.GetName().Name) == 1, "one-actual-assembly-identity");
        DebuggableAttribute? debug = assembly.GetCustomAttribute<DebuggableAttribute>();
        Check.That(debug?.IsJITOptimizerDisabled == true, "debug-disable-jit-optimization");
        return new { assembly.FullName, Sha256 = InputPins.Sha(assembly.Location), Debuggable = debug!.DebuggingFlags.ToString() };
    }
}
