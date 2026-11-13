namespace Story65.DelegatedClientProof;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

internal sealed class ProbeDomainHandler(Func<CancellationToken, Task<QueryResult>> invoke) : IDomainQueryHandler
{
    public string Domain => "projects";
    public string QueryType => "synthetic-owner-proof";
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken) => invoke(cancellationToken);
}
