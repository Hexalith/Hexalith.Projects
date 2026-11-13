using System.Reflection;
using System.Text.Json;
using Story65.OutboundAssertionProof;

try
{
    string home = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    string prep = "/tmp/story65-outbound-assertion-prep-20261107";
    var paths = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(prep, "variant-paths.json"))).RootElement;
    ServerVariant ordinary = new("ordinary-server", paths.GetProperty("oracleDll").GetString()!);
    ServerVariant instrumented = new("private-server", paths.GetProperty("privateDll").GetString()!);
    ServerVariant.CompareIdentities(ordinary, instrumented);
    if (args.ElementAtOrDefault(0) == "facts")
    {
        static object Facts(Assembly assembly)
        {
            var debug = assembly.GetCustomAttributes<System.Diagnostics.DebuggableAttribute>().SingleOrDefault();
            return new { assembly = assembly.GetName().Name, configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                jitOptimizerDisabled = debug?.IsJITOptimizerDisabled, debuggingFlags = debug?.DebuggingFlags.ToString() };
        }
        Console.WriteLine(JsonSerializer.Serialize(new { runtimeVersion = Environment.Version.ToString(), processorCount = Environment.ProcessorCount,
            processorOverride = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT"), oracle = Facts(ordinary.Assembly), candidate = Facts(instrumented.Assembly), scratch = Facts(typeof(ServerVariant).Assembly),
            samePublicIdentity = ordinary.Assembly.FullName == instrumented.Assembly.FullName, differentServerInstances = !ReferenceEquals(ordinary.Assembly, instrumented.Assembly) }));
        return 0;
    }
    return args[0].StartsWith("canary-", StringComparison.Ordinal) ? await FreshFlowCanary.RunAsync(args, instrumented)
        : await DifferentialDriver.RunAsync(args, ordinary, instrumented);
}
catch (Exception exception)
{
    var failure = new { Cases = 0, Passed = 0, ErrorType = exception.GetType().FullName,
        AssertionFile = (exception as ProofAssertionFailure)?.AssertionFile, AssertionLine = (exception as ProofAssertionFailure)?.AssertionLine };
    if (args.Length > 2) await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(failure));
    Console.WriteLine(JsonSerializer.Serialize(new { cases = 0, passed = 0, errorType = exception.GetType().FullName }));
    return 1;
}
