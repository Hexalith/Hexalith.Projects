namespace Hexalith.Projects.Server.Projections.ProjectReads;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Projections.ProjectReads;

/// <summary>Maintains supported detail and enumeration atomically using the platform batch store.</summary>
public sealed class ProjectReadProjectionHandler(IReadModelStore store, IReadModelBatchStore batches) : IAsyncDomainProjectionRebuildHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Gets the finite catalog of events that change supported Project reads.</summary>
    internal static IReadOnlyCollection<Type> RelevantEventTypes => ProjectReadModelFolder.RelevantEventTypes;

    /// <summary>Gets the explicitly classified harmless events that advance evidence only.</summary>
    internal static IReadOnlyCollection<Type> NoOpEventTypes => ProjectReadModelFolder.NoOpEventTypes;

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

        ProjectReadModel rebuilt = ProjectReadModelFolder.Fold(request, null);
        if (rebuilt.Freshness != EvidenceFreshnessState.Current
            || request.RebuildSourceHeadSequence is { } head && head != rebuilt.Sequence)
        {
            throw new DomainProjectionRebuildRejectedException("projection-unavailable");
        }

        string detailKey = ProjectReadModelKeys.Detail(request.TenantId, request.AggregateId);
        string indexKey = ProjectReadModelKeys.Index(request.TenantId);
        ReadModelEntry<ProjectReadModel> detail = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, cancellationToken).ConfigureAwait(false);
        ReadModelEntry<ProjectReadIndex> index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, indexKey, cancellationToken).ConfigureAwait(false);
        if (detail.Value is { } prior && (prior.TenantId != request.TenantId || prior.ProjectId != request.AggregateId || prior.Sequence > rebuilt.Sequence || ProjectReadModelFolder.RecoveryFloor(prior) > rebuilt.Sequence)
            || index.Value is { } previousIndex && previousIndex.TenantId != request.TenantId)
        {
            throw new DomainProjectionRebuildRejectedException("stale-replay");
        }

        bool advances = rebuilt.GlobalPosition > (index.Value?.GlobalPosition ?? 0);
        ProjectReadIndex merged = new(request.TenantId,
            (index.Value?.ProjectIds ?? []).Where(id => id != request.AggregateId)
                .Concat(rebuilt.Detail is null || rebuilt.IsCreationAbandoned ? [] : new[] { request.AggregateId }).Order(StringComparer.Ordinal).ToArray(),
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
            ProjectReadModel next = ProjectReadModelFolder.Fold(request, detailEntry.Value);
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
                .Where(id => (next.Detail is not null && !next.IsCreationAbandoned) || id != next.ProjectId)
                .Concat(next.Detail is null || next.IsCreationAbandoned ? [] : new[] { next.ProjectId })
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
}
