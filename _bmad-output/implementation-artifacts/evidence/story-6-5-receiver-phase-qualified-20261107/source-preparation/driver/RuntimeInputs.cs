namespace Story65.ReceiverPhase;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
internal sealed class RuntimeInputs
{
    internal string Variant { get; }
    internal string Role { get; }
    internal string Directory { get; }
    internal Assembly Sdk { get; }
    internal Assembly Tests { get; }
    internal Assembly Companion { get; }
    private readonly Dictionary<string,string> files;
    private readonly string[] frameworkRoots;
    private RuntimeInputs(JsonElement root)
    {
        Variant = root.GetProperty("variant").GetString()!; Role = root.GetProperty("role").GetString()!;
        Check.Require(Variant is "ordinary" or "private" && Role is "projects" or "tenants" or "folders" or "excluded", "closed-runtime-variant-role");
        Directory = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
        files = root.GetProperty("filesSha256").Deserialize<Dictionary<string,string>>()!;
        frameworkRoots = root.GetProperty("frameworkRoots").Deserialize<string[]>()!;
        Check.Require(files.Count > 0 && frameworkRoots.SequenceEqual(new[] { "/home/administrator/.dotnet/shared/Microsoft.NETCore.App/10.0.14", "/home/administrator/.dotnet/shared/Microsoft.AspNetCore.App/10.0.14" }), "closed-runtime-inputs");
        VerifyFiles();
        AssemblyLoadContext.Default.Resolving += Resolve;
        Sdk = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Directory,"Hexalith.EventStore.ServiceDefaults.dll"));
        Tests = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Directory,"Hexalith.EventStore.Server.Tests.dll"));
        Companion = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Directory,"Hexalith.EventStore.Server.LiveSidecar.Tests.dll"));
        Check.Require(Sdk.GetName().Name == "Hexalith.EventStore.ServiceDefaults" && Tests.GetName().Name == "Hexalith.EventStore.Server.Tests"
            && Companion.GetName().Name == "Hexalith.EventStore.Server.LiveSidecar.Tests", "actual-assembly-identities");
        Check.Require(Hash(Sdk.Location) == root.GetProperty("sdkSha256").GetString(), "actual-sdk-variant-hash");
        if(Variant=="ordinary")Check.Require(Hash(Sdk.Location)=="6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542", "current-ordinary-sdk-required");
        foreach (Assembly a in new[] { Assembly.GetEntryAssembly()!, typeof(RuntimeInputs).Assembly, Sdk, Tests, Companion })
        {
            Check.Require(AssemblyLoadContext.GetLoadContext(a) == AssemblyLoadContext.Default, "all-business-default-context");
            Check.Require(a.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration == "Debug"
                && a.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true, "matched-debug-disable-optimizations");
        }
        string entry = Assembly.GetEntryAssembly()!.GetName().Name!;
        Check.Require(entry == root.GetProperty("entryAssemblyName").GetString(), "genuine-entry-metadata");
        Check.Require(entry == (Role switch { "projects" => "Hexalith.Projects.Server", "tenants" => "Hexalith.Tenants", "folders" => "Hexalith.Folders.Server", _ => "Story65.ReceiverPhase.Excluded" }), "fixed-entry-role-map");
        VerifyLoaded();
    }
    internal static RuntimeInputs Load(string path, string expected)
    {
        Check.Require(Path.IsPathFullyQualified(path) && Hash(path) == expected && expected.Length == 64, "runtime-binding-hash");
        using JsonDocument json = JsonDocument.Parse(File.ReadAllBytes(path));
        Check.Require(json.RootElement.GetProperty("schema").GetInt32() == 1, "runtime-binding-schema");
        Check.Require(Environment.Version == new Version(10,0,14) && Environment.ProcessorCount == 14
            && Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") is null, "natural-runtime-fourteen");
        return new(json.RootElement);
    }
    private Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
    {
        string file = name.Name + ".dll";
        if (files.ContainsKey(file)) return context.LoadFromAssemblyPath(Path.Combine(Directory,file));
        throw new DriverFailure("dependency-missing-from-pinned-variant");
    }
    internal void VerifyFiles()
    {
        foreach ((string relative,string expected) in files)
        {
            Check.Require(!Path.IsPathFullyQualified(relative) && !relative.Split('/').Contains(".."), "closed-input-relative-path");
            string path = Path.GetFullPath(Path.Combine(Directory,relative));
            Check.Require(path.StartsWith(Directory+Path.DirectorySeparatorChar,StringComparison.Ordinal)
                && (File.GetAttributes(path)&FileAttributes.ReparsePoint)==0 && Hash(path)==expected, "runtime-input-unchanged");
        }
    }
    internal void VerifyLoaded()
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies().Where(a=>!a.IsDynamic))
        {
            string path = Path.GetFullPath(a.Location);
            if (frameworkRoots.Any(root=>path.StartsWith(root.TrimEnd('/')+"/",StringComparison.Ordinal))) continue;
            Check.Require(Path.GetDirectoryName(path)==Directory && files.TryGetValue(Path.GetFileName(path),out string? h)
                && Hash(path)==h && AssemblyLoadContext.GetLoadContext(a)==AssemblyLoadContext.Default, "no-workspace-or-other-variant-fallback");
        }
        Check.Require(AppDomain.CurrentDomain.GetAssemblies().Count(a=>a.GetName().Name=="Hexalith.EventStore.ServiceDefaults")==1, "single-real-sdk-identity");
    }
    internal object Facts() => new { Variant, Role, Runtime=Environment.Version.ToString(), Processors=Environment.ProcessorCount,
        SdkSha256=Hash(Sdk.Location), TestDllSha256=Hash(Tests.Location), CompanionSha256=Hash(Companion.Location),
        ActualDefaultContextOnly=true, HistoricalSdkFallback=false, RuntimeInputs=files.Count,
        Entry=Assembly.GetEntryAssembly()!.GetName().Name, BuildingSdk=typeof(RuntimeInputs).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a=>a.Key=="BuildingSdk").Value };
    internal static string Hash(string path) { using FileStream s=File.OpenRead(path); return Convert.ToHexStringLower(SHA256.HashData(s)); }
}
