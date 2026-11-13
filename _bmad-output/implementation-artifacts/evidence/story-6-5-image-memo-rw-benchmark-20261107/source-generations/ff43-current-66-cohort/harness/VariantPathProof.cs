using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Story65.ImageMemo.Harness;

/// <summary>Proves which memo, scope, fresh-stream and technical implementation each typed adapter actually selects.</summary>
internal static class VariantPathProof
{
    internal const string ExpectedSdkSha256 = "5bb66fad2959bc0db70cd9e3392c175da56c1bb0e5446fe14b3ad37f109a7872";

    internal static object Validate(IMemoHost host)
    {
        string expectedNamespace = host.Name switch
        {
            "oracle" => "Hexalith.EventStore.ServiceDefaults.Routing",
            "control" => "Story65.ImageMemo.Control",
            "candidate" => "Story65.ImageMemo.Candidate",
            _ => throw new InvalidOperationException("variant-name"),
        };
        Type[] types = [host.MemoType, host.ScopeType, host.FreshType, host.TechnicalType];
        Require.That(types.All(type => type.Namespace == expectedNamespace), "variant-type-namespace");
        Require.That(types.All(type => type.Assembly == host.MemoType.Assembly), "variant-types-one-assembly");
        Assembly expectedAssembly = host.Name == "oracle"
            ? typeof(Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogImageDigestMemo).Assembly
            : Assembly.GetExecutingAssembly();
        Require.That(host.MemoType.Assembly == expectedAssembly, "variant-actual-assembly");
        if (host.Name == "oracle")
        {
            Require.That(ImageInventory.HashFile(expectedAssembly.Location) == ExpectedSdkSha256, "compiled-sdk-oracle-sha");
        }
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
