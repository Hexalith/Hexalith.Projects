namespace Story65.AdvisoryWaveProof;

// Expected values are derived only from closed synthetic cases/source branches. They
// do not inspect business DTOs or turn ordinary response payloads into recorder data.
internal static class TerminalExpectations
{
    internal static void Check(ProofCase test, long[]? actual)
    {
        if (actual is null) return; // The driver separately requires exact eligible admission.
        long[] expected = [10, 0, 1, 0, 0, 0];
        if (test.Mutation is "invalid-payload" or "invalid-envelope" or "raw-33") expected = [11, 0, 4, 0, 0, 0];
        if (test.Mutation == "deny-initial") expected = [1, 0, 3, 0, 0, 0];
        if (test.Mutation == "revoke-scope") expected = [7, 0, 3, 0, 0, 0];
        if (test.Mutation == "file-only") expected[2] = 2;
        if (test.Mutation == "folder-unavailable") expected[2] = 2;
        if (test.Mutation == "clock-exception-identity") expected = [11, 0, 0, 0, 0, 0];
        if (test.Fault is "throw" or "dependency-cancel")
        {
            int kind = test.Fault == "dependency-cancel" ? 1 : 2;
            expected = test.FaultWave switch
            {
                1 => [1, 1, 5, kind, 0, 0],
                3 or 7 => [10, test.FaultWave, 6, kind, 0, 0],
                _ => [10, 0, 2, 0, 0, 0],
            };
        }
        if (test.Fault is "caller-cancel" or "uncooperative-caller")
            expected = [test.FaultWave, test.FaultWave, 7, 1, 1, 1];
        if (test.Fault == "uncooperative") expected = [10, test.FaultWave, 6, 1, 0, 1];
        if (test.Mutation == "shared-deadline") expected = [10, 3, 6, 1, 0, 1];
        TraceLog.Require(actual.SequenceEqual(expected), "Closed terminal source-branch expectation changed.");
    }
}
