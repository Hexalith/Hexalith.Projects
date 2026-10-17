namespace Hexalith.Projects.Server.Queries;

using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;

/// <summary>Read-only, authorization-first list/open assembly over supported persisted models.</summary>
public sealed class ProjectReadQueryExecutor(
    IReadModelStore store,
    ProjectAuthorizationGate gate,
    ProjectQueryEnvelopePrincipalBinding binding,
    IProjectFolderDirectory folders,
    IQueryCursorCodec cursors,
    ProjectReadAudit audit,
    TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The separate per-tenant descriptive inspection permission.</summary>
    public const string InspectionPermission = "projects:inspect_descriptive_metadata";

    /// <summary>Executes an authenticated list, filtering Folder authority before pagination.</summary>
    public async Task<QueryResult> ListAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ProjectAuthorizationResult? scope = await AuthorizeScopeAsync(query, true, cancellationToken).ConfigureAwait(false);
        if (scope is null) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
        if (!TryPayload(query, ["lifecycleState", "cursor", "pageSize", "inspectDescriptiveMetadata"], out ListProjectsQuery? input)
            || input is null || input.PageSize is < 1 or > ListProjectsQuery.MaximumPageSize
            || input.LifecycleState is not (null or "Active" or "Archived") || query.Paging is not null
            || query.EntityId is not null && query.EntityId != query.AggregateId || query.AggregateId != "projects"
            || input.Cursor is { Length: > 4096 } || input.Cursor is not null && string.IsNullOrWhiteSpace(input.Cursor))
        {
            return Invalid();
        }

        bool chatbot = IsChatbot(query);
        bool descriptive = chatbot || input.InspectDescriptiveMetadata;
        if (!MayInspect(input.InspectDescriptiveMetadata, chatbot))
        {
            return await DenyAsync(query, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            ReadModelEntry<ProjectReadIndex> indexEntry = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName,
                ProjectReadModelKeys.Index(query.TenantId), cancellationToken).ConfigureAwait(false);
            ProjectReadIndex? index = indexEntry.Value;
            if (index is null || string.IsNullOrWhiteSpace(indexEntry.ETag) || index.TenantId != query.TenantId || index.GlobalPosition <= 0
                || index.LastVerifiedAt == default || index.UnavailableProjectIds.Count > 0
                || !index.ProjectIds.SequenceEqual(index.ProjectIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))
            {
                return Success(new ProjectListPage([], null, false, Unavailable(index?.LastVerifiedAt ?? default)), query.QueryType);
            }

            // Observe the whole filtered view before decoding a continuation, including identities before its boundary.
            // Owner revisions are unavailable, so each page pays for a complete current authorization sweep.
            List<ProjectListRow> visible = [];
            QueryCursorScope visibility = QueryCursorScope.Create();
            foreach (string projectId in index.ProjectIds)
            {
                if (!SafeIdentity(query.TenantId, projectId)) { return Success(new ProjectListPage([], null, false, Unavailable(index.LastVerifiedAt)), query.QueryType); }
                ProjectReadModel? model = await ReadAsync(query.TenantId, projectId, cancellationToken).ConfigureAwait(false);
                if (model?.Detail is not { } detail || model.TenantId != query.TenantId || model.ProjectId != projectId
                    || detail.TenantId != query.TenantId || detail.ProjectId != projectId || !Enum.IsDefined(detail.Lifecycle)
                    || !ValidObservation(model))
                {
                    return Success(new ProjectListPage([], null, false, Unavailable(index.LastVerifiedAt)), query.QueryType);
                }

                if (input.LifecycleState is not null && detail.Lifecycle.ToString() != input.LifecycleState) { continue; }
                ProjectAuthorizationResult? projectAuthorization = await AuthorizeProjectAsync(query, model, cancellationToken).ConfigureAwait(false);
                if (projectAuthorization is { IsAllowed: false } denied && (denied.Retryable || denied.Reason is ReferenceState.Stale or ReferenceState.Unavailable))
                {
                    return Success(new ProjectListPage([], null, false, Unavailable(index.LastVerifiedAt, "Authorization")), query.QueryType);
                }
                visibility.Add("project", projectId).Add("allowed", (projectAuthorization?.IsAllowed == true).ToString())
                    .Add("authorization", projectAuthorization?.EventStoreValidationResult?.FreshnessWatermark);
                if (projectAuthorization?.IsAllowed != true) { continue; }
                ReferenceState folderState = await FolderStateAsync(detail, cancellationToken, query.CorrelationId).ConfigureAwait(false);
                visibility.Add("folder", folderState.ToString());
                if (chatbot && folderState is ReferenceState.Stale or ReferenceState.Unavailable)
                {
                    return Success(new ProjectListPage([], null, false, Unavailable(index.LastVerifiedAt, "FolderVisibility",
                        folderState == ReferenceState.Stale ? EvidenceFreshnessState.Stale : EvidenceFreshnessState.Unavailable)), query.QueryType);
                }
                if (chatbot && folderState != ReferenceState.Included) { continue; }
                bool valid = ProjectPersistedDetailValidator.IsHeaderValid(detail);
                visible.Add(new(projectId, descriptive && valid ? detail.Name : null, detail.Lifecycle,
                    folderState, Snapshot(model, folderState)));
            }

            string visibilityFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(visibility.Build())));
            string cursorScope = Scope(query, input, indexEntry, scope, visibilityFingerprint);
            if (!cursors.TryDecode(input.Cursor, query.QueryType, cursorScope, out string? position, out _)) { return InvalidCursor(); }
            ProjectReadCursorPosition? continuation = null;
            if (position is not null)
            {
                try { continuation = JsonSerializer.Deserialize<ProjectReadCursorPosition>(position, JsonOptions); }
                catch (JsonException) { return InvalidCursor(); }
                if (continuation is null || !SafeIdentity(query.TenantId, continuation.ProjectId) || continuation.ExpiresAt <= clock.GetUtcNow()
                    || !visible.Any(row => row.ProjectId == continuation.ProjectId))
                {
                    return InvalidCursor();
                }
            }

            // The SDK ETag changes for every index update, including delayed positions below the tenant maximum.
            ReadModelEntry<ProjectReadIndex> after = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName,
                ProjectReadModelKeys.Index(query.TenantId), cancellationToken).ConfigureAwait(false);
            ProjectAuthorizationResult? finalScope = await AuthorizeScopeAsync(query, true, cancellationToken).ConfigureAwait(false);
            if (!StableScope(scope, finalScope)) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
            if (!SameIndex(indexEntry, after)) { return Success(new ProjectListPage([], null, false, Unavailable(index.LastVerifiedAt)), query.QueryType); }

            ProjectListRow[] remaining = visible.Where(row => continuation is null || string.CompareOrdinal(row.ProjectId, continuation.ProjectId) > 0)
                .Take(input.PageSize + 1).ToArray();
            bool hasMore = remaining.Length > input.PageSize;
            ProjectListRow[] rows = remaining.Take(input.PageSize).ToArray();
            string? next = hasMore ? cursors.Encode(query.QueryType, cursorScope,
                JsonSerializer.Serialize(new ProjectReadCursorPosition(rows[^1].ProjectId,
                    continuation?.ExpiresAt ?? clock.GetUtcNow().AddMinutes(15)), JsonOptions)) : null;
            if (descriptive && !await audit.RecordAsync(query, true, rows.Length, cursorScope, cancellationToken).ConfigureAwait(false))
            {
                return await DenyAsync(query, cancellationToken).ConfigureAwait(false);
            }

            return Success(new ProjectListPage(rows, next, hasMore, PageSnapshot(rows, index)), query.QueryType);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Success(new ProjectListPage([], null, false, Unavailable(default)), query.QueryType);
        }
    }

    /// <summary>Executes an authenticated singleton read without context resolution or domain writes.</summary>
    public async Task<QueryResult> GetAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ProjectAuthorizationResult? scope = await AuthorizeScopeAsync(query, false, cancellationToken).ConfigureAwait(false);
        if (scope is null) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
        if (!TryPayload(query, ["inspectDescriptiveMetadata"], out GetProjectQuery? input) || input is null
            || !SafeIdentity(query.TenantId, query.AggregateId) || query.EntityId is not null && query.EntityId != query.AggregateId
            || query.Paging is not null)
        {
            return Invalid();
        }

        bool chatbot = IsChatbot(query);
        bool descriptive = chatbot || input.InspectDescriptiveMetadata;
        if (!MayInspect(input.InspectDescriptiveMetadata, chatbot)) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
        ProjectReadModel? model;
        try { model = await ReadAsync(query.TenantId, query.AggregateId, cancellationToken).ConfigureAwait(false); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
        if (model?.Detail is not { } detail || model.TenantId != query.TenantId || model.ProjectId != query.AggregateId
            || detail.TenantId != query.TenantId || detail.ProjectId != query.AggregateId
            || !Enum.IsDefined(detail.Lifecycle))
        {
            return await DenyAsync(query, cancellationToken).ConfigureAwait(false);
        }

        ProjectAuthorizationResult? projectAuthorization = await AuthorizeProjectAsync(query, model, cancellationToken).ConfigureAwait(false);
        if (projectAuthorization?.IsAllowed != true) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
        ReferenceState folderState = await FolderStateAsync(detail, cancellationToken, query.CorrelationId).ConfigureAwait(false);
        if (chatbot && folderState != ReferenceState.Included) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }
        try
        {
            ProjectReadModel? after = await ReadAsync(query.TenantId, query.AggregateId, cancellationToken).ConfigureAwait(false);
            if (!SameModel(model, after)
                || !StableScope(scope, await AuthorizeScopeAsync(query, false, cancellationToken).ConfigureAwait(false)))
            {
                return await DenyAsync(query, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return await DenyAsync(query, cancellationToken).ConfigureAwait(false); }

        bool valid = ProjectPersistedDetailValidator.IsHeaderValid(detail);
        bool referencesValid = valid && ProjectPersistedDetailValidator.IsValid(detail);
        GetProjectResponse response = new(detail.ProjectId, descriptive && valid ? detail.Name : null,
            descriptive && valid ? detail.Description : null,
            detail.Lifecycle,
            valid ? detail.CreatedAt : default, valid ? detail.UpdatedAt : default,
            descriptive && valid ? detail.Setup ?? ProjectSetup.Empty : null,
            descriptive && valid ? detail.SetupMetadata : null,
            referencesValid ? References(detail, descriptive, folderState) : [], Snapshot(model, folderState));
        if (descriptive && !await audit.RecordAsync(query, true, 1, OpenObservation(model, scope, projectAuthorization, folderState), cancellationToken).ConfigureAwait(false))
        {
            return await DenyAsync(query, cancellationToken).ConfigureAwait(false);
        }

        return Success(response, query.QueryType);
    }

    private async Task<ProjectAuthorizationResult?> AuthorizeScopeAsync(QueryEnvelope query, bool list, CancellationToken cancellationToken)
    {
        if (query.Domain != ProjectsServerModule.DomainName
            || query.QueryType != (list ? ListProjectsQuery.QueryType : GetProjectQuery.QueryType)
            || string.IsNullOrWhiteSpace(query.OriginalActorId) || string.IsNullOrWhiteSpace(query.AuthenticatedWorkloadId)
            || query.IsDelegated && string.IsNullOrWhiteSpace(query.DelegationId)
            || !query.IsDelegated && query.DelegationId is not null
            || query.Scopes?.Contains(list ? "projects.list" : "projects.read", StringComparer.Ordinal) != true
            || string.IsNullOrWhiteSpace(binding.ExpectedAudience)
            || query.Audience?.Contains(binding.ExpectedAudience, StringComparer.Ordinal) != true
            || !binding.TryBind(query, out IProjectTenantContextAccessor tenantContext) || binding.HttpContext is not { } http)
        {
            return null;
        }

        ClaimsPrincipal principal = http.User;
        if (query.AuthenticatedWorkloadId == "chatbot" && !IsChatbot(query)) { return null; }
        if (!IsChatbot(query) && !HasRole(principal, "TenantOperator") && !HasRole(principal, "TenantProjectAdministrator")) { return null; }
        ProjectAuthorizationResult result = await gate.AuthorizeSupportedScopeAsync(list, tenantContext, http,
            query.CorrelationId, cancellationToken).ConfigureAwait(false);
        return result.IsAllowed && result.TenantAccessResult is { IsAllowed: true, FreshnessStatus: TenantProjectionFreshnessStatus.Fresh } tenant
            && tenant.TenantId == query.TenantId && !string.IsNullOrWhiteSpace(tenant.ProjectionWatermark) ? result : null;
    }

    private bool IsChatbot(QueryEnvelope query)
        => query.AuthenticatedWorkloadId == "chatbot" && query.IsDelegated && !string.IsNullOrWhiteSpace(query.DelegationId)
            && binding.HttpContext is { } http && HasRole(http.User, "ProjectUser");

    private static bool HasRole(ClaimsPrincipal principal, string role)
        => principal.IsInRole(role) || principal.HasClaim("role", role) || principal.HasClaim("projects:role", role);

    private bool MayInspect(bool requested, bool chatbot)
        => !requested || chatbot || binding.HttpContext?.User.HasClaim("eventstore:permission", InspectionPermission) == true;

    private async Task<ProjectAuthorizationResult?> AuthorizeProjectAsync(QueryEnvelope query, ProjectReadModel model, CancellationToken cancellationToken)
    {
        if (!binding.TryBind(query, out IProjectTenantContextAccessor tenantContext) || binding.HttpContext is not { } http) { return null; }
        ProjectAuthorizationResult result = await gate.AuthorizeSupportedReadAsync(model.ProjectId, tenantContext,
            http, query.CorrelationId, null, async (tenant, project, token) =>
                (await ReadAsync(tenant, project, token).ConfigureAwait(false))?.Detail, cancellationToken).ConfigureAwait(false);
        if (!result.IsAllowed) { return result; }
        if (result.TenantAccessResult is not { FreshnessStatus: TenantProjectionFreshnessStatus.Fresh })
        {
            return result with { IsAllowed = false, Retryable = true, Reason = ReferenceState.Stale };
        }
        return result.ProjectDetail is { } detail && detail.TenantId == query.TenantId && detail.ProjectId == model.ProjectId
            && detail.Sequence == model.Sequence ? result : null;
    }

    private async Task<ReferenceState> FolderStateAsync(ProjectDetailItem detail, CancellationToken cancellationToken, string correlationId)
    {
        if (detail.ProjectFolder is null) { return ReferenceState.Pending; }
        if (detail.ProjectFolder is not { ReferenceState: ReferenceState.Included, FolderId: { } folderId })
        {
            return Enum.IsDefined(detail.ProjectFolder.ReferenceState) ? detail.ProjectFolder.ReferenceState : ReferenceState.Unavailable;
        }
        try
        {
            ProjectFolderValidationResult result = await folders.RefreshFolderReferenceAsync(new ProjectId(detail.ProjectId),
                folderId, correlationId, cancellationToken).ConfigureAwait(false);
            return result.Outcome switch
            {
                ProjectFolderValidationOutcome.Accepted => ReferenceState.Included,
                ProjectFolderValidationOutcome.Denied => ReferenceState.Unauthorized,
                ProjectFolderValidationOutcome.Archived => ReferenceState.Archived,
                ProjectFolderValidationOutcome.Stale => ReferenceState.Stale,
                _ => ReferenceState.Unavailable,
            };
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return ReferenceState.Unavailable; }
    }

    private Task<ReadModelEntry<ProjectReadModel>> EntryAsync(string tenant, string project, CancellationToken token)
        => store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(tenant, project), token);

    private async Task<ProjectReadModel?> ReadAsync(string tenant, string project, CancellationToken token)
        => (await EntryAsync(tenant, project, token).ConfigureAwait(false)).Value;

    private AdmissionSnapshot Snapshot(ProjectReadModel model, ReferenceState folderState)
    {
        bool folderCurrent = folderState == ReferenceState.Included;
        ProjectDetailItem detail = model.Detail!;
        bool header = ProjectPersistedDetailValidator.IsHeaderValid(detail);
        bool observation = ValidObservation(model);
        bool project = header && observation && model.Freshness == EvidenceFreshnessState.Current && model.GlobalPosition > 0;
        bool references = header && ProjectPersistedDetailValidator.IsValid(detail)
            && detail.FileReferences.Count == 0 && detail.MemoryReferences.Count == 0;
        EvidenceFreshnessState freshness = project ? EvidenceFreshnessState.Current : !observation || model.Freshness == EvidenceFreshnessState.Current ? EvidenceFreshnessState.Unavailable : model.Freshness;
        DateTimeOffset? verifiedAt = observation && model.LastVerifiedAt != default ? model.LastVerifiedAt : null;
        AdmissionResponseState state = !project || !folderCurrent ? AdmissionResponseState.Unavailable
            : references ? AdmissionResponseState.Complete : AdmissionResponseState.Partial;
        DateTimeOffset observedAt = clock.GetUtcNow();
        return new(state, observedAt, header ? model.Sequence : 0,
        [
            Component("Project", header && observation, freshness, project ? "Current" : "ProjectionUnavailable", verifiedAt),
            Component("Folder", folderCurrent, folderCurrent ? EvidenceFreshnessState.Current : folderState == ReferenceState.Stale ? EvidenceFreshnessState.Stale : EvidenceFreshnessState.Unavailable,
                folderCurrent ? "Current" : "FolderUnavailable", folderCurrent ? observedAt : null),
            Component("Setup", project, freshness, project ? "Current" : "SetupUnavailable", verifiedAt),
            Component("Authorization", true, EvidenceFreshnessState.Current, "Current", observedAt),
            Component("References", references, references ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable,
                references ? "Current" : "OptionalReferencesOmitted", references ? verifiedAt : null),
        ], state == AdmissionResponseState.Complete ? [AdmissionRecoveryAction.None] : [AdmissionRecoveryAction.Retry]);
    }

    private static IReadOnlyList<ProjectReadReference> References(ProjectDetailItem detail, bool descriptive, ReferenceState folderState)
    {
        bool folderCurrent = folderState == ReferenceState.Included;
        List<ProjectReadReference> result = [];
        if (detail.ProjectFolder is { } folder)
        {
            result.Add(new("ProjectFolder", folder.FolderId, null, null, descriptive && folderCurrent ? folder.DisplayName : null,
                folderState, SafeReason(folderState), folder.ObservedAt));
        }

        result.AddRange(detail.FileReferences.OrderBy(item => item.FileReferenceId, StringComparer.Ordinal).Select(item =>
            new ProjectReadReference("FileReference", item.FolderId, item.FileReferenceId, null, null,
                ReferenceState.Unavailable, "OwnerReadUnverified", item.ObservedAt)));
        result.AddRange(detail.MemoryReferences.OrderBy(item => item.MemoryReferenceId, StringComparer.Ordinal).Select(item =>
            new ProjectReadReference("MemoryReference", null, null, item.MemoryReferenceId, null,
                ReferenceState.Unavailable, "OwnerReadUnverified", item.ObservedAt)));
        return result;
    }

    private static string? SafeReason(ReferenceState state) => state == ReferenceState.Included ? null : "ReferenceUnavailable";

    private static bool TryPayload<T>(QueryEnvelope query, IReadOnlyList<string> fields, out T? input)
    {
        input = default;
        if (query.Payload.Length > 8192) { return false; }
        try
        {
            ReadOnlyMemory<byte> bytes = query.Payload.Length == 0 ? "{}"u8.ToArray() : query.Payload;
            using JsonDocument document = JsonDocument.Parse(bytes);
            if (document.RootElement.ValueKind != JsonValueKind.Object) { return false; }
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (!fields.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name)) { return false; }
            }

            input = document.RootElement.Deserialize<T>(JsonOptions);
            return input is not null;
        }
        catch (JsonException) { return false; }
    }

    private static string Scope(QueryEnvelope query, ListProjectsQuery input, ReadModelEntry<ProjectReadIndex> index, ProjectAuthorizationResult authorization, string visibilityFingerprint)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(QueryCursorScope.Create().Add("tenant", query.TenantId).Add("actor", query.OriginalActorId)
            .Add("workload", query.AuthenticatedWorkloadId).Add("delegation", query.DelegationId)
            .Add("delegated", query.IsDelegated.ToString()).Add("scopes", JsonSerializer.Serialize(query.Scopes))
            .Add("audience", JsonSerializer.Serialize(query.Audience)).Add("action", ProjectAuthorizationGate.ListProjectsAction)
            .Add("lifecycle", input.LifecycleState).Add("pageSize", input.PageSize.ToString(CultureInfo.InvariantCulture))
            .Add("inspect", input.InspectDescriptiveMetadata.ToString()).Add("policy", "project-list-v1-50-200-15m")
            .Add("authority", authorization.TenantAccessResult?.ProjectionWatermark)
            .Add("eventStoreAuthority", authorization.EventStoreValidationResult?.FreshnessWatermark)
            .Add("eventStoreFreshness", authorization.EventStoreValidationResult?.FreshnessClass)
            .Add("indexETag", index.ETag).Add("visibility", visibilityFingerprint)
            .AddProjectionWatermark(index.Value!.GlobalPosition).Build())));

    private static bool ValidObservation(ProjectReadModel model)
        => Enum.IsDefined(model.Freshness) && (model.Freshness != EvidenceFreshnessState.Current || model.LastVerifiedAt != default);

    internal static string OpenObservation(ProjectReadModel model, ProjectAuthorizationResult authorization, ProjectAuthorizationResult projectAuthorization, ReferenceState folderState)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Model = model, TenantAuthority = authorization.TenantAccessResult?.ProjectionWatermark,
            EventStoreAuthority = authorization.EventStoreValidationResult?.FreshnessWatermark,
            EventStoreFreshness = authorization.EventStoreValidationResult?.FreshnessClass, Folder = folderState,
            ProjectAuthority = projectAuthorization.EventStoreValidationResult?.FreshnessWatermark,
            ProjectAuthorityFreshness = projectAuthorization.EventStoreValidationResult?.FreshnessClass,
        }, JsonOptions)));

    private static bool StableScope(ProjectAuthorizationResult initial, ProjectAuthorizationResult? final)
        => final is not null && initial.TenantAccessResult?.ProjectionWatermark == final.TenantAccessResult?.ProjectionWatermark
            && initial.EventStoreValidationResult?.FreshnessWatermark == final.EventStoreValidationResult?.FreshnessWatermark;

    private static bool SameIndex(ReadModelEntry<ProjectReadIndex> left, ReadModelEntry<ProjectReadIndex> right)
        => right.Value is not null && left.ETag == right.ETag && JsonSerializer.Serialize(left.Value, JsonOptions) == JsonSerializer.Serialize(right.Value, JsonOptions);

    private static bool SameModel(ProjectReadModel left, ProjectReadModel? right)
        => right is not null && JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions);

    private static bool SafeIdentity(string tenant, string value)
    {
        try { _ = new AggregateIdentity(tenant, ProjectsServerModule.DomainName, value); return true; }
        catch (ArgumentException) { return false; }
    }

    private AdmissionSnapshot PageSnapshot(ProjectListRow[] rows, ProjectReadIndex index)
    {
        AdmissionResponseState state = rows.Any(row => row.Snapshot.ResponseState == AdmissionResponseState.Unavailable) ? AdmissionResponseState.Unavailable
            : rows.Any(row => row.Snapshot.ResponseState == AdmissionResponseState.Partial) ? AdmissionResponseState.Partial : AdmissionResponseState.Complete;
        AdmissionComponent[] unavailableComponents = rows.SelectMany(row => row.Snapshot.Components)
            .Where(component => !component.Included || component.Freshness != EvidenceFreshnessState.Current)
            .GroupBy(component => component.Name, StringComparer.Ordinal)
            .Select(group => group.FirstOrDefault(component => component.Freshness == EvidenceFreshnessState.Unavailable) ?? group.First()).ToArray();
        return new(state, clock.GetUtcNow(), 0,
            [Component("Enumeration", true, EvidenceFreshnessState.Current, "Current", index.LastVerifiedAt), .. unavailableComponents],
            state == AdmissionResponseState.Complete ? [AdmissionRecoveryAction.None] : [AdmissionRecoveryAction.Retry]);
    }

    private static AdmissionComponent Component(string name, bool included, EvidenceFreshnessState freshness, string reason, DateTimeOffset? verified)
        => new(name, included, freshness, reason) { LastVerifiedAt = verified };

    private AdmissionSnapshot Unavailable(DateTimeOffset lastVerifiedAt, string component = "Enumeration", EvidenceFreshnessState freshness = EvidenceFreshnessState.Unavailable)
        => new(AdmissionResponseState.Unavailable, clock.GetUtcNow(), 0,
            [Component(component, false, freshness, component == "Enumeration" ? "ProjectionUnavailable" : component == "Authorization" ? "AuthorizationUnavailable" : "FolderUnavailable", lastVerifiedAt == default ? null : lastVerifiedAt)], [AdmissionRecoveryAction.Retry]);

    private async Task<QueryResult> DenyAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        _ = await audit.RecordAsync(query, false, 0, cancellationToken).ConfigureAwait(false);
        return QueryResult.Failure("safe-denial");
    }

    private static QueryResult Success<T>(T value, string queryType) => QueryResult.FromPayload(JsonSerializer.SerializeToElement(value, JsonOptions), queryType);
    private static QueryResult Invalid() => QueryResult.Failure(QueryAdapterFailureReason.InvalidEnvelope);
    private static QueryResult InvalidCursor() => QueryResult.Failure(QueryAdapterFailureReason.InvalidCursor);
}
