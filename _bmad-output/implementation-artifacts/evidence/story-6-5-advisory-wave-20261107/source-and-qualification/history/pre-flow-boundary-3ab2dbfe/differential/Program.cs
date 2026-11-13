using Story65.AdvisoryWaveProof;

try { return await DifferentialDriver.RunAsync(args); }
catch (Exception exception)
{
    var failure = new { Cases = 0, Passed = 0, ErrorType = exception.GetType().FullName,
        AssertionFile = (exception as ProofAssertionFailure)?.AssertionFile,
        AssertionLine = (exception as ProofAssertionFailure)?.AssertionLine };
    if (args.Length > 2) await File.WriteAllTextAsync(args[2], System.Text.Json.JsonSerializer.Serialize(failure));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { cases = 0, passed = 0, errorType = exception.GetType().FullName }));
    return 1;
}
