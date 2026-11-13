namespace Story65.ImageMemo.Harness;
using System.Reflection;
using System.Security.Cryptography;

internal static class Parity
{
    private const int MiB = 1024 * 1024;
    internal static async Task<ParityReport> RunAsync(Assembly[] inventory, string scratchDirectory)
    {
        if (inventory.Length != 67 || Directory.Exists(scratchDirectory)) throw new ParityFailure("fresh-67-image-input-required");
        Directory.CreateDirectory(scratchDirectory);
        int checks = 0, passed = 0;
        void Check(bool condition, string closed)
        { checks++; if (!condition) throw new ParityFailure(closed); passed++; }
        static string Digest(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        byte[] data = Enumerable.Range(0, 100003).Select(index => (byte)(index * 31)).ToArray();
        Assembly key = inventory[0];
        Dictionary<string, string[]> technicalOutcomes = [];
        try
        {
            foreach (string name in Hosts.Names)
            {
                using (IMemoHost host = Hosts.Create(name))
                {
                    Check(host.Name == name && (name == "oracle" ? host.MemoType.Namespace == "Hexalith.EventStore.ServiceDefaults.Routing"
                        : host.MemoType.Namespace == "Story65.ImageMemo." + (name == "control" ? "Control" : "Candidate")), "selected-host-path");
                    using IObservation scope = host.BeginObservation();
                    Check(scope.ScopeType == host.ScopeType, "selected-scope-path");
                    var pool = new PoisonPool();
                    foreach ((int chunk, bool seek, long? length) in new[] { (17, false, (long?)null), (4093, true, (long?)4096), (65536, true, (long?)200000) })
                    {
                        using var stream = new ReplayStream(data, chunk, seek, length);
                        Check(host.ComputeStream(key, stream, pool) == Digest(data) && stream.Consumed == data.Length && stream.EofReads > 0, "full-eof-short-growth-shrink");
                        Check(pool.Outstanding == 0 && pool.Rents == pool.Returns, "complete-pool-custody");
                    }
                    Check(host.Count == 1 && host.RetainedBytes == data.Length + 512 && scope.CacheHits == 2, "selected-cold-warm-owned-memo");
                    using (host.SuppressCurrent())
                    {
                        using var stream = new ReplayStream(data, 13, false);
                        int hits = scope.CacheHits;
                        Check(host.ComputeStream(new MemoKey(), stream) == Digest(data) && stream.EofReads > 0 && scope.CacheHits == hits && host.Count == 1, "foreign-suppression-fresh-fallback");
                    }
                    byte[] changed = data.ToArray(); changed[^1] ^= 1;
                    using (var stream = new ReplayStream(changed)) Check(host.ComputeStream(key, stream, pool) == Digest(changed), "same-length-fresh-byte-mutation");
                    using (var stream = new ReplayStream(data)) Check(host.ComputeStream(key, stream, pool) == Digest(data), "owned-copy-survives-pool-poison-and-replacement");
                    using var failing = new ReplayStream(data, 31, false, failAfter: 1000);
                    bool threw = false;
                    try { host.ComputeStream(new MemoKey(), failing, pool); } catch (IOException) { threw = true; }
                    Check(threw && host.Count == 1 && pool.Outstanding == 0 && pool.Rents == pool.Returns, "failed-read-retains-no-partial-and-returns-pool");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    byte[] original = data.ToArray();
                    Check(host.TryCompute(key, original, out string first, out bool reused) && !reused && first == Digest(data), "memo-cold-digest");
                    original.AsSpan().Fill(3);
                    Check(host.TryCompute(key, data, out string second, out reused) && reused && second == first, "private-copy-never-input-alias");
                    using IObservation scope = host.BeginObservation();
                    using var stream = new ReplayStream(data, 19, false, atEnd: host.Dispose);
                    Check(host.ComputeStream(key, stream) == Digest(data) && stream.Consumed == data.Length && stream.EofReads > 0, "close-during-fresh-read-keeps-eof-and-hash");
                    host.Dispose();
                    Check(host.IsClosed && host.Count == 0 && host.RetainedBytes == 0 && !host.TryCompute(key, data, out string empty, out reused)
                        && empty == string.Empty && !reused, "postclose-getters-repeat-dispose-safe-fallback");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    IObservation scope = host.BeginObservation();
                    TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    Task<string> escaped = Task.Run(async () => { await ready.Task.ConfigureAwait(false); using var source = new ReplayStream(data, 7, false); return host.ComputeStream(key, source); });
                    scope.Dispose(); ready.SetResult();
                    Check(await escaped.ConfigureAwait(false) == Digest(data) && host.Count == 0, "escaped-closed-scope-cannot-retain");
                }
                using (IMemoHost first = Hosts.Create(name))
                using (IMemoHost second = Hosts.Create(name))
                {
                    using (first.BeginObservation()) { using var s = new ReplayStream(data); Check(first.ComputeStream(key, s) == Digest(data), "independent-first-owner"); }
                    byte[] other = data.ToArray(); other[1] ^= 1;
                    using (second.BeginObservation()) { using var s = new ReplayStream(other); Check(second.ComputeStream(key, s) == Digest(other), "independent-second-owner"); }
                    Check(first.Count == second.Count && first.Count == 1 && first.RetainedBytes == second.RetainedBytes, "independent-owner-bounds");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    byte[] maximum = new byte[16 * MiB]; maximum[^1] = 1;
                    Check(host.TryCompute(key, maximum, out string hash, out bool reused) && hash == Digest(maximum) && !reused && host.RetainedBytes == maximum.Length + 512, "exact-image-bound");
                    Check(!host.TryCompute(new MemoKey(), new byte[maximum.Length + 1], out _, out _) && host.Count == 1, "oversized-image-no-retention");
                    using IObservation scope = host.BeginObservation();
                    byte[] growing = new byte[maximum.Length + 19]; growing[^1] = 7;
                    using var source = new ReplayStream(growing, 65521, true, maximum.Length);
                    Check(host.ComputeStream(new MemoKey(), source) == Digest(growing) && source.Consumed == growing.Length && source.EofReads > 0 && host.Count == 1,
                        "oversized-growth-prefix-plus-original-remainder");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    int size = 16 * MiB - 512;
                    byte[] large = new byte[size];
                    Assembly[] keys = Enumerable.Range(0, 5).Select(_ => (Assembly)new MemoKey()).ToArray();
                    for (int i = 0; i < 4; i++) Check(host.TryCompute(keys[i], large, out _, out _), "charged-total-fill");
                    Check(host.Count == 4 && host.RetainedBytes == 64 * MiB && !host.TryCompute(keys[4], [1], out _, out _), "charged-total-includes-headers");
                    Check(host.TryCompute(keys[0], large.AsSpan(0, size - 513), out _, out _) && host.TryCompute(keys[4], [1], out _, out _)
                        && host.RetainedBytes == 64 * MiB && host.Count == 5, "replace-shrink-frees-exact-charge");
                    Check(!host.TryCompute(keys[0], large.AsSpan(0, size - 512), out _, out _) && host.RetainedBytes == 64 * MiB, "replacement-growth-cannot-exceed-total");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    for (int i = 0; i < 127; i++) Check(host.TryCompute(new MemoKey(), [1], out _, out _), "entry-count-fill");
                    using var start = new ManualResetEventSlim(); using var ready = new CountdownEvent(2);
                    Task<bool>[] tails = Enumerable.Range(0, 2).Select(slot => Task.Run(() => { using var observation = host.BeginObservation(); ready.Signal(); start.Wait(); return host.TryCompute(new MemoKey(), [2], out _, out _); })).ToArray();
                    Check(ready.Wait(TimeSpan.FromSeconds(2)), "both-count-race-workers-ready");
                    start.Set(); bool[] accepted = await Task.WhenAll(tails).ConfigureAwait(false);
                    Check(accepted.Count(value => value) == 1 && host.Count == 128 && host.RetainedBytes == 128 * 513, "concurrent-128th-129th-count-bound");
                }
                foreach (bool different in new[] { false, true })
                {
                    using IMemoHost host = Hosts.Create(name); using var start = new ManualResetEventSlim(); using var ready = new CountdownEvent(2);
                    byte[][] inputs = [data, different ? data.Select(value => (byte)(value ^ 1)).ToArray() : data];
                    Task<(bool Reused, string Digest)>[] calls = Enumerable.Range(0, 2).Select(index => Task.Run(() =>
                    { using var scope = host.BeginObservation(); ready.Signal(); start.Wait(); bool okay = host.TryCompute(key, inputs[index], out string hash, out bool reused); if (!okay) throw new ParityFailure("cold-race-no-publication"); return (reused, hash); })).ToArray();
                    Check(ready.Wait(TimeSpan.FromSeconds(2)), "both-cold-race-workers-ready");
                    start.Set(); var result = await Task.WhenAll(calls).ConfigureAwait(false);
                    Check(result.Select((value, index) => value.Digest == Digest(inputs[index])).All(value => value) && host.Count == 1 && host.RetainedBytes == data.Length + 512, "concurrent-cold-same-or-different-key-bytes");
                    if (!different) Check(result.Count(value => value.Reused) == 1, "cold-same-key-second-reuses-after-write-recheck");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    Assembly[] keys = [new MemoKey(), new MemoKey()];
                    byte[][] inputs = [data, [.. data, 17]];
                    using var start = new ManualResetEventSlim(); using var ready = new CountdownEvent(2);
                    Task<(bool Accepted, bool Reused, string Hash)>[] calls = Enumerable.Range(0, 2).Select(index => Task.Run(() =>
                    {
                        using var scope = host.BeginObservation(); ready.Signal(); start.Wait();
                        bool accepted = host.TryCompute(keys[index], inputs[index], out string hash, out bool reused);
                        return (accepted, reused, hash);
                    })).ToArray();
                    Check(ready.Wait(TimeSpan.FromSeconds(2)), "both-distinct-cold-keys-ready"); start.Set();
                    var result = await Task.WhenAll(calls).ConfigureAwait(false);
                    Check(result.Select((value, index) => value.Accepted && !value.Reused && value.Hash == Digest(inputs[index])).All(value => value)
                        && host.Count == 2 && host.RetainedBytes == inputs.Sum(bytes => bytes.Length + 512), "distinct-key-cold-publications-exact-bounds");
                }
                // End-to-end overlapping miss/close races: accept either legal linearization, never delivery-order assumptions.
                for (int race = 0; race < 12; race++)
                {
                    using IMemoHost host = Hosts.Create(name); host.TryCompute(key, data, out _, out _);
                    byte[] changed = data.ToArray(); changed[^1] ^= 1; using var start = new ManualResetEventSlim(); using var ready = new CountdownEvent(2);
                    Task<(bool Accepted, string Hash)> compute = Task.Run(() => { using var scope = host.BeginObservation(); ready.Signal(); start.Wait(); bool accepted = host.TryCompute(key, changed, out string hash, out _); return (accepted, hash); });
                    Task close = Task.Run(() => { ready.Signal(); start.Wait(); host.Dispose(); });
                    Check(ready.Wait(TimeSpan.FromSeconds(2)), "both-close-race-workers-ready"); start.Set();
                    var value = await compute.ConfigureAwait(false); await close.ConfigureAwait(false);
                    Check((value.Accepted ? value.Hash == Digest(changed) : value.Hash == string.Empty) && host.IsClosed && host.Count == 0 && host.RetainedBytes == 0,
                        "overlapping-readmiss-write-close-linearization");
                    Check(!host.TryCompute(key, changed, out _, out _), "closed-race-no-late-publication");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    host.TryCompute(key, data, out _, out _); object gate = host.GateForTesting;
                    if (gate is ReaderWriterLockSlim rw) rw.EnterReadLock(); else Monitor.Enter(gate);
                    Task close;
                    try
                    {
                        using var entered = new ManualResetEventSlim();
                        close = Task.Run(() => { entered.Set(); host.Dispose(); });
                        Check(entered.Wait(TimeSpan.FromSeconds(2)) && !close.Wait(TimeSpan.FromMilliseconds(25)), "dispose-waits-for-active-protection");
                        if (gate is ReaderWriterLockSlim readers)
                            Check(SpinWait.SpinUntil(() => readers.WaitingWriteCount > 0, TimeSpan.FromSeconds(2)) && !close.IsCompleted,
                                "dispose-actually-waits-for-active-reader");
                    }
                    finally { if (gate is ReaderWriterLockSlim held) held.ExitReadLock(); else Monitor.Exit(gate); }
                    await close.ConfigureAwait(false);
                    Check(host.IsClosed && host.Count == 0, "dispose-completes-after-protection-drains");
                }
                using (IMemoHost host = Hosts.Create(name))
                {
                    string imagePath = Path.Combine(scratchDirectory, name + ".dll"); byte[] bytes = File.ReadAllBytes(key.Location); File.WriteAllBytes(imagePath, bytes);
                    var image = new ImageAssembly(key, imagePath); var outcomes = new List<string>(); using var scope = host.BeginObservation();
                    outcomes.Add(host.ComputeTechnical(image)); bytes[0x20] ^= 1; File.WriteAllBytes(imagePath, bytes); outcomes.Add(host.ComputeTechnical(image));
                    Check(outcomes[0] != outcomes[1] && outcomes[1].EndsWith(Digest(bytes), StringComparison.Ordinal), "technical-fullimage-read-hash-mutation");
                    File.WriteAllBytes(imagePath, bytes.AsSpan(0, 32).ToArray());
                    try { host.ComputeTechnical(image); throw new ParityFailure("truncated-pe-must-fail"); }
                    catch (Exception error) when (error is not ParityFailure) { outcomes.Add(error.GetType().FullName!); }
                    Assembly other = inventory.First(assembly => assembly.ManifestModule.ModuleVersionId != key.ManifestModule.ModuleVersionId);
                    File.WriteAllBytes(imagePath, File.ReadAllBytes(other.Location));
                    try { host.ComputeTechnical(image); throw new ParityFailure("mvid-mismatch-must-fail"); }
                    catch (Exception error) when (error is not ParityFailure) { outcomes.Add(error.GetType().FullName!); }
                    technicalOutcomes[name] = outcomes.ToArray();
                }
            }
            Check(technicalOutcomes.Values.All(values => values.SequenceEqual(technicalOutcomes["oracle"], StringComparer.Ordinal)), "oracle-control-candidate-technical-output-error-parity");
            return new(checks, passed, checks == passed);
        }
        finally { Directory.Delete(scratchDirectory, true); }
    }
}
