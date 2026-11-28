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

        // These established query types keep the AD-19 compatibility mapping for invalid input.
        bool compatibility = query.QueryType == ResolveProjectChatBotAuthority.QueryType
            || query.QueryType is ProjectsServerModule.GetProjectContextQueryType
            or ProjectsServerModule.GetConversationStartSetupQueryType
            or ProjectsServerModule.ExplainContextSelectionQueryType;
        if (!compatibility && result.ErrorMessage is (QueryAdapterFailureReason.InvalidEnvelope or QueryAdapterFailureReason.InvalidCursor))
        {
            return Results.BadRequest(result);
        }

        // Keep the SDK's missing-handler diagnostic and the handler's coarse denial reason.
        return Results.NotFound(result with { PayloadBytes = null, Metadata = null, ProjectionType = null });
    }
}
