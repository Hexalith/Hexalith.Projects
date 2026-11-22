namespace Hexalith.Projects.Server.Queries;

using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Server.Projections.ProjectReads;

/// <summary>Reads committed operator facts under the current identity and exact target authority.</summary>
public sealed class ProjectOperatorQueryExecutor(IReadModelStore store, ProjectAuthorizationGate gate,
    ProjectQueryEnvelopePrincipalBinding binding, IProjectAuditTimelineReadModel timeline, ProjectReadAudit audit,
    IQueryCursorCodec cursors, TimeProvider clock, IProjectTaskStatusAuthority? tasks = null)
{
    /// <summary>Reads a bounded, authorized Project audit page.</summary>
    public async Task<QueryResult> AuditAsync(QueryEnvelope query, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!await HasOperatorScopeAsync(query, GetProjectAuditQuery.QueryType, token).ConfigureAwait(false))
            return await DenyAsync(query, token).ConfigureAwait(false);
        if (!ValidTarget(query) || !ProjectReadQueryExecutor.TryPayload(query, ["pageSize", "cursor"], out GetProjectAuditQuery? input)
            || input is null || input.PageSize is < 1 or > 200 || input.Cursor?.Length > 4096
            || input.Cursor is not null && string.IsNullOrWhiteSpace(input.Cursor))
            return QueryResult.Failure(QueryAdapterFailureReason.InvalidEnvelope);

        ProjectAuthorizationResult? authority = await AuthorizeProjectAsync(query, token).ConfigureAwait(false);
        if (authority is null) return await DenyAsync(query, token).ConfigureAwait(false);
        string scope = CursorScope(query, input.PageSize, authority);
        if (!cursors.TryDecode(input.Cursor, query.QueryType, scope, out string? position, out _))
            return QueryResult.Failure(QueryAdapterFailureReason.InvalidCursor);
        DateTimeOffset now = clock.GetUtcNow();
        ProjectAuditCursorPosition? boundary = null;
        if (position is not null)
        {
            try { boundary = JsonSerializer.Deserialize<ProjectAuditCursorPosition>(position); }
            catch (JsonException) { return QueryResult.Failure(QueryAdapterFailureReason.InvalidCursor); }
            if (boundary is null || boundary.ExpiresAt <= now) return QueryResult.Failure(QueryAdapterFailureReason.InvalidCursor);
        }

        DateTimeOffset snapshot = boundary?.SnapshotAt ?? now;
        using IncrementalHash revision = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        SortedSet<ProjectAuditFact> rows = new(Comparer<ProjectAuditFact>.Create((left, right) =>
        {
            int time = right.OccurredAt.CompareTo(left.OccurredAt);
            return time == 0 ? string.CompareOrdinal(left.AuditEventId, right.AuditEventId) : time;
        }));
        void Observe(ProjectAuditFact fact)
        {
            if (fact.OccurredAt > snapshot) return;
            revision.AppendData(JsonSerializer.SerializeToUtf8Bytes(fact));
            if (boundary is not null && (fact.OccurredAt > boundary.OccurredAt
                || fact.OccurredAt == boundary.OccurredAt && string.CompareOrdinal(fact.AuditEventId, boundary.AuditEventId) <= 0)) return;
            rows.Add(fact);
            if (rows.Count > input.PageSize + 1) rows.Remove(rows.Max!);
        }

        bool domainAvailable = true;
        try
        {
            var history = await timeline.ListAsync(query.TenantId, query.AggregateId, null, token).ConfigureAwait(false);
            HashSet<string> committedIds = new(StringComparer.Ordinal);
            foreach (ProjectAuditTimelineItem row in history.Reverse())
            {
                if (row.TenantId != query.TenantId || row.ProjectId != query.AggregateId || row.OccurredAt == default)
                    throw new InvalidDataException("Invalid audit scope.");
                if (ProjectAuditTimelineProjection.ToSupportedFact(row) is { } fact && committedIds.Add(fact.AuditEventId)) Observe(fact);
            }
        }
        catch (Exception) when (!token.IsCancellationRequested) { rows.Clear(); domainAvailable = false; }

        bool securityAvailable = true;
        try
        {
            await foreach (ProjectReadAuditRecord row in audit.ReadAsync(query.TenantId, query.AggregateId, token).ConfigureAwait(false))
            {
                bool inspected = row.Outcome == "Inspection";
                Observe(new("audit_" + row.RequestSurrogate, inspected ? "metadata.inspected" : "authorization.denied",
                    row.Outcome, row.RecordedAt, ProjectCount: inspected ? row.ProjectCount : null,
                    FieldClass: inspected ? row.FieldClass : null));
            }
        }
        catch (Exception) when (!token.IsCancellationRequested) { securityAvailable = false; }

        if (!await HasOperatorScopeAsync(query, GetProjectAuditQuery.QueryType, token).ConfigureAwait(false)
            || await AuthorizeProjectAsync(query, token).ConfigureAwait(false) is not { } final
            || scope != CursorScope(query, input.PageSize, final)) return await DenyAsync(query, token).ConfigureAwait(false);

        if (!securityAvailable) return Success(new ProjectAuditPage("Unavailable", [], null));
        revision.AppendData(domainAvailable ? "domain-observed"u8 : "domain-unavailable"u8);
        string digest = Convert.ToHexString(revision.GetHashAndReset());
        if (boundary is not null && boundary.Revision != digest) return QueryResult.Failure(QueryAdapterFailureReason.InvalidCursor);

        ProjectAuditFact[] items = rows.Take(input.PageSize).ToArray();
        string? next = rows.Count > items.Length ? cursors.Encode(query.QueryType, scope, JsonSerializer.Serialize(
            new ProjectAuditCursorPosition(snapshot, items[^1].OccurredAt, items[^1].AuditEventId, digest, boundary?.ExpiresAt ?? now.AddMinutes(15)))) : null;
        // Retained v1 inspection facts without target bindings and unavailable workflow producers preclude complete coverage.
        return Success(new ProjectAuditPage(!domainAvailable && items.Length == 0 ? "Unavailable" : "Partial", items, next));
    }

    /// <summary>Reports transition-authority availability without disclosing a task or reserved Project.</summary>
    public async Task<QueryResult> TaskStatusAsync(QueryEnvelope query, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!await HasTaskScopeAsync(query, token).ConfigureAwait(false))
            return await DenyAsync(query, token).ConfigureAwait(false);
        if (!ValidTarget(query) || !ProjectReadQueryExecutor.TryPayload<GetProjectTaskStatusQuery>(query, [], out _))
            return QueryResult.Failure(QueryAdapterFailureReason.InvalidEnvelope);

        if (tasks is null) return TaskUnavailable();
        ProjectAuthorizedTaskObservation? authorized;
        try { authorized = await tasks.ReadAuthorizedAsync(query, token).ConfigureAwait(false); }
        catch (Exception) when (!token.IsCancellationRequested) { return TaskUnavailable(); }
        if (!await HasTaskScopeAsync(query, token).ConfigureAwait(false) || authorized is null || authorized.TenantId != query.TenantId
            || authorized.ActorId != query.OriginalActorId
            || ProjectReadQueryAdmission.IsChatbot(query, binding) && authorized.AdmittedActorId != query.OriginalActorId
            || authorized.Result is null || authorized.Result.State == "Denied"
            || authorized.Result.State == "Available" && authorized.Result.TaskId != query.AggregateId)
            return await DenyAsync(query, token).ConfigureAwait(false);
        ProjectTaskStatusResponse observation = authorized.Result;
        if (observation.State != "Available"
            || observation.Status is not ("Pending" or "Running" or "WaitingForDependency" or "NeedsAttention" or "Succeeded" or "Rejected" or "Failed" or "Cancelled")
            || observation.Reason is not ("Admitted" or "InProgress" or "DependencyUnavailable" or "NeedsAttention" or "Confirmed" or "Denied" or "Failed" or "Cancelled" or "Reconciled")
            || observation.ReconciliationOutcome is not (null or "Resumed" or "Rejected")
            || observation.CreatedAt is null || observation.CreatedAt == default(DateTimeOffset) || observation.UpdatedAt is null
            || observation.UpdatedAt < observation.CreatedAt || observation.ExpiresAt < observation.CreatedAt
            || observation.RecoveryActions is null || observation.RecoveryActions.Count > AdmissionRecoveryAction.Values.Count
            || observation.RecoveryActions.Any(code => !AdmissionRecoveryAction.Values.Contains(code, StringComparer.Ordinal)))
            return TaskUnavailable();
        return Success(authorized.PreActivation ? observation with { ReconciliationOutcome = null } : observation);
    }

    private static QueryResult TaskUnavailable()
        => Success(new ProjectTaskStatusResponse("Unavailable", null, null, "TaskAuthorityUnavailable",
            [AdmissionRecoveryAction.Retry], null, null, null));

    private async Task<bool> HasTaskScopeAsync(QueryEnvelope query, CancellationToken token)
        => binding.HttpContext?.User.Identity?.IsAuthenticated == true
            && (await ProjectReadQueryAdmission.AuthorizeAsync(query, GetProjectTaskStatusQuery.QueryType, false,
                gate, binding, token).ConfigureAwait(false))?.IsAllowed == true;

    private async Task<bool> HasOperatorScopeAsync(QueryEnvelope query, string type, CancellationToken token)
        => binding.HttpContext?.User.Identity?.IsAuthenticated == true && query.AuthenticatedWorkloadId != "chatbot"
            && (await ProjectReadQueryAdmission.AuthorizeAsync(query, type, false, gate, binding, token).ConfigureAwait(false))?.IsAllowed == true;

    private async Task<ProjectAuthorizationResult?> AuthorizeProjectAsync(QueryEnvelope query, CancellationToken token)
    {
        if (!binding.TryBind(query, out var tenant) || binding.HttpContext is not { } http) return null;
        ProjectAuthorizationResult result = await gate.AuthorizeSupportedReadAsync(query.AggregateId, tenant, http,
            query.CorrelationId, null, async (tenantId, projectId, ct) =>
            {
                ProjectReadModel? model = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
                    ProjectReadModelKeys.Detail(tenantId, projectId), ct).ConfigureAwait(false)).Value;
                return model is { Freshness: EvidenceFreshnessState.Current, Detail: { } detail }
                    && model.TenantId == tenantId && model.ProjectId == projectId && detail.TenantId == tenantId
                    && detail.ProjectId == projectId ? detail : null;
            }, token).ConfigureAwait(false);
        return result.IsAllowed && result.TenantAccessResult is { FreshnessStatus: TenantProjectionFreshnessStatus.Fresh }
            && result.ProjectDetail is { } authorized && authorized.ProjectId == query.AggregateId
            && (!ProjectReadQueryExecutor.IsQuarantined(authorized) || ProjectReadQueryAdmission.IsTenantProjectAdministrator(binding)) ? result : null;
    }

    private static string CursorScope(QueryEnvelope query, int pageSize, ProjectAuthorizationResult authority)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            query.TenantId, query.OriginalActorId, query.AggregateId, query.QueryType, query.AuthenticatedWorkloadId,
            query.IsDelegated, query.DelegationId, query.Scopes, query.Audience, PageSize = pageSize,
            Action = ProjectAuthorizationGate.ReadProjectAction, TenantRevision = authority.TenantAccessResult?.ProjectionWatermark,
            ResourceRevision = authority.EventStoreValidationResult?.FreshnessWatermark, authority.ProjectDetail!.Sequence,
        })));

    private static bool ValidTarget(QueryEnvelope query)
        => ProjectReadQueryExecutor.SafeIdentity(query.TenantId, query.AggregateId) && query.Paging is null
            && (query.EntityId is null || query.EntityId == query.AggregateId);

    private async Task<QueryResult> DenyAsync(QueryEnvelope query, CancellationToken token)
    {
        string[] targets = query.QueryType == GetProjectAuditQuery.QueryType && binding.TryBind(query, out _)
            && ValidTarget(query) ? [query.AggregateId] : [];
        _ = await audit.RecordAsync(query, false, 0, null, false, token, targets).ConfigureAwait(false);
        return QueryResult.Failure("safe-denial");
    }

    private static QueryResult Success<T>(T result)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, JsonSerializerOptions.Web));
}
