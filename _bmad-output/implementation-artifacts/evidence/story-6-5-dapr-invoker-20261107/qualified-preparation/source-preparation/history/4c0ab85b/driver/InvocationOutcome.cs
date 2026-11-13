namespace Story65.DaprInvokerProof;

using Hexalith.EventStore.Contracts.Queries;

internal sealed record InvocationOutcome(QueryResult? Result, Exception? Error, FixtureState State,
    bool CallerCanceled, bool ErrorTokenIsCaller, bool WaitHeldForAcquire, bool WaitHeldForRelease);
