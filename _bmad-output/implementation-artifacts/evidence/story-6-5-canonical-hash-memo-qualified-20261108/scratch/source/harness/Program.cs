using System.Text.Json;
namespace Story65.Canonical.Harness;
/// <summary>Runs only after an exact source/compiled-input manifest and separate Root release.</summary>
internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        Check.That(args.Length == 3, "manifest-sha-output-arguments");
        string output=Path.GetFullPath(args[2]);
        Check.That(!Directory.Exists(output), "fresh-output-directory"); Directory.CreateDirectory(output);
        try
        {
            InputPins.Verify(args[0],args[1]);
            object facts=RuntimeFacts.Validate();
            File.WriteAllText(Path.Combine(output,"runtime-facts.json"),JsonSerializer.Serialize(facts,new JsonSerializerOptions {WriteIndented=true}));
            Dataset[] datasets=Datasets.Load();
            File.WriteAllText(Path.Combine(output,"dataset-facts.json"),JsonSerializer.Serialize(datasets.Select(Datasets.Facts),new JsonSerializerOptions {WriteIndented=true}));
            object parity=await Parity.Run(datasets);
            File.WriteAllText(Path.Combine(output,"parity.json"),JsonSerializer.Serialize(parity,new JsonSerializerOptions {WriteIndented=true}));
            BatchSample[] raw=await BenchmarkDriver.Run(datasets,output);
            File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(Summary.Build(raw),new JsonSerializerOptions {WriteIndented=true}));
            InputPins.Verify(args[0],args[1]);
            File.WriteAllText(Path.Combine(output,"closed.json"),JsonSerializer.Serialize(new {Passed=true,Batches=raw.Length,Observations=raw.Sum(x=>x.Observations),ProductionApproval=false,HostDeadlineClaim=false}));
            return 0;
        }
        catch(Exception error)
        {
            var closed=error as ClosedFailure;
            File.WriteAllText(Path.Combine(output,"failure.json"),JsonSerializer.Serialize(new {ErrorType=error.GetType().FullName,closed?.Check,closed?.AssertionFile,closed?.AssertionLine}));
            return 1;
        }
    }
}
