using System.Text.Json;

using Story65.TechnicalFingerprint.Harness;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Balances all variants in each cyclic position without selecting a favorable endpoint subset.</summary>
internal static class BenchmarkDriver
{
    internal static async Task<BatchSample[]> Run(MvcCorpus corpus, string output)
    {
        Check.That(VariantHosts.Names.SequenceEqual(new[] { "actual", "control", "candidate" }), "frozen-three-variant-api");
        string[][] orders = [["actual", "control", "candidate"], ["control", "candidate", "actual"], ["candidate", "actual", "control"]];
        var all = new List<BatchSample>();
        using var writer = new StreamWriter(Path.Combine(output, "raw-batches.jsonl"), append: false);
        foreach (string state in new[] { "memo-cold", "warm" })
        foreach (int readers in new[] { 1, 2, 4 })
        for (int round = 0; round < orders.Length; round++)
        foreach (string variant in orders[round])
        {
            BatchSample sample = await BatchRunner.Run(corpus, variant, state, readers, round + 1);
            all.Add(sample);
            await writer.WriteLineAsync(JsonSerializer.Serialize(sample));
            await writer.FlushAsync();
        }
        Check.That(all.Count == 54 && all.Sum(sample => sample.Observations) == 2079, "complete-bounded-balanced-matrix");
        return all.ToArray();
    }
}
