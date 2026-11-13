using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Story65.ImageMemo.Harness;

/// <summary>Proves the new compiled production SDK and unchanged copied RWL/stream-compare paths.</summary>
internal static class VariantPathProof
{
    internal static string ExpectedSdkSha256 => FinalTransferInputs.SdkSha256;

    internal static object Validate(IMemoHost host)
    {
        string expectedNamespace = host.Name switch
        {
            "production" => "Hexalith.EventStore.ServiceDefaults.Routing",
            "rwl-control" => "Story65.ImageMemo.Control",
            "stream-compare" => "Story65.ImageMemo.Candidate",
            _ => throw new InvalidOperationException("variant-name"),
        };
        Type[] types = [host.MemoType, host.ScopeType, host.FreshType, host.TechnicalType];
        Require.That(types.All(type => type.Namespace == expectedNamespace), "variant-type-namespace");
        Require.That(types.All(type => type.Assembly == host.MemoType.Assembly), "variant-types-one-assembly");
        Assembly expectedAssembly = host.Name == "production"
            ? typeof(Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogImageDigestMemo).Assembly
            : Assembly.GetExecutingAssembly();
        Require.That(host.MemoType.Assembly == expectedAssembly, "variant-actual-assembly");
        if (host.Name == "production")
        {
            Require.That(host.GateForTesting is ReaderWriterLockSlim, "compiled-production-rwl-gate");
            Require.That(AssemblyLoadContext.GetLoadContext(expectedAssembly) == AssemblyLoadContext.Default, "one-production-sdk-default-context");
            Require.That(ImageInventory.HashFile(expectedAssembly.Location) == ExpectedSdkSha256, "compiled-production-sdk-native-qualified-sha");
        }
        Require.That(host.GateForTesting is ReaderWriterLockSlim, "all-three-current-rwl-gates");
        Assembly sdkImage = typeof(Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogImageDigestMemo).Assembly;
        string expectedDigest = ImageInventory.HashFile(sdkImage.Location);
        string expectedTechnical = $"{sdkImage.FullName}:{sdkImage.ManifestModule.ModuleVersionId}:{expectedDigest}";
        long sdkBytes = new FileInfo(sdkImage.Location).Length;
        Require.That(sdkBytes is > 0 and <= 16 * 1024 * 1024, "path-proof-small-fit-image");
        using (IObservation scope = host.BeginObservation())
        {
            Require.That(scope.ScopeType == host.ScopeType && scope.CacheHits == 0, "selected-observation-type");
            Require.That(host.ComputeDomain(sdkImage) == expectedDigest && scope.CacheHits == 0, "selected-domain-cold-path");
            Require.That(host.ComputeDomain(sdkImage) == expectedDigest && scope.CacheHits == 1, "selected-domain-fresh-hit-path");
            using (FileStream stream = File.OpenRead(sdkImage.Location))
            {
                Require.That(host.ComputeStream(sdkImage, stream) == expectedDigest && scope.CacheHits == 2, "selected-stream-hit-path");
            }
            Require.That(host.ComputeTechnical(sdkImage) == expectedTechnical && scope.CacheHits == 3, "selected-technical-pe-mvid-hit-path");
            Require.That(host.Count == 1 && host.RetainedBytes == sdkBytes + 512, "selected-memo-actual-occupancy");
        }
        return new
        {
            host.Name,
            MemoType = host.MemoType.FullName,
            ScopeType = host.ScopeType.FullName,
            FreshType = host.FreshType.FullName,
            TechnicalType = host.TechnicalType.FullName,
            ScopeTypeProvenByActualObservation = true,
            DomainStreamAndTechnicalSelectedPathProvenByActualHitsAndOccupancy = true,
            DomainPathUsesSelectedFreshHelperNotDomainServiceMethod = true,
            Assembly = Facts(expectedAssembly),
        };
    }

    internal static object Facts(Assembly assembly)
    {
        DebuggableAttribute debug = assembly.GetCustomAttribute<DebuggableAttribute>()
            ?? throw new InvalidOperationException("debug-attribute-missing");
        string? configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        Require.That(configuration == "Debug" && debug.IsJITOptimizerDisabled, "matched-unoptimized-debug");
        return new
        {
            assembly.FullName,
            assembly.Location,
            Sha256 = ImageInventory.HashFile(assembly.Location),
            ModuleVersionId = assembly.ManifestModule.ModuleVersionId,
            Configuration = configuration,
            DebugFlags = debug.DebuggingFlags.ToString(),
            debug.IsJITOptimizerDisabled,
            debug.IsJITTrackingEnabled,
            LoadContext = AssemblyLoadContext.GetLoadContext(assembly)?.Name,
        };
    }
}
