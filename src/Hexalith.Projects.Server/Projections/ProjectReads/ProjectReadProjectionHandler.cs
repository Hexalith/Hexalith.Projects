namespace Hexalith.Projects.Server.Projections.ProjectReads;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>Maintains supported detail and enumeration atomically using the platform batch store.</summary>
public sealed class ProjectReadProjectionHandler(IReadModelStore store, IReadModelBatchStore batches) : IAsyncDomainProjectionRebuildHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // This finite catalog is intentional: newly added event types require an explicit compatibility decision.
    private static readonly IReadOnlyDictionary<string, Type> Relevant = new[]
    {
        typeof(ProjectCreated), typeof(ProjectSetupUpdated), typeof(ProjectFolderSet),
        typeof(ProjectFolderCreationPending), typeof(FileReferenceLinked), typeof(FileReferenceUnlinked),
        typeof(MemoryLinked), typeof(MemoryUnlinked), typeof(ProjectArchived), typeof(ProjectRestored),
        typeof(ProjectResolutionConfirmed),
    }.ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, Type> Irrelevant = new[]
    {
        typeof(ProjectChatBotAuthorityGranted), typeof(ProjectChatBotAuthorityRevoked),
        typeof(ProjectChatBotAuthorityRejected), typeof(ProjectCreationRejected),
        typeof(ProjectArchiveRejected), typeof(ProjectRestoreRejected), typeof(ProjectSetupUpdateRejected),
        typeof(ProjectReferenceLinkRejected), typeof(ProjectReferenceUnlinkRejected),
        typeof(ProjectResolutionConfirmationRejected),
    }.ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string ProjectionType => "project-reads";

    /// <inheritdoc/>
    public DomainProjectionRebuildSemantics RebuildSemantics => DomainProjectionRebuildSemantics.FullReplay;

    /// <inheritdoc/>
    public async Task<DomainProjectionRebuildPlan> PrepareRebuildAsync(ProjectionRequest request, string operationId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        if (request.Domain != Domain || request.Events is not { Length: > 0 } || request.Events[0].SequenceNumber != 1)
        {
            throw new DomainProjectionRebuildRejectedException("invalid-replay");
        }

        ProjectReadModel rebuilt = Fold(request, null);
        if (rebuilt.Freshness != EvidenceFreshnessState.Current
            || request.RebuildSourceHeadSequence is { } head && head != rebuilt.Sequence)
        {
            throw new DomainProjectionRebuildRejectedException("projection-unavailable");
        }

        string detailKey = ProjectReadModelKeys.Detail(request.TenantId, request.AggregateId);
        string indexKey = ProjectReadModelKeys.Index(request.TenantId);
        ReadModelEntry<ProjectReadModel> detail = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, cancellationToken).ConfigureAwait(false);
        ReadModelEntry<ProjectReadIndex> index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, cancellationToken).ConfigureAwait(false);
        if (detail.Value is { } prior && (prior.TenantId != request.TenantId || prior.ProjectId != request.AggregateId || prior.Sequence > rebuilt.Sequence || RecoveryFloor(prior) > rebuilt.Sequence)
            || index.Value is { } previousIndex && previousIndex.TenantId != request.TenantId)
        {
            throw new DomainProjectionRebuildRejectedException("stale-replay");
        }

        bool advances = rebuilt.GlobalPosition > (index.Value?.GlobalPosition ?? 0);
        ProjectReadIndex merged = new(request.TenantId,
            (index.Value?.ProjectIds ?? []).Where(id => id != request.AggregateId)
                .Concat(rebuilt.Detail is null ? [] : new[] { request.AggregateId }).Order(StringComparer.Ordinal).ToArray(),
            Math.Max(rebuilt.GlobalPosition, index.Value?.GlobalPosition ?? 0),
            advances ? rebuilt.LastVerifiedAt : index.Value!.LastVerifiedAt)
        {
            UnavailableProjectIds = (index.Value?.UnavailableProjectIds ?? []).Where(id => id != request.AggregateId).ToArray(),
        };
        return new(ProjectReadModelKeys.StoreName,
        [
            ReadModelBatchOperation.Write(detailKey, rebuilt, Concurrency(detail.ETag)),
            ReadModelBatchOperation.Write(indexKey, merged, Concurrency(index.ETag)),
        ]);
    }

    /// <inheritdoc/>
    public async Task<DomainProjectionHandlerResult> ProjectAsync(ProjectionRequest request, string dispatchId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);
        if (request.Domain != Domain || string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.AggregateId) || request.Events is null)
        {
            return DomainProjectionHandlerResult.Failed("invalid-request");
        }

        string detailKey = ProjectReadModelKeys.Detail(request.TenantId, request.AggregateId);
        string indexKey = ProjectReadModelKeys.Index(request.TenantId);
        bool recovered = false;
        for (int attempt = 0; attempt < ReadModelWritePolicy.DefaultMaxAttempts; attempt++)
        {
            if (batches is IReadModelPendingBatchRecoveryStore recovery)
            {
                ReadModelBatchScope recoveryScope = new(ProjectReadModelKeys.StoreName, request.TenantId, Domain,
                    request.AggregateId, ProjectionType, dispatchId);
                foreach (string key in new[] { detailKey, indexKey })
                {
                    ReadModelPendingBatchRecoveryResult pending = await recovery.RecoverPendingAsync(recoveryScope, key, cancellationToken).ConfigureAwait(false);
                    if (!pending.IsResolved) { return DomainProjectionHandlerResult.Retryable("projection-unavailable"); }
                    recovered |= pending.WasRecovered;
                }
            }
            ReadModelEntry<ProjectReadModel> detailEntry = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, cancellationToken).ConfigureAwait(false);
            ReadModelEntry<ProjectReadIndex> indexEntry = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, cancellationToken).ConfigureAwait(false);
            ProjectReadModel next = Fold(request, detailEntry.Value);
            if (next == detailEntry.Value)
            {
                return next.Freshness == EvidenceFreshnessState.Current
                    ? recovered ? DomainProjectionHandlerResult.Completed() : DomainProjectionHandlerResult.AlreadyCompleted()
                    : DomainProjectionHandlerResult.Failed("projection-unavailable");
            }

            ProjectReadIndex? currentIndex = indexEntry.Value;
            if (currentIndex is not null && currentIndex.TenantId != request.TenantId)
            {
                return DomainProjectionHandlerResult.Failed("invalid-index");
            }

            string[] ids = (currentIndex?.ProjectIds ?? [])
                .Concat(next.Detail is null ? [] : new[] { next.ProjectId })
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            bool advancesIndex = next.GlobalPosition > (currentIndex?.GlobalPosition ?? 0);
            ProjectReadIndex index = new(request.TenantId, ids,
                Math.Max(next.GlobalPosition, currentIndex?.GlobalPosition ?? 0),
                advancesIndex ? next.LastVerifiedAt : currentIndex?.LastVerifiedAt ?? next.LastVerifiedAt)
            {
                UnavailableProjectIds = (currentIndex?.UnavailableProjectIds ?? [])
                    .Where(id => id != request.AggregateId)
                    .Concat(next.Freshness == EvidenceFreshnessState.Current ? [] : new[] { request.AggregateId })
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            };

            // An ETag-bound identity permits a fresh manifest after a compensated optimistic conflict.
            // The SDK recovers original pending manifests above before these fresh ETags are loaded.
            string batchId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new[] { dispatchId, detailEntry.ETag, indexEntry.ETag }, JsonOptions))));
            ReadModelBatch batch = new(new ReadModelBatchScope(ProjectReadModelKeys.StoreName,
                request.TenantId, Domain, request.AggregateId, ProjectionType, batchId),
            [
                ReadModelBatchOperation.Write(detailKey, next, Concurrency(detailEntry.ETag)),
                ReadModelBatchOperation.Write(indexKey, index, Concurrency(indexEntry.ETag)),
            ]) { RetainManifestForRecovery = true };
            ReadModelBatchResult result = await batches.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return next.Freshness == EvidenceFreshnessState.Current
                    ? DomainProjectionHandlerResult.Completed()
                    : DomainProjectionHandlerResult.Failed("projection-unavailable");
            }

            if (result.ConflictKind != ReadModelBatchConflictKind.Optimistic)
            {
                return DomainProjectionHandlerResult.Retryable("projection-unavailable");
            }
        }

        return DomainProjectionHandlerResult.Retryable("projection-concurrency");
    }

    private static ReadModelBatchConcurrency Concurrency(string? etag)
        => string.IsNullOrEmpty(etag) ? ReadModelBatchConcurrency.CreateOnly : ReadModelBatchConcurrency.Match(etag);

    private static ProjectReadModel Fold(ProjectionRequest request, ProjectReadModel? previous)
    {
        ProjectReadModel initial = previous ?? new(request.TenantId, request.AggregateId, null, 0, 0, default, EvidenceFreshnessState.Current);
        long floor = RecoveryFloor(initial);
        long observed = Math.Max(floor, request.Events.Select(static item => item.SequenceNumber).DefaultIfEmpty(initial.Sequence).Max());
        ProjectReadModel failed = initial with { Freshness = EvidenceFreshnessState.Unavailable, RecoveryFloor = observed };
        if (initial.TenantId != request.TenantId || initial.ProjectId != request.AggregateId)
        {
            return failed;
        }

        bool replay = request.Events.Length > 0 && request.Events[0].SequenceNumber == 1
            && request.Events[^1].SequenceNumber >= Math.Max(initial.Sequence, floor)
            && initial.Freshness != EvidenceFreshnessState.Current;
        ProjectReadModel current = replay
            ? new(request.TenantId, request.AggregateId, null, 0, 0, default, EvidenceFreshnessState.Current)
            : initial with { Freshness = EvidenceFreshnessState.Current };
        foreach (ProjectionEventDto item in request.Events)
        {
            if (item.SequenceNumber <= current.Sequence)
            {
                continue;
            }

            if (current.Freshness != EvidenceFreshnessState.Current || item.SequenceNumber != current.Sequence + 1
                || item.GlobalPosition <= current.GlobalPosition || item.Timestamp == default
                || item.SerializationFormat != "json" || item.Payload is null || string.IsNullOrWhiteSpace(item.EventTypeName)
                || (!Relevant.TryGetValue(item.EventTypeName, out Type? type) && !Irrelevant.TryGetValue(item.EventTypeName, out type)))
            {
                return failed;
            }

            object? value;
            try
            {
                value = JsonSerializer.Deserialize(item.Payload, type, JsonOptions);
            }
            catch (JsonException)
            {
                return failed;
            }

            if (value is null || value is IProjectEvent projectEvent
                && (projectEvent.TenantId != request.TenantId || projectEvent.ProjectId != request.AggregateId))
            {
                return failed;
            }

            ProjectDetailItem? detail = current.Detail;
            if (Relevant.ContainsKey(item.EventTypeName))
            {
                if (value is not ProjectCreated && detail is null || value is ProjectCreated && detail is not null)
                {
                    return failed;
                }

                try
                {
                    detail = (detail is null ? ProjectDetailProjection.Empty : ProjectDetailProjection.Seed(detail))
                        .Apply([new ProjectProjectionEnvelope(request.TenantId, item.SequenceNumber, (IProjectEvent)value)])
                        .Get(request.TenantId, request.AggregateId);
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NullReferenceException)
                {
                    return failed;
                }
                if (detail is null)
                {
                    return failed;
                }
            }

            current = new(request.TenantId, request.AggregateId,
                detail is null ? null : detail with { Sequence = item.SequenceNumber },
                item.SequenceNumber, item.GlobalPosition, item.Timestamp, EvidenceFreshnessState.Current);
        }

        return current.Sequence < floor ? failed : current;
    }

    private static long RecoveryFloor(ProjectReadModel model) => model.RecoveryFloor
        ?? (model.Freshness == EvidenceFreshnessState.Current ? 0 : model.Sequence == long.MaxValue ? long.MaxValue : model.Sequence + 1);
}
