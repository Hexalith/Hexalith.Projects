namespace Hexalith.Projects.Server.Queries;

using System.Collections.Concurrent;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;

/// <summary>Composes bounded advisory observations without persisting inference or granting authority.</summary>
/// <param name="store">The committed supported read models.</param>
/// <param name="gate">The fresh scope and exact Project authorization gate.</param>
/// <param name="binding">The verified original caller.</param>
/// <param name="folders">The actual Folder owner's authorization boundary.</param>
/// <param name="conversations">The narrow signed Conversation owner read.</param>
/// <param name="clock">The evaluation clock, unrelated to source currentness.</param>
/// <param name="logger">The optional metadata-only dependency failure logger.</param>
public sealed partial class ProjectAdvisoryQueryExecutor(IReadModelStore store, ProjectAuthorizationGate gate,
    ProjectQueryEnvelopePrincipalBinding binding, IProjectFolderDirectory folders,
    IProjectAdvisoryConversationDirectory conversations, TimeProvider clock, ILogger<ProjectAdvisoryQueryExecutor>? logger = null)
{
    /// <summary>The maximum distinct Project details examined per query.</summary>
    public const int MaximumCandidates = 200;
    /// <summary>The maximum raw attachment count, before deduplication.</summary>
    public const int MaximumAttachments = 32;
    /// <summary>The maximum number of simultaneous dependency calls.</summary>
    public const int MaximumConcurrency = 4;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Resolves advisory suggestions for a Conversation.</summary>
    public Task<QueryResult> FromConversationAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return ExecuteAsync(query, true, cancellationToken);
    }

    /// <summary>Resolves advisory suggestions for presented Folder references, identifying unsupported Files.</summary>
    public Task<QueryResult> FromAttachmentsAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return ExecuteAsync(query, false, cancellationToken);
    }

    private async Task<QueryResult> ExecuteAsync(QueryEnvelope query, bool conversation, CancellationToken callerToken)
    {
        callerToken.ThrowIfCancellationRequested();
        if (query.QueryType != (conversation ? ResolveProjectFromConversationQuery.QueryType : ResolveProjectFromAttachmentsQuery.QueryType)) return Denied();
        // One linked dependency budget begins with scope authorization, before validating caller input.
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        CancellationToken token = deadline.Token;
        try
        {
            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            LogFailure(query, exception);
            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(
                new ProjectAdvisoryResolutionResponse(null, [], clock.GetUtcNow(), true, []), Json), query.QueryType);
        }
        if (query.AggregateId != "projects" || query.Paging is not null || query.IsGlobalAdmin
            || query.EntityId is not null && query.EntityId != query.AggregateId) return Invalid();
        string? conversationId = null;
        bool includeArchived;
        ProjectAdvisoryAttachment[] inputs = [];
        if (conversation)
        {
            if (!Payload(query, ["conversationId", "includeArchived"], out ResolveProjectFromConversationQuery? input)
                || input is null || !Identity(query.TenantId, input.ConversationId)) return Invalid();
            conversationId = input.ConversationId; includeArchived = input.IncludeArchived;
        }
        else
        {
            if (!Payload(query, ["attachments", "includeArchived"], out ResolveProjectFromAttachmentsQuery? input)
                || input?.Attachments is null || input.Attachments.Count > MaximumAttachments
                || input.Attachments.Any(item => item is null || item.ReferenceKind is not ("folder" or "file")
                    || !Identity(query.TenantId, item.ReferenceId))) return Invalid();
            inputs = input.Attachments.Distinct().OrderBy(item => item.ReferenceKind, StringComparer.Ordinal)
                .ThenBy(item => item.ReferenceId, StringComparer.Ordinal).ToArray();
            includeArchived = input.IncludeArchived;
        }

        DateTimeOffset evaluatedAt = clock.GetUtcNow();
        bool unsupportedFiles = inputs.Any(item => item.ReferenceKind == "file");
        int incomplete = unsupportedFiles ? 1 : 0;
        void Omitted() => Interlocked.Exchange(ref incomplete, 1);
        QueryResult Response(IReadOnlyList<ProjectResolutionCandidateEvidence> evidence)
        {
            ProjectResolution resolved = new ProjectResolutionEngine().Resolve(
                new(query.TenantId, query.TenantId, includeArchived, evaluatedAt, query.CorrelationId, null), evidence);
            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(new ProjectAdvisoryResolutionResponse(
                incomplete != 0 && resolved.Candidates.Count == 0 ? null : resolved.Result,
                resolved.Candidates.Select(item => new ProjectAdvisoryCandidate(item.ProjectId, item.ReasonCodes, item.Rank, item.Score)).ToArray(),
                evaluatedAt, incomplete != 0, unsupportedFiles ? ["file"] : []), Json), query.QueryType);
        }

        try
        {
            ConversationAdvisoryMetadataResult? observed = null;
            if (conversationId is not null)
            {
                var read = await ConversationAsync(query, conversationId, token).WaitAsync(token).ConfigureAwait(false);
                callerToken.ThrowIfCancellationRequested();
                if (read.Denied) return Denied();
                observed = read.Metadata;
                if (observed?.Incomplete != false) Omitted();
                if (observed?.Metadata is null) { Omitted(); return Response([]); }
            }
            string[] acceptedInputs = await FilterInputsAsync(inputs.Where(item => item.ReferenceKind == "folder")
                .Select(item => item.ReferenceId).ToArray(), query, Omitted, token).WaitAsync(token).ConfigureAwait(false);
            ConcurrentDictionary<string, ProjectDetailItem> candidates = new(StringComparer.Ordinal);
            bool hasSignal = conversationId is not null
                ? observed?.Metadata is { } initial && (initial.ProjectId is not null || initial.SafeLabel is not null)
                : acceptedInputs.Length != 0;
            if (hasSignal)
            {
                var index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName,
                    ProjectReadModelKeys.Index(query.TenantId), token).WaitAsync(token).ConfigureAwait(false);
                IEnumerable<string> ids = [];
                if (index.Value is { ProjectIds: not null, UnavailableProjectIds: not null } view && view.TenantId == query.TenantId && view.GlobalPosition > 0 && view.LastVerifiedAt != default
                    && !string.IsNullOrEmpty(index.ETag))
                {
                    ids = view.ProjectIds;
                    if (view.UnavailableProjectIds.Count != 0) Omitted();
                }
                else Omitted();
                // The existing index is monolithic. This caps detail fanout, not index bytes.
                string? linked = observed?.Metadata?.ProjectId;
                if (linked is not null && !Identity(query.TenantId, linked)) { Omitted(); linked = null; }
                int capacity = MaximumCandidates - (linked is null ? 0 : 1);
                SortedSet<string> selected = new(StringComparer.Ordinal);
                foreach (string id in ids)
                {
                    token.ThrowIfCancellationRequested();
                    if (!Identity(query.TenantId, id)) { Omitted(); continue; }
                    if (id == linked || selected.Contains(id)) continue;
                    if (selected.Count < capacity) { selected.Add(id); continue; }
                    Omitted();
                    if (StringComparer.Ordinal.Compare(id, selected.Max) < 0)
                    {
                        selected.Remove(selected.Max!);
                        selected.Add(id);
                    }
                }
                token.ThrowIfCancellationRequested();
                if (linked is not null) selected.Add(linked);
                await Parallel.ForEachAsync(selected, new ParallelOptions { MaxDegreeOfParallelism = MaximumConcurrency, CancellationToken = token }, async (id, ct) =>
                {
                    try
                    {
                        if (!await ProjectAllowedAsync(query, id, Omitted, ct).ConfigureAwait(false)) return;
                        var detail = await DetailAsync(query, id, includeArchived, Omitted, ct).ConfigureAwait(false);
                        if (detail is null || !conversation && !acceptedInputs.Contains(detail.ProjectFolder?.FolderId, StringComparer.Ordinal)) return;
                        if (conversation && observed?.Metadata is { } metadata
                            && ConversationResolutionEvidenceMapper.Map(new(metadata.ConversationId, metadata.ProjectId,
                                metadata.SafeLabel, ReferenceState.Included), [new(id, detail.Name, detail.Lifecycle)], evaluatedAt).Count == 0) return;
                        if (detail.ProjectFolder?.FolderId is null
                            || await FolderAllowedAsync(query, id, detail.ProjectFolder.FolderId, Omitted, ct).ConfigureAwait(false))
                            candidates[id] = detail;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception exception) { LogFailure(query, exception); Omitted(); }
                }).WaitAsync(token).ConfigureAwait(false);

            }

            // Re-read owner permission; none of the initial permission decisions is cached for disclosure.
            if (conversationId is not null)
            {
                var final = await ConversationAsync(query, conversationId, token).WaitAsync(token).ConfigureAwait(false);
                callerToken.ThrowIfCancellationRequested();
                if (final.Denied) return Denied();
                if (final.Metadata?.Metadata is null) { Omitted(); return Response([]); }
                if (final.Metadata != observed || final.Metadata.Incomplete) Omitted();
                observed = final.Metadata;
            }
            acceptedInputs = await FilterInputsAsync(acceptedInputs, query, Omitted, token).WaitAsync(token).ConfigureAwait(false);
            await Parallel.ForEachAsync(candidates.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = MaximumConcurrency, CancellationToken = token }, async (pair, ct) =>
            {
                try
                {
                    // The committed Folder anchor may have changed after initial discovery. Authorize the
                    // newly observed owner, never borrow permission from the displaced Folder reference.
                    ProjectDetailItem? detail = null;
                    if (await ProjectAllowedAsync(query, pair.Key, Omitted, ct).ConfigureAwait(false))
                        detail = await DetailAsync(query, pair.Key, includeArchived, Omitted, ct).ConfigureAwait(false);
                    if (detail is null || !conversation && !acceptedInputs.Contains(detail.ProjectFolder?.FolderId, StringComparer.Ordinal)
                        || detail.ProjectFolder?.FolderId is { } folderId
                            && !await FolderAllowedAsync(query, pair.Key, folderId, Omitted, ct).ConfigureAwait(false))
                        candidates.TryRemove(pair.Key, out _);
                    else candidates[pair.Key] = detail;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception exception) { LogFailure(query, exception); candidates.TryRemove(pair.Key, out _); Omitted(); }
            }).WaitAsync(token).ConfigureAwait(false);
            if (!await ScopeAsync(query, token).WaitAsync(token).ConfigureAwait(false)) return Denied();
            callerToken.ThrowIfCancellationRequested();
            ProjectDetailItem[] visible = candidates.Values.OrderBy(item => item.ProjectId, StringComparer.Ordinal).ToArray();
            if (observed?.Metadata is { } metadata)
            {
                return Response(ConversationResolutionEvidenceMapper.Map(new(metadata.ConversationId, metadata.ProjectId,
                    metadata.SafeLabel, ReferenceState.Included), visible.Select(item => new ConversationResolutionProjectCandidate(
                        item.ProjectId, item.Name, item.Lifecycle)).ToArray(), evaluatedAt));
            }
            // The attachment mapper ignores input ReferenceState, so rejected input identities never reach it.
            return Response(AttachmentResolutionEvidenceMapper.Map(new(acceptedInputs.Select(id => new AttachmentResolutionReference(
                "folder", id, ReferenceState.Included)).ToArray(), []), visible.Select(item => new AttachmentResolutionProjectCandidate(
                    item.ProjectId, null, item.Lifecycle, [new("folder", item.ProjectFolder!.FolderId!, ReferenceState.Included)], [])).ToArray(), evaluatedAt));
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { LogFailure(query, exception); Omitted(); return Response([]); }
    }

    private async Task<ProjectDetailItem?> DetailAsync(QueryEnvelope query, string id, bool includeArchived, Action omitted, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var entry = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(query.TenantId, id), token).ConfigureAwait(false);
        if (entry.Value is not { Detail: { } detail } model || string.IsNullOrEmpty(entry.ETag)
            || model.TenantId != query.TenantId || model.ProjectId != id || detail.TenantId != query.TenantId || detail.ProjectId != id
            || !ProjectPersistedDetailValidator.IsAdvisoryHeaderValid(detail) || model.Sequence <= 0 || detail.Sequence != model.Sequence
            || !Enum.IsDefined(model.Freshness) || model.Freshness is EvidenceFreshnessState.Unavailable or EvidenceFreshnessState.Rebuilding)
        { omitted(); return null; }
        return detail.Lifecycle == ProjectLifecycle.Archived && !includeArchived ? null : detail;
    }

    private async Task<bool> ScopeAsync(QueryEnvelope query, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var result = await ProjectReadQueryAdmission.AuthorizeAsync(query, query.QueryType, false, gate, binding, token).ConfigureAwait(false);
        if (result?.Retryable == true) throw new InvalidOperationException("Scope authorization unavailable.");
        return result?.IsAllowed == true;
    }

    private void LogFailure(QueryEnvelope query, Exception exception)
    {
        if (logger is not null) LogDependencyFailure(logger, exception.GetType().Name, query.CorrelationId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Advisory dependency failed with {ExceptionType}; correlation {CorrelationId}.")]
    private static partial void LogDependencyFailure(ILogger logger, string exceptionType, string correlationId);

    private async Task<bool> ProjectAllowedAsync(QueryEnvelope query, string id, Action omitted, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!binding.TryBind(query, out var context) || binding.HttpContext is not { } http) return false;
        var result = await gate.AuthorizeSupportedResourceAsync(id, context, http, query.CorrelationId, token).ConfigureAwait(false);
        if (result.Retryable) omitted();
        return result.IsAllowed;
    }

    private async Task<bool> FolderAllowedAsync(QueryEnvelope query, string projectId, string? folderId, Action omitted, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (folderId is null || !Identity(query.TenantId, folderId)) { omitted(); return false; }
        try
        {
            var result = await folders.RefreshFolderReferenceAsync(new ProjectId(projectId), folderId, query.CorrelationId, token).ConfigureAwait(false);
            if (result.Outcome == ProjectFolderValidationOutcome.Accepted) return true;
            if (result.Outcome != ProjectFolderValidationOutcome.Denied) omitted();
            return false;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception) { LogFailure(query, exception); omitted(); return false; }
    }

    private async Task<string[]> FilterInputsAsync(string[] ids, QueryEnvelope query, Action omitted, CancellationToken token)
    {
        ConcurrentBag<string> accepted = [];
        await Parallel.ForEachAsync(ids, new ParallelOptions { MaxDegreeOfParallelism = MaximumConcurrency, CancellationToken = token }, async (id, ct) =>
        {
            if (await FolderAllowedAsync(query, query.AggregateId, id, omitted, ct).ConfigureAwait(false)) accepted.Add(id);
        }).ConfigureAwait(false);
        return accepted.Order(StringComparer.Ordinal).ToArray();
    }

    private async Task<(bool Denied, ConversationAdvisoryMetadataResult? Metadata)> ConversationAsync(QueryEnvelope query, string id, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        QueryResult result = await conversations.ReadAsync(query.TenantId, id, token).ConfigureAwait(false);
        if (!result.Success) return (result.ErrorMessage == "safe-denial", null);
        var payload = result.GetPayload();
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("incomplete", out var incomplete)
            || incomplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !payload.TryGetProperty("metadata", out _)) return (false, null);
        var metadata = payload.Deserialize<ConversationAdvisoryMetadataResult>(Json);
        if (metadata?.Metadata is { } value && (value.ConversationId != id
            || value.LifecycleState is not ("Initializing" or "Open" or "Closed" or "Archived")
            || value.SafeLabel?.Length > ConversationResolutionScoringLabel.MaximumLength || value.ProjectId is not null && !Identity(query.TenantId, value.ProjectId))) return (false, null);
        return (false, metadata);
    }

    private static bool Identity(string tenant, string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) return false;
        try { _ = new AggregateIdentity(tenant, ProjectsServerModule.DomainName, id); return true; }
        catch (ArgumentException) { return false; }
    }

    private static bool Payload<T>(QueryEnvelope query, string[] fields, out T? input)
    {
        input = default;
        if (query.Payload is not { Length: > 0 and <= 16384 }) return false;
        try
        {
            using JsonDocument document = JsonDocument.Parse(query.Payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty field in document.RootElement.EnumerateObject())
                if (!fields.Contains(field.Name, StringComparer.Ordinal) || !names.Add(field.Name)) return false;
            input = document.RootElement.Deserialize<T>(Json);
            return input is not null;
        }
        catch (JsonException) { return false; }
    }

    private static QueryResult Denied() => QueryResult.Failure("safe-denial");
    private static QueryResult Invalid() => QueryResult.Failure(QueryAdapterFailureReason.InvalidEnvelope);
}
