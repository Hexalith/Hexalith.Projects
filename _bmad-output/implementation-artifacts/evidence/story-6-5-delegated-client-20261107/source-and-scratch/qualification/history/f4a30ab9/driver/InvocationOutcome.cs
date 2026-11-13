namespace Story65.DelegatedClientProof;

using Hexalith.EventStore.Contracts.Queries;

internal sealed record InvocationOutcome(QueryResult? Result, Exception? Error, FixtureState State,
    bool CallerCanceled, bool ErrorTokenIsCaller, bool ChildAfterReturn, bool UnboundedHeldAfterCallerCancel, TimeSpan Elapsed);
