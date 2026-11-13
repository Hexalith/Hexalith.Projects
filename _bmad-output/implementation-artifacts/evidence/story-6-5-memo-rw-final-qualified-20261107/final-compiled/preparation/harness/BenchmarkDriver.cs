namespace Story65.ImageMemo.Harness;

/// <summary>Alternates all three variants while retaining every memo-cold, warmup and warm batch.</summary>
internal static class BenchmarkDriver
{
    internal const int Rounds = 6;
    internal const int WarmupIterations = 3;
    internal const int WarmIterations = 12;

    internal static BatchMeasurement[] Run(ImageInventory inventory, string outputDirectory)
    {
        var samples = new List<BatchMeasurement>();
        using var writer = new RawSampleWriter(Path.Combine(outputDirectory, "benchmark-samples.jsonl"));
        foreach (string path in new[] { "domain", "technical" })
        {
            foreach (int readers in new[] { 1, 2, 4 })
            {
                for (int round = 0; round < Rounds; round++)
                {
                    string[] order = round switch
                    {
                        0 => ["production", "monitor-control", "rw-copy"],
                        1 => ["rw-copy", "monitor-control", "production"],
                        2 => ["monitor-control", "rw-copy", "production"],
                        3 => ["production", "rw-copy", "monitor-control"],
                        4 => ["rw-copy", "production", "monitor-control"],
                        5 => ["monitor-control", "production", "rw-copy"],
                        _ => throw new InvalidOperationException("closed-balanced-round"),
                    };
                    for (int offset = 0; offset < order.Length; offset++)
                    {
                        using IMemoHost host = Hosts.Create(order[offset]);
                        Require.That(host.Count == 0 && host.RetainedBytes == 0 && !host.IsClosed, "new-empty-owner");
                        Capture("memo-cold", 1);
                        Capture("warmup", WarmupIterations);
                        Capture("warm", WarmIterations);

                        void Capture(string phase, int iterations)
                        {
                            BatchMeasurement sample = BatchRunner.Run(host, inventory, path, phase, round, offset, readers, iterations);
                            samples.Add(sample);
                            writer.Write(sample);
                        }
                    }
                }
            }
        }
        return samples.ToArray();
    }
}
