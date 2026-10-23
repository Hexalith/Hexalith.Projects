namespace Hexalith.Projects.Server.Tests;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

/// <summary>Exercises captured owner directories inside the SDK-verified query boundary.</summary>
/// <param name="execute">The request-local directory assertions.</param>
internal sealed class DelegatedDirectoryTestQueryHandler(Func<CancellationToken, Task<QueryResult>> execute) : IDomainQueryHandler
{
    /// <summary>The isolated test route.</summary>
    public const string Route = "delegated-directory-registration";

    /// <inheritdoc/>
    public string Domain => "projects";

    /// <inheritdoc/>
    public string QueryType => Route;

    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken = default)
        => execute(cancellationToken);
}
