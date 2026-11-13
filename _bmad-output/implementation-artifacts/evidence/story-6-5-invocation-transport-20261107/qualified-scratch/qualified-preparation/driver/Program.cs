using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Story65.InvocationTransportProof;
try
{
    SharedDependencies.Install();
    var paths = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Paths.Prep, "variant-paths.json"))).RootElement;
    SdkVariant ordinary = new("ordinary-sdk", paths.GetProperty("oracleDll").GetString()!);
    SdkVariant candidate = new("private-sdk", paths.GetProperty("privateDll").GetString()!); SdkVariant.CompareIdentities(ordinary, candidate);
    string entry = Assembly.GetEntryAssembly()!.GetName().Name!;
    Check.Require(entry is "Hexalith.EventStore" or "Story65.InvocationTransport.Excluded", "Exact physical entry required.");
    if (args.ElementAtOrDefault(0) == "facts")
    {
        static object Facts(Assembly a)
        {
            var debug = a.GetCustomAttribute<DebuggableAttribute>();
            return new { assembly = a.GetName().Name, configuration = a.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                jitOptimizerDisabled = debug?.IsJITOptimizerDisabled, debuggingFlags = debug?.DebuggingFlags.ToString() };
        }
        Console.WriteLine(JsonSerializer.Serialize(new {
            runtimeVersion = Environment.Version.ToString(), processorCount = Environment.ProcessorCount, processorOverride = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT"),
            buildingSdk = typeof(SdkVariant).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(x => x.Key == "BuildingSdk").Value,
            physicalEntry = entry, oracle = Facts(ordinary.Assembly), candidate = Facts(candidate.Assembly), pluginOracle = Facts(ordinary.Plugin), pluginCandidate = Facts(candidate.Plugin), scratch = Facts(typeof(SdkVariant).Assembly),
            samePublicIdentity = true, distinctSdkInstances = true, distinctPluginInstances = true, allOtherDependenciesDefaultShared = true, defaultSdkAbsent = true,
        })); return 0;
    }
    Check.Require(args.Length >= 3, "Closed CLI required.");
    if (entry == "Story65.InvocationTransport.Excluded") { Check.Require(args[0] == "excluded", "Excluded mode required."); return await ExcludedDriver.RunAsync(args, ordinary, candidate); }
    return args[0].StartsWith("canary-", StringComparison.Ordinal) ? await FreshFlowCanary.RunAsync(args, candidate) : await DifferentialDriver.RunAsync(args, ordinary, candidate);
}
catch (Exception error)
{
    SafeFailure safe = SafeFailure.From(error);
    var failure = new { Cases = 0, Passed = 0, ErrorType = safe.Kind, safe.AssertionFile, safe.AssertionLine };
    if (args.Length > 2) await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(failure));
    Console.WriteLine(JsonSerializer.Serialize(new { cases = 0, passed = 0, errorType = safe.Kind })); return 1;
}
