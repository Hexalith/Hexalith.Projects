// <copyright file="ProjectReadModelFolder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectReads;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Pure deterministic folder for supported Project read models (AD-24, Story 7.1).
/// Classifies every contract event explicitly, enforces the <see cref="ProjectCreationAbandoned"/>
/// terminal fence, validates all additive bound-Folder fields, and fails closed on corrupt history.
/// </summary>
public static class ProjectReadModelFolder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // This finite catalog is intentional: newly added event types require an explicit compatibility decision.
    private static readonly IReadOnlyDictionary<string, Type> Relevant = new[]
    {
        typeof(ProjectCreated), typeof(ProjectCreationAbandoned), typeof(ProjectSetupUpdated), typeof(ProjectFolderSet),
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

    /// <summary>Gets the finite catalog of events that change supported Project reads.</summary>
    public static IReadOnlyCollection<Type> RelevantEventTypes => Relevant.Values.ToArray();

    /// <summary>Gets the explicitly classified harmless events that advance evidence only.</summary>
    public static IReadOnlyCollection<Type> NoOpEventTypes => Irrelevant.Values.ToArray();

    /// <summary>Gets the recovery floor sequence that full replay must cover for a degraded read model.</summary>
    /// <param name="model">The current read model.</param>
    /// <returns>The minimum sequence that recovery must cover.</returns>
    public static long RecoveryFloor(ProjectReadModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.RecoveryFloor
            ?? (model.Freshness == EvidenceFreshnessState.Current ? 0 : model.Sequence == long.MaxValue ? long.MaxValue : model.Sequence + 1);
    }

    /// <summary>Folds a projection request onto an existing persisted read model.</summary>
    /// <param name="request">The incoming projection request.</param>
    /// <param name="previous">The previously persisted read model, or null.</param>
    /// <returns>The resulting read model.</returns>
    public static ProjectReadModel Fold(ProjectionRequest request, ProjectReadModel? previous)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Events);

        ProjectReadModel initial = previous ?? new(request.TenantId, request.AggregateId, null, 0, 0, default, EvidenceFreshnessState.Current);
        long floor = RecoveryFloor(initial);
        long observed = Math.Max(floor, request.Events.Select(static item => item.SequenceNumber).DefaultIfEmpty(initial.Sequence).Max());
        ProjectReadModel failed = initial with { Freshness = EvidenceFreshnessState.Unavailable, RecoveryFloor = observed };
        if (initial.TenantId != request.TenantId
            || initial.ProjectId != request.AggregateId
            || (initial.IsCreationAbandoned && initial.Detail is not null))
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
            if (item.SequenceNumber <= 0 || item.GlobalPosition <= 0 || item.Timestamp == default
                || item.SerializationFormat != "json" || item.Payload is null || string.IsNullOrWhiteSpace(item.EventTypeName)
                || (!Relevant.TryGetValue(item.EventTypeName, out Type? type) && !Irrelevant.TryGetValue(item.EventTypeName, out type)))
            {
                return failed;
            }

            object? value;
            try
            {
                using JsonDocument content = JsonDocument.Parse(item.Payload);
                if (!HasRequiredContent(content.RootElement, type))
                {
                    return failed;
                }

                value = content.RootElement.Deserialize(type, JsonOptions);
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

            if (!ValidKnownContent(value, request.TenantId, request.AggregateId, item.SequenceNumber))
            {
                return failed;
            }

            if (item.SequenceNumber <= current.Sequence)
            {
                continue;
            }

            if (current.Freshness != EvidenceFreshnessState.Current || item.SequenceNumber != current.Sequence + 1
                || item.GlobalPosition <= current.GlobalPosition)
            {
                return failed;
            }

            ProjectDetailItem? detail = current.Detail;
            bool isCreationAbandoned = current.IsCreationAbandoned;
            if (Relevant.ContainsKey(item.EventTypeName))
            {
                if (value is ProjectCreationAbandoned)
                {
                    if (detail is not null || isCreationAbandoned)
                    {
                        return failed;
                    }

                    isCreationAbandoned = true;
                }
                else
                {
                    if (isCreationAbandoned)
                    {
                        return failed;
                    }

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
            }

            current = new(
                request.TenantId,
                request.AggregateId,
                detail is null ? null : detail with { Sequence = item.SequenceNumber },
                item.SequenceNumber,
                item.GlobalPosition,
                item.Timestamp,
                EvidenceFreshnessState.Current)
            {
                IsCreationAbandoned = isCreationAbandoned,
            };
        }

        return current.Sequence < floor ? failed : current;
    }

    private static bool HasRequiredContent(JsonElement content, Type type)
    {
        if (content.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        Dictionary<string, JsonElement> fields = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in content.EnumerateObject())
        {
            if (!fields.TryAdd(property.Name, property.Value))
            {
                return false;
            }
        }

        string[] required = type.Name switch
        {
            nameof(ProjectCreated) => ["name", "lifecycle"],
            nameof(ProjectCreationAbandoned) => ["reasonCode"],
            nameof(ProjectSetupUpdated) => ["setup"],
            nameof(ProjectFolderSet) => ["folderId", "folderMetadata"],
            nameof(ProjectFolderCreationPending) => ["displayNameIntent", "reasonCode", "retryable"],
            nameof(FileReferenceLinked) => ["fileReferenceId", "folderId", "fileMetadata"],
            nameof(FileReferenceUnlinked) => ["fileReferenceId"],
            nameof(MemoryLinked) => ["memoryReferenceId", "memoryMetadata"],
            nameof(MemoryUnlinked) => ["memoryReferenceId"],
            nameof(ProjectArchived) or nameof(ProjectRestored) => ["lifecycle"],
            nameof(ProjectResolutionConfirmed) => ["conversationId"],
            nameof(ProjectChatBotAuthorityGranted) => ["principalId", "operation", "actorClass", "origin", "grantId", "expiresAt"],
            nameof(ProjectChatBotAuthorityRevoked) => ["principalId", "operation", "actorClass", "origin", "grantId"],
            nameof(ProjectChatBotAuthorityRejected) => ["reasonCode"],
            nameof(ProjectReferenceLinkRejected) or nameof(ProjectReferenceUnlinkRejected) => ["tenantId", "projectId", "referenceKind", "referenceId", "reason"],
            nameof(ProjectCreationRejected) => ["tenantId", "reason"],
            nameof(ProjectArchiveRejected) or nameof(ProjectRestoreRejected) or nameof(ProjectSetupUpdateRejected)
                or nameof(ProjectResolutionConfirmationRejected) => ["tenantId", "projectId", "reason"],
            _ => [],
        };
        if (required.Length == 0)
        {
            return false;
        }

        if (typeof(IProjectEvent).IsAssignableFrom(type))
        {
            required = [.. required, "tenantId", "projectId", "occurredAt"];
        }

        return required.All(name => fields.TryGetValue(name, out JsonElement field)
            && field.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            && (field.ValueKind != JsonValueKind.String || !string.IsNullOrWhiteSpace(field.GetString())));
    }

    private static bool ValidKnownContent(object value, string tenant, string aggregate, long sequence)
    {
        if (value is IProjectEvent project && project.OccurredAt == default)
        {
            return false;
        }

        if (value is ProjectChatBotAuthorityGranted grant && (grant.ExpiresAt == default || grant.ExpiresAt <= grant.OccurredAt))
        {
            return false;
        }

        if (value is ProjectCreated { Lifecycle: not ProjectLifecycle.Active }
            or ProjectArchived { Lifecycle: not ProjectLifecycle.Archived }
            or ProjectRestored { Lifecycle: not ProjectLifecycle.Active })
        {
            return false;
        }

        if (value is ProjectCreated created && !IsValidCreatedBinding(created))
        {
            return false;
        }

        if (value is ProjectCreationAbandoned abandoned)
        {
            return IsValidAbandonedContent(abandoned, tenant, aggregate);
        }

        if (value.GetType().GetProperty("Reason")?.GetValue(value) is ReferenceState reason && !Enum.IsDefined(reason))
        {
            return false;
        }

        if (value.GetType().GetProperty("TenantId")?.GetValue(value) is string scope && scope != tenant)
        {
            return false;
        }

        if (value.GetType().GetProperty("ProjectId")?.GetValue(value) is ProjectId identity && identity.Value != aggregate)
        {
            return false;
        }

        if (Irrelevant.ContainsKey(value.GetType().FullName!))
        {
            return true;
        }

        try
        {
            // Validate only this event's persisted content. A duplicate predates later reference
            // observations, so replaying it onto today's detail would incorrectly rewind its lifetime.
            IProjectEvent known = (IProjectEvent)value;
            ProjectDetailItem baseline = new(
                tenant,
                aggregate,
                "Project",
                null,
                null,
                null,
                value is FileReferenceLinked file ? new(file.FolderId, null, ReferenceState.Included, null, known.OccurredAt) : null,
                [],
                [],
                ProjectLifecycle.Active,
                known.OccurredAt,
                known.OccurredAt,
                sequence);
            ProjectDetailItem? preview = (value is ProjectCreated ? ProjectDetailProjection.Empty : ProjectDetailProjection.Seed(baseline))
                .Apply([new ProjectProjectionEnvelope(tenant, sequence, known)]).Get(tenant, aggregate);
            return preview is not null && ProjectPersistedDetailValidator.IsValid(preview);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return false;
        }
    }

    private static bool IsValidCreatedBinding(ProjectCreated created)
    {
        if (created.TaskStepKey is not null && !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(created.TaskStepKey))
        {
            return false;
        }

        if (created.FolderId is null)
        {
            return created.FolderDisplayName is null
                && created.FolderProvisioningReceiptId is null
                && created.FolderClaimGeneration is null;
        }

        return ProjectCommandValidator.IsSafePersistedReferenceIdentifier(created.FolderId)
            && ProjectCommandValidator.IsSafePersistedMetadata(created.FolderDisplayName, ProjectCommandValidator.MaxNameLength)
            && (created.FolderProvisioningReceiptId is null
                || ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(created.FolderProvisioningReceiptId))
            && (created.FolderClaimGeneration is null || created.FolderClaimGeneration.Value > 0);
    }

    private static bool IsValidAbandonedContent(ProjectCreationAbandoned abandoned, string tenant, string aggregate)
    {
        if (abandoned.TenantId != tenant || abandoned.ProjectId != aggregate)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(abandoned.ReasonCode)
            || !ProjectCommandValidator.IsSafePersistedMetadata(abandoned.ReasonCode, ProjectCommandValidator.MaxNameLength))
        {
            return false;
        }

        if (abandoned.TaskStepKey is not null && !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(abandoned.TaskStepKey))
        {
            return false;
        }

        if (abandoned.FolderId is null)
        {
            return abandoned.FolderClaimGeneration is null;
        }

        return ProjectCommandValidator.IsSafePersistedReferenceIdentifier(abandoned.FolderId)
            && (abandoned.FolderClaimGeneration is null || abandoned.FolderClaimGeneration.Value > 0);
    }
}
