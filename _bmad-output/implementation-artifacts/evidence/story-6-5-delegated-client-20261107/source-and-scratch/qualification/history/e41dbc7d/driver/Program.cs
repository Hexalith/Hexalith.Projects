using Story65.DelegatedClientProof;

if (args.ElementAtOrDefault(0) == "facts")
{
    static object AssemblyFacts(System.Reflection.Assembly assembly)
    {
        var debug = assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false)
            .Cast<System.Diagnostics.DebuggableAttribute>().SingleOrDefault();
        return new { assembly = assembly.GetName().Name, configuration = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
            .Cast<System.Reflection.AssemblyConfigurationAttribute>().SingleOrDefault()?.Configuration,
            jitOptimizerDisabled = debug?.IsJITOptimizerDisabled, debuggingFlags = debug?.DebuggingFlags.ToString() };
    }
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
    {
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, runtimeVersion = Environment.Version.ToString(),
        processorCount = Environment.ProcessorCount, processorOverride = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT"),
        oracle = AssemblyFacts(typeof(Hexalith.EventStore.DomainService.DelegatedOwnerQueryClient).Assembly),
        scratch = AssemblyFacts(typeof(DifferentialDriver).Assembly),
    }));
    return 0;
}

try
{
    return args[0].StartsWith("canary-", StringComparison.Ordinal)
        ? await FreshFlowCanary.RunAsync(args) : await DifferentialDriver.RunAsync(args);
}
catch (Exception exception)
{
    var failure = new { Cases = 0, Passed = 0, ErrorType = exception.GetType().FullName,
        AssertionFile = (exception as ProofAssertionFailure)?.AssertionFile, AssertionLine = (exception as ProofAssertionFailure)?.AssertionLine };
    if (args.Length > 2) await File.WriteAllTextAsync(args[2], System.Text.Json.JsonSerializer.Serialize(failure));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { cases = 0, passed = 0, errorType = exception.GetType().FullName }));
    return 1;
}
