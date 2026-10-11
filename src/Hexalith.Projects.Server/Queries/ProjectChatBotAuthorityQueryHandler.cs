using System.Text.Json;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

namespace Hexalith.Projects.Server.Queries;

/// <summary>Returns current exact project grant evidence to a bound ChatBot requester.</summary>
public sealed class ProjectChatBotAuthorityQueryHandler(
    IAuthoritativeEventStreamReader reader, TimeProvider clock) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, Type> EventTypes = typeof(IProjectEvent).Assembly.GetTypes()
        .Where(static type => !type.IsAbstract && !type.IsInterface && typeof(IProjectEvent).IsAssignableFrom(type))
        .ToDictionary(static type => type.FullName!, StringComparer.Ordinal);
    private static readonly IReadOnlySet<string> RejectionTypes = typeof(IProjectEvent).Assembly.GetTypes()
        .Where(static type => !type.IsAbstract && !type.IsInterface && typeof(IRejectionEvent).IsAssignableFrom(type))
        .Select(static type => type.FullName!).ToHashSet(StringComparer.Ordinal);

    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => ResolveProjectChatBotAuthority.QueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ResolveProjectChatBotAuthority? request;
        try
        {
            request = JsonSerializer.Deserialize<ResolveProjectChatBotAuthority>(envelope.Payload, JsonOptions);
        }
        catch (JsonException)
        {
            return QueryResult.Failure("Forbidden");
        }

        if (request is null || envelope.Domain != Domain || envelope.QueryType != QueryType ||
            envelope.AuthenticatedWorkloadId != "chatbot" || !envelope.IsDelegated ||
            envelope.OriginalActorId != envelope.UserId || request.TenantId != envelope.TenantId ||
            request.ProjectId != envelope.AggregateId || request.PrincipalId != envelope.UserId ||
            request.ActorClass is not ("human" or "service" or "ai") ||
            string.IsNullOrWhiteSpace(request.Operation) || string.IsNullOrWhiteSpace(request.Origin))
        {
            return QueryResult.Failure("Forbidden");
        }

        AuthoritativeStreamReadResult read = await reader.ReadAsync(
            new AggregateIdentity(envelope.TenantId, Domain, envelope.AggregateId), cancellationToken).ConfigureAwait(false);
        if (!read.IsAuthoritative || read.Stream is not { } source || source.Head <= 0 ||
            source.ObservedAt > clock.GetUtcNow())
        {
            return QueryResult.Failure("Forbidden");
        }

        ProjectState state = ProjectState.Empty;
        try
        {
            var identity = new ProjectIdentity(envelope.TenantId, new ProjectId(envelope.AggregateId));
            foreach (var item in source.Events)
            {
                string eventTypeName = item.EventTypeName.Split(',', 2)[0].Trim();
                if (RejectionTypes.Contains(eventTypeName)) { continue; }
                if (!EventTypes.TryGetValue(eventTypeName, out Type? eventType) ||
                    JsonSerializer.Deserialize(item.Payload, eventType, JsonOptions) is not IProjectEvent projectEvent)
                {
                    return QueryResult.Failure("Forbidden");
                }

                state = ProjectStateApply.Apply(state, projectEvent, identity);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException or ArgumentException)
        {
            return QueryResult.Failure("Forbidden");
        }

        string scope = ChatBotGrantScope.For(request.PrincipalId, request.Operation, request.ActorClass, request.Origin);
        state.ChatBotGrants.TryGetValue(scope, out ProjectChatBotGrant? grant);
        bool allowed = state.IsCreated && state.Lifecycle == ProjectLifecycle.Active &&
            grant is not null &&
            !grant.Revoked && grant.ExpiresAt > clock.GetUtcNow();
        var result = new ProjectChatBotAuthorityResult(request.TenantId, request.PrincipalId,
            request.ProjectId, request.Operation, request.ActorClass, request.Origin, allowed,
            allowed ? grant!.GrantId : null, source.Head, source.ObservationId, source.ObservedAt,
            allowed ? grant!.ExpiresAt : null);
        return QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, JsonOptions),
            projectionType: null, metadata: new QueryResponseMetadata { Provenance = QueryResponseProvenance.HandlerComputed });
    }
}
