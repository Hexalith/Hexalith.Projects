namespace Story65.AdvisoryWaveProof;

using Hexalith.EventStore.Contracts.Queries;

internal sealed record ProofOutcome(QueryResult? Result, Exception? Error, TraceLog Trace,
    TimeSpan Elapsed, bool CallerCanceled, bool HadChildAfterReturn, int StoreCount, byte[][] StoreBytes,
    string[] LogEntries, long Admitted, int FaultTriggered, bool ErrorTokenIsCaller);
