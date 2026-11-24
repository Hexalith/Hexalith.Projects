namespace Hexalith.Projects.Server.Queries;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Preserves the Projects query receiver's response contract.</summary>
internal sealed class ProjectsQueryResponsePolicy : IDomainQueryResponsePolicy
{
    /// <inheritdoc/>
    public IResult CreateResponse(QueryEnvelope query, QueryResult result)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(result);

        if (result.Success)
        {
            return Results.Ok(result);
        }

        if (query.QueryType is not (ListProjectsQuery.QueryType or GetProjectQuery.QueryType
            or GetProjectAuditQuery.QueryType or GetProjectTaskStatusQuery.QueryType
            or ResolveProjectFromConversationQuery.QueryType or ResolveProjectFromAttachmentsQuery.QueryType))
        {
            return Results.NotFound(result);
        }

        return result.ErrorMessage is QueryAdapterFailureReason.InvalidEnvelope or QueryAdapterFailureReason.InvalidCursor
            ? Results.BadRequest(result)
            : Results.NotFound(QueryResult.Failure("safe-denial"));
    }
}
