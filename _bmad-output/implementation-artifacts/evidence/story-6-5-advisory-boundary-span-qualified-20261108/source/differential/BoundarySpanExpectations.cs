namespace Story65.AdvisoryWaveProof;

// Source-branch expectations for the existing closed differential cases, independent
// of the adapter's selection implementation. Stage7 is the Index marker span.
internal static class BoundarySpanExpectations
{
    internal static void Check(ProofCase test, long[]? ticks)
    {
        if (ticks is null) return; // Driver separately proves exact eligible admission.
        int[] expected = [1, 2, 3, 5, 6, 7];
        if (test.Mutation is "deny-initial" or "invalid-payload" or "invalid-envelope" or "raw-33" or "clock-exception-identity")
            expected = [1];
        if (test.Mutation is "no-signal" or "file-only" or "deny-folder" or "folder-unavailable")
            expected = [1, 2, 5, 6];
        if (test.Mutation == "shared-deadline") expected = [1, 2, 7];
        if (test.Fault is "throw" or "dependency-cancel")
            expected = test.FaultWave switch
            {
                1 => [1],
                2 => [1, 2, 5, 6],
                3 => [1, 2, 7],
                _ => [1, 2, 3, 5, 6, 7],
            };
        if (test.Fault is "caller-cancel" or "uncooperative" or "uncooperative-caller")
            expected = test.FaultWave switch
            {
                1 => [1],
                2 => [1, 2],
                3 => [1, 2, 7],
                4 => [1, 2, 3, 7],
                5 => [1, 2, 3, 5, 7],
                _ => [1, 2, 3, 5, 6, 7],
            };
        CheckMask(ticks, expected);
    }

    internal static void CheckMask(long[] ticks, int[] expected)
    {
        TraceLog.Require(ticks.Length == 8 && ticks.All(value => value >= 0), "Boundary span shape or sign changed.");
        TraceLog.Require(ticks[0] > 0 && ticks[4] > 0, "Whole or terminal encoding interval missing.");
        foreach (int stage in new[] { 1, 2, 3, 5, 6, 7 })
            TraceLog.Require((ticks[stage] > 0) == expected.Contains(stage), "Expected source-boundary stage set changed.");
        TraceLog.Require(ticks.Skip(1).Sum() <= ticks[0], "Disjoint spans plus encoding exceeded Whole.");
    }
}
