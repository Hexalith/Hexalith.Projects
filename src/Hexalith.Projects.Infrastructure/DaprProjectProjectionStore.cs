// <copyright file="DaprProjectProjectionStore.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;

/// <summary>
/// Dapr state-backed project projection journal. Reads rebuild through the shared pure folds.
/// </summary>
public sealed class DaprProjectProjectionStore(
    IProjectsStateStore stateStore,
    ProjectsStateStoreOptions? options = null) : IProjectProjectionStore
{
    private const string DomainName = "projects";
    private const string KeyPrefix = "projects:projection-journal:";

    private static readonly IReadOnlyDictionary<string, Type> ProjectEventTypes = typeof(IProjectEvent).Assembly
        .GetTypes()
        .Where(static type => !type.IsAbstract && !type.IsInterface && typeof(IProjectEvent).IsAssignableFrom(type))
        .ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

    private readonly ProjectsStateStoreOptions _options = options ?? ProjectsStateStoreOptions.Default;
    private readonly IProjectsStateStore _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));

    /// <summary>Gets the JSON options used for event payloads and durable documents.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    /// <inheritdoc/>
    public async Task<ProjectProjectionAppendResult> AppendAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        EventMetadata metadata = envelope.Metadata;
        if (!string.Equals(metadata.Domain, DomainName, StringComparison.Ordinal))
        {
            return Result(ProjectProjectionAppendStatus.SkippedForeignDomain, metadata, ProjectionPosition(metadata));
        }

        if (!ProjectEventTypes.TryGetValue(metadata.EventTypeName, out Type? eventType))
        {
            return Result(ProjectProjectionAppendStatus.SkippedUnknownEventType, metadata, ProjectionPosition(metadata));
        }

        if (!TryDeserializeProjectEvent(envelope.Payload, eventType, out IProjectEvent? projectEvent))
        {
            await MarkMalformedAsync(metadata, cancellationToken).ConfigureAwait(false);
            return Result(ProjectProjectionAppendStatus.InvalidPayload, metadata, ProjectionPosition(metadata));
        }

        if (projectEvent is null || !IsConsistentEnvelope(metadata, projectEvent))
        {
            await MarkMalformedAsync(metadata, cancellationToken).ConfigureAwait(false);
            return Result(ProjectProjectionAppendStatus.InvalidPayload, metadata, ProjectionPosition(metadata));
        }

        string key = Key(metadata.TenantId);
        ProjectsStateEntry<ProjectProjectionJournalDocument> entry = await _stateStore
            .GetAsync<ProjectProjectionJournalDocument>(_options.StateStoreName, key, cancellationToken)
            .ConfigureAwait(false);
        ProjectProjectionJournalDocument document = entry.Value ?? new ProjectProjectionJournalDocument(metadata.TenantId);

        string fingerprint = Fingerprint(metadata, envelope.Payload);
        if (document.ProcessedMessages.TryGetValue(metadata.MessageId, out string? existingFingerprint))
        {
            if (string.Equals(existingFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return Result(ProjectProjectionAppendStatus.Duplicate, metadata, ProjectionPosition(metadata));
            }

            document = document with { ReplayConflict = true };
            await SaveAsync(key, document, entry.ETag, cancellationToken).ConfigureAwait(false);
            return Result(ProjectProjectionAppendStatus.ReplayConflict, metadata, ProjectionPosition(metadata));
        }

        long projectionPosition = ProjectionPosition(metadata);
        if (document.Events.Count > 0 && projectionPosition <= document.Watermark)
        {
            return Result(ProjectProjectionAppendStatus.OutOfOrder, metadata, projectionPosition);
        }

        ProjectProjectionJournalDocument updated = document.Append(
            new PersistedProjectProjectionEnvelope(
                metadata.TenantId,
                projectionPosition,
                metadata.EventTypeName,
                Convert.ToBase64String(envelope.Payload)),
            metadata.MessageId,
            fingerprint);

        await SaveAsync(key, updated, entry.ETag, cancellationToken).ConfigureAwait(false);
        return Result(ProjectProjectionAppendStatus.Applied, metadata, projectionPosition);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectListItem>> ListAsync(
        string tenantId,
        ProjectLifecycle? lifecycleFilter,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        ProjectProjectionJournalDocument document = EnsureReadable(
            await ReadJournalAsync(tenantId, cancellationToken).ConfigureAwait(false));

        ProjectListProjection projection = ProjectListProjection.Rebuild(ToEnvelopes(document));
        return projection.List(tenantId, lifecycleFilter);
    }

    /// <inheritdoc/>
    public async Task<ProjectDetailItem?> GetDetailAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        ProjectProjectionJournalDocument document = EnsureReadable(
            await ReadJournalAsync(tenantId, cancellationToken).ConfigureAwait(false));

        ProjectDetailProjection projection = ProjectDetailProjection.Rebuild(ToEnvelopes(document));
        return projection.Get(tenantId, projectId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectReferenceIndexItem>> ListReferencesByReferenceAsync(
        string tenantId,
        IReadOnlyCollection<string> folderIds,
        IReadOnlyCollection<string> fileReferenceIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(folderIds);
        ArgumentNullException.ThrowIfNull(fileReferenceIds);

        ProjectProjectionJournalDocument document = EnsureReadable(
            await ReadJournalAsync(tenantId, cancellationToken).ConfigureAwait(false));

        ProjectReferenceIndexProjection projection = ProjectReferenceIndexProjection.Empty.Apply(ToEnvelopes(document));
        return projection.ListByReference(tenantId, folderIds, fileReferenceIds);
    }

    /// <inheritdoc/>
    public async Task<ProjectProjectionReadiness> GetReadinessAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        ProjectProjectionJournalDocument? document = await ReadJournalAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return document is null
            ? new ProjectProjectionReadiness(tenantId, 0L, ReplayConflict: false, MalformedEvidence: false)
            : new ProjectProjectionReadiness(tenantId, document.Watermark, document.ReplayConflict, document.MalformedEvidence);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static bool TryDeserializeProjectEvent(
        byte[] payload,
        Type eventType,
        out IProjectEvent? projectEvent)
    {
        try
        {
            projectEvent = JsonSerializer.Deserialize(payload, eventType, JsonOptions) as IProjectEvent;
            return projectEvent is not null;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            projectEvent = null;
            return false;
        }
    }

    private static bool IsConsistentEnvelope(EventMetadata metadata, IProjectEvent projectEvent)
        => string.Equals(metadata.TenantId, projectEvent.TenantId, StringComparison.Ordinal)
            && string.Equals(metadata.AggregateId, projectEvent.ProjectId, StringComparison.Ordinal);

    private static ProjectProjectionJournalDocument EnsureReadable(ProjectProjectionJournalDocument? document)
    {
        if (document is null)
        {
            throw new InvalidOperationException("Project projection journal is unavailable.");
        }

        if (document.ReplayConflict || document.MalformedEvidence)
        {
            throw new InvalidOperationException("Project projection journal failed closed due to malformed or conflicting evidence.");
        }

        return document;
    }

    private async Task<ProjectProjectionJournalDocument?> ReadJournalAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        ProjectsStateEntry<ProjectProjectionJournalDocument> entry = await _stateStore
            .GetAsync<ProjectProjectionJournalDocument>(_options.StateStoreName, Key(tenantId), cancellationToken)
            .ConfigureAwait(false);
        return entry.Value;
    }

    private async Task MarkMalformedAsync(EventMetadata metadata, CancellationToken cancellationToken)
    {
        string key = Key(metadata.TenantId);
        ProjectsStateEntry<ProjectProjectionJournalDocument> entry = await _stateStore
            .GetAsync<ProjectProjectionJournalDocument>(_options.StateStoreName, key, cancellationToken)
            .ConfigureAwait(false);
        ProjectProjectionJournalDocument document = (entry.Value ?? new ProjectProjectionJournalDocument(metadata.TenantId)) with
        {
            MalformedEvidence = true,
        };
        await SaveAsync(key, document, entry.ETag, cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveAsync(
        string key,
        ProjectProjectionJournalDocument document,
        string? eTag,
        CancellationToken cancellationToken)
    {
        bool saved = await _stateStore
            .TrySaveAsync(_options.StateStoreName, key, document, eTag, cancellationToken)
            .ConfigureAwait(false);
        if (!saved)
        {
            throw new InvalidOperationException("Project projection journal optimistic concurrency conflict.");
        }
    }

    private static IEnumerable<ProjectProjectionEnvelope> ToEnvelopes(ProjectProjectionJournalDocument document)
    {
        foreach (PersistedProjectProjectionEnvelope persisted in document.Events)
        {
            if (!ProjectEventTypes.TryGetValue(persisted.EventTypeName, out Type? eventType))
            {
                throw new InvalidOperationException("Project projection journal contains an unsupported event type.");
            }

            byte[] payload = Convert.FromBase64String(persisted.PayloadBase64);
            if (!TryDeserializeProjectEvent(payload, eventType, out IProjectEvent? projectEvent) || projectEvent is null)
            {
                throw new InvalidOperationException("Project projection journal contains malformed event payload evidence.");
            }

            yield return new ProjectProjectionEnvelope(persisted.TenantId, persisted.Sequence, projectEvent);
        }
    }

    private static string Fingerprint(EventMetadata metadata, byte[] payload)
    {
        byte[] hash = SHA256.HashData(payload);
        return string.Concat(
            metadata.EventTypeName,
            ":",
            metadata.TenantId,
            ":",
            metadata.AggregateId,
            ":",
            metadata.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ":",
            metadata.GlobalPosition.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ":",
            Convert.ToHexString(hash));
    }

    private static long ProjectionPosition(EventMetadata metadata)
        => metadata.GlobalPosition;

    private static string Key(string tenantId)
        => KeyPrefix + tenantId.Trim();

    private static ProjectProjectionAppendResult Result(ProjectProjectionAppendStatus status, EventMetadata metadata, long projectionPosition)
        => new(status, metadata.TenantId, metadata.MessageId, projectionPosition);

    private sealed record PersistedProjectProjectionEnvelope(
        string TenantId,
        long Sequence,
        string EventTypeName,
        string PayloadBase64);

    private sealed record ProjectProjectionJournalDocument
    {
        public ProjectProjectionJournalDocument(string tenantId)
        {
            TenantId = tenantId;
        }

        public string TenantId { get; init; }

        public long Watermark { get; init; }

        public bool ReplayConflict { get; init; }

        public bool MalformedEvidence { get; init; }

        public IReadOnlyList<PersistedProjectProjectionEnvelope> Events { get; init; } = [];

        public IReadOnlyDictionary<string, string> ProcessedMessages { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public ProjectProjectionJournalDocument Append(
            PersistedProjectProjectionEnvelope envelope,
            string messageId,
            string fingerprint)
        {
            List<PersistedProjectProjectionEnvelope> events = [.. Events, envelope];
            Dictionary<string, string> processed = new(ProcessedMessages, StringComparer.Ordinal)
            {
                [messageId] = fingerprint,
            };

            return this with
            {
                Watermark = envelope.Sequence,
                Events = events,
                ProcessedMessages = processed,
            };
        }
    }
}
