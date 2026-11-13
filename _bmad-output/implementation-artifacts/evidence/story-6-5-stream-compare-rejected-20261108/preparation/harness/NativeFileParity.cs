using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Candidate = Story65.ImageMemo.Candidate;
namespace Story65.ImageMemo.Harness;
internal static class NativeFileParity
{
    internal static object Run(Assembly key, string directory)
    {
        Require.That(!Directory.Exists(directory), "fresh-native-file-fixtures"); Directory.CreateDirectory(directory);
        byte[] original = Enumerable.Range(0, 100003).Select(i => (byte)(i * 31)).ToArray();
        var records = new List<object>(); var signatures = new Dictionary<string,List<string>>();
        try
        {
            foreach (string variant in Hosts.Names)
            {
                signatures[variant] = [];
                foreach (string scenario in new[] { "same", "tail-mutation", "shrink-after-hint", "growth-after-hint", "empty-after-hint", "closed-after-hint", "fault-after-hint", "grow-over-limit" })
                {
                    string path = Path.Combine(directory, variant + ".bin"); File.WriteAllBytes(path, original);
                    using IMemoHost host = Hosts.Create(variant); Require.That(host.TryCompute(key, original, out _, out _), "native-seed");
                    using IObservation scope = host.BeginObservation();
                    byte[] expected = original;
                    if (scenario == "tail-mutation") { expected = original.ToArray(); expected[^1] ^= 1; File.WriteAllBytes(path, expected); }
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var pool = new MutationPool(() =>
                    {
                        if (scenario == "shrink-after-hint") expected = original.AsSpan(0, 7013).ToArray();
                        if (scenario == "growth-after-hint") expected = [.. original, .. new byte[100019]];
                        if (scenario == "empty-after-hint") expected = [];
                        if (scenario == "grow-over-limit") expected = new byte[16 * 1024 * 1024 + 19];
                        if (scenario is "shrink-after-hint" or "growth-after-hint" or "empty-after-hint" or "grow-over-limit") File.WriteAllBytes(path, expected);
                        if (scenario == "closed-after-hint") scope.Dispose();
                        if (scenario == "fault-after-hint") stream.Dispose();
                    });
                    long allocated = GC.GetAllocatedBytesForCurrentThread(); long began = Stopwatch.GetTimestamp();
                    string? digest = null, errorType = null;
                    try { digest = host.ComputeStream(key, stream, pool); }
                    catch (Exception error) { errorType = error.GetType().FullName; }
                    long elapsed = Stopwatch.GetTimestamp() - began; allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    if (scenario == "fault-after-hint") Require.That(errorType == typeof(ObjectDisposedException).FullName && host.Count == 1, "native-real-disposed-read-fault");
                    else Require.That(errorType is null && digest == Convert.ToHexStringLower(SHA256.HashData(expected)) && stream.Position == expected.Length, "native-full-fresh-output-and-position");
                    Require.That(pool.Outstanding == 0 && pool.Rents == pool.Returns, "native-pool-return-on-every-path");
                    if (scenario == "same") Require.That(scope.CacheHits == 1, "native-warm-one-final-hit");
                    if (scenario is "tail-mutation" or "shrink-after-hint" or "growth-after-hint" or "empty-after-hint" or "closed-after-hint" or "grow-over-limit") Require.That(scope.CacheHits == 0, "native-decline-does-not-invent-hit");
                    if (scenario is "empty-after-hint" or "grow-over-limit") Require.That(host.Count == 1 && host.RetainedBytes == original.Length + 512, "native-nonretained-old-bounds-unchanged");
                    signatures[variant].Add(scenario + ":" + (errorType ?? digest));
                    records.Add(new { Variant = variant, Scenario = scenario, ElapsedTicks = elapsed, Stopwatch.Frequency,
                        AllocatedBytes = allocated, scope.CacheHits, host.Count, host.RetainedBytes, pool.Rents, pool.Returns,
                        HashMatchesIndependentFreshBytes = errorType is null, ErrorType = errorType,
                        FixtureMutationMayBeInsideTimedRegion = scenario.Contains("after-hint", StringComparison.Ordinal),
                        NotSteadyStateLatencyOrHostCaptureTiming = true });
                }
            }
            Require.That(signatures.Values.All(rows => rows.SequenceEqual(signatures["production"])), "native-actual-control-candidate-outcome-parity");
            using (var memo = new Candidate.RouteCatalogImageDigestMemo())
            using (var scope = Candidate.RouteCatalogImageDigestScope.Enter(memo))
            {
                memo.TryCompute(key, original, out _, out _); object? token = null;
                Require.That(scope.TryMatchNativeChunk(key, original.AsSpan(0, 65536), 0, ref token), "token-first-hot-chunk");
                Require.That(token is not null && token.GetType() == typeof(object) && token.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0, "opaque-token-has-no-entry-or-bytes");
                byte[] other = original.ToArray(); other[^1] ^= 1; memo.TryCompute(key, other, out _, out _); memo.TryCompute(key, original, out _, out _);
                Require.That(!scope.TryMatchNativeChunk(key, original.AsSpan(65536), 65536, ref token) && !scope.TryCompleteNativeMatch(key, token!, original.Length, out _), "replacement-back-to-identical-bytes-still-invalidates-old-token");
                token = null; Require.That(scope.TryMatchNativeChunk(key, original, 0, ref token), "full-fresh-token-match");
                long started = Stopwatch.GetTimestamp(); memo.Dispose(); long disposeTicks = Stopwatch.GetTimestamp() - started;
                Require.That(memo.IsClosed && memo.Count == 0 && !scope.TryCompleteNativeMatch(key, token!, original.Length, out _), "close-no-retained-entry-token-claim");
                records.Add(new { CandidateShortLocksNoIoProtectionRetained = true, DisposeTicks = disposeTicks,
                    Stopwatch.Frequency, OpaqueVersionReplacementAndCloseChecks = true,
                    ActualEnabledNativeFileEventListenerReentryStillRequiresSeparateCheck = true });
            }
            return new { Passed = true, Records = records, OriginalReadRequestExpressionsRetainedInSource = true,
                WholeOpenAndScopeTimingCoveredByMainCohortBatches = true,
                NativeObserverReentryNotClaimedTestedByTokenUnitChecks = true };
        }
        finally { Directory.Delete(directory, true); }
    }
}
