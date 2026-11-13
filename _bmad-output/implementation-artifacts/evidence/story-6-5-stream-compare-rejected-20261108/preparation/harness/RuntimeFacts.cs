using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Story65.ImageMemo.Harness;

/// <summary>Requires matched unoptimized Debug, natural CPU count and actual owner GC mode for the qualified production transfer.</summary>
internal static class RuntimeFacts
{
    internal const string OwnerRuntimeConfig = ImageInventory.Workspace + "/references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server.runtimeconfig.json";

    internal static object Validate()
    {
        Require.That(Environment.Version == new Version(10, 0, 14), "runtime-10-0-14");
        Require.That(Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") is null, "natural-cpu-override-absent");
        Require.That(Environment.ProcessorCount == 14, "natural-fourteen-processors");
        using JsonDocument runtime = JsonDocument.Parse(File.ReadAllBytes(OwnerRuntimeConfig));
        bool ownerServerGc = runtime.RootElement.GetProperty("runtimeOptions").GetProperty("configProperties")
            .GetProperty("System.GC.Server").GetBoolean();
        Require.That(ownerServerGc && GCSettings.IsServerGC == ownerServerGc, "match-owner-server-gc");
        return new
        {
            Runtime = Environment.Version.ToString(),
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture,
            Environment.ProcessorCount,
            ProcessorCountOverrideAbsent = true,
            GCSettings.IsServerGC,
            LatencyMode = GCSettings.LatencyMode.ToString(),
            OriginalOwnerRuntimeConfigPath = OwnerRuntimeConfig,
            OriginalOwnerRuntimeConfigSha256 = ImageInventory.HashFile(OwnerRuntimeConfig),
            OriginalOwnerServerGc = ownerServerGc,
            ForcedGcCalls = 0,
            Harness = VariantPathProof.Facts(Assembly.GetExecutingAssembly()),
        };
    }
}
