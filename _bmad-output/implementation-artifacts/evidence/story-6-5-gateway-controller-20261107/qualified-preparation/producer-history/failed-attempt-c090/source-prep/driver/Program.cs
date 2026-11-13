using System.Reflection;
using System.Text.Json;
using Story65.GatewayControllerProof;

try
{
    string home = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));
    string prep = "/tmp/story65-gateway-controller-prep-20261107";
    var paths = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(prep, "variant-paths.json"))).RootElement;
    GatewayVariant ordinary = new("ordinary-gateway", paths.GetProperty("oracleDll").GetString()!);
    GatewayVariant instrumented = new("private-gateway", paths.GetProperty("privateDll").GetString()!);
    GatewayVariant.CompareIdentities(ordinary, instrumented);
    if (args.ElementAtOrDefault(0) == "facts")
    {
        static object Facts(Assembly assembly)
        {
            var debug = assembly.GetCustomAttributes<System.Diagnostics.DebuggableAttribute>().SingleOrDefault();
            return new { assembly = assembly.GetName().Name, configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                jitOptimizerDisabled = debug?.IsJITOptimizerDisabled, debuggingFlags = debug?.DebuggingFlags.ToString() };
        }
        Console.WriteLine(JsonSerializer.Serialize(new { runtimeVersion = Environment.Version.ToString(), processorCount = Environment.ProcessorCount,
            processorOverride = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT"), oracle = Facts(ordinary.Assembly), candidate = Facts(instrumented.Assembly), scratch = Facts(typeof(GatewayVariant).Assembly),
            samePublicIdentity = ordinary.Assembly.FullName == instrumented.Assembly.FullName, differentGatewayInstances = !ReferenceEquals(ordinary.Assembly, instrumented.Assembly) }));
        return 0;
    }
    SharedResources.Prepare(paths.GetProperty("catalogPath").GetString()!);
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
