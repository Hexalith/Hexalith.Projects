using System.Text.Json;
namespace Story65.Canonical.Harness;
/// <summary>Balances three cyclic variant orders, each variant in each position; compares entire operations, not canonical-writer-only loops.</summary>
internal static class BenchmarkDriver
{
    internal static async Task<BatchSample[]> Run(Dataset[] datasets, string output)
    {
        string[][] orders = [["production","control","candidate"], ["control","candidate","production"],
            ["candidate","production","control"]];
        var all = new List<BatchSample>();
        using var writer = new StreamWriter(Path.Combine(output, "raw-batches.jsonl"), append: false);
        foreach (var dataset in datasets)
        foreach (string workload in new[] { "inventory", "identity" })
        foreach (string state in new[] { "memo-cold", "warm" })
        foreach (int readers in new[] { 1, 2, 4 })
        for (int round = 0; round < orders.Length; round++)
        foreach (string variant in orders[round])
        {
            BatchSample sample = await BatchRunner.Run(dataset, workload, state, variant, readers, round+1);
            all.Add(sample);
            await writer.WriteLineAsync(JsonSerializer.Serialize(sample));
            await writer.FlushAsync();
        }
        Check.That(all.Count == 216 && all.Sum(sample => sample.Observations) == 16380, "complete-balanced-benchmark-matrix");
        return all.ToArray();
    }
}
