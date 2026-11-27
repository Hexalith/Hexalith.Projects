// <copyright file="ProjectAuditTimelineProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectAuditTimeline;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using System.Text.Json;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Tenant-scoped, deterministic, metadata-only audit projection over Project success events.
/// </summary>
public sealed record ProjectAuditTimelineProjection
{
    private const string FolderReferenceKind = "folder";
    private const string FileReferenceKind = "file";
    private const string MemoryReferenceKind = "memory";
    private const string ConversationReferenceKind = "conversation";

    private ProjectAuditTimelineProjection(IReadOnlyDictionary<string, ProjectAuditTimelineItem> rows)
    {
        Rows = rows;
    }

    /// <summary>Gets audit rows keyed by deterministic audit event id.</summary>
    public IReadOnlyDictionary<string, ProjectAuditTimelineItem> Rows { get; }

    /// <summary>Gets the empty audit projection.</summary>
    public static ProjectAuditTimelineProjection Empty { get; } = new(FrozenDictionary<string, ProjectAuditTimelineItem>.Empty);

    /// <summary>Rebuilds the audit projection from a full Project event stream.</summary>
    /// <param name="envelopes">The Project projection envelopes.</param>
    /// <returns>The rebuilt audit projection.</returns>
    public static ProjectAuditTimelineProjection Rebuild(IEnumerable<ProjectProjectionEnvelope> envelopes)
        => Empty.Apply(envelopes);

    /// <summary>Applies Project projection envelopes to the audit timeline.</summary>
    /// <param name="envelopes">The Project projection envelopes.</param>
    /// <returns>The updated audit projection.</returns>
    public ProjectAuditTimelineProjection Apply(IEnumerable<ProjectProjectionEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        Dictionary<string, ProjectAuditTimelineItem> rows = new(Rows, StringComparer.Ordinal);
        IEnumerable<ProjectProjectionEnvelope> ordered = envelopes
            .Where(static envelope => envelope is not null)
            .Where(static envelope => envelope.Event is not null)
            .OrderBy(static envelope => envelope.Sequence)
            .ThenBy(static envelope => envelope.Event.IdempotencyKey, StringComparer.Ordinal)
            .ThenBy(static envelope => envelope.Event.IdempotencyFingerprint, StringComparer.Ordinal);

        foreach (ProjectProjectionEnvelope envelope in ordered)
        {
            if (!string.Equals(envelope.TenantId, envelope.Event.TenantId, StringComparison.Ordinal))
            {
                continue;
            }

            ProjectAuditTimelineItem item = ToTimelineItem(envelope);
            rows[item.AuditEventId] = item;
        }

        return new ProjectAuditTimelineProjection(rows.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Lists tenant-scoped audit rows, optionally filtered to a single Project, newest first.</summary>
    /// <param name="tenantId">The authoritative tenant identifier.</param>
    /// <param name="projectId">The optional Project identifier filter.</param>
    /// <param name="limit">The optional maximum row count.</param>
    /// <returns>The matching audit rows.</returns>
    public IReadOnlyList<ProjectAuditTimelineItem> List(string tenantId, string? projectId, int? limit)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return [];
        }

        string tenant = tenantId.Trim();
        IEnumerable<ProjectAuditTimelineItem> query = Rows.Values
            .Where(row => string.Equals(row.TenantId, tenant, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(projectId))
        {
            string project = projectId.Trim();
            query = query.Where(row => string.Equals(row.ProjectId, project, StringComparison.Ordinal));
        }

        query = query
            .OrderByDescending(static row => row.Sequence)
            .ThenByDescending(static row => row.OccurredAt)
            .ThenBy(static row => row.AuditEventId, StringComparer.Ordinal);

        if (limit is > 0)
        {
            query = query.Take(limit.Value);
        }

        return query.ToArray();
    }

    /// <summary>Maps committed mutation facts to the supported Safe Metadata contract.</summary>
    public static ProjectAuditFact? ToSupportedFact(ProjectAuditTimelineItem row)
    {
        ArgumentNullException.ThrowIfNull(row);
        string? outcome = row.OperationType switch
        {
            "project.created" or "project.restored" => "Active",
            "project.creation_abandoned" => "Abandoned",
            "project.archived" => "Archived",
            "project.setup_updated" => "Updated",
            "project.folder_set" or "file_reference.linked" or "memory.linked" => "Included",
            "file_reference.unlinked" or "memory.unlinked" => "Excluded",
            "project.resolution_confirmed" => "Confirmed",
            "chatbot.authority_granted" => "Granted",
            "chatbot.authority_revoked" => "Revoked",
            _ => null,
        };
        if (outcome is null) return null;
        // Aggregate idempotency is scoped by identity and operation. Delivery positions can change on replay.
        string id = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            row.TenantId, row.ProjectId, row.ActorPrincipalId, row.OperationType,
            string.IsNullOrWhiteSpace(row.IdempotencyKey) ? row.AuditEventId : row.IdempotencyKey,
        })));
        return new("audit_" + id, row.OperationType, outcome, row.OccurredAt);
    }

    /// <summary>Projects one committed mutation through the same allow-list as the retained journal.</summary>
    public static ProjectAuditFact? ToSupportedFact(ProjectProjectionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return ToSupportedFact(ToTimelineItem(envelope));
    }

    private static ProjectAuditTimelineItem ToTimelineItem(ProjectProjectionEnvelope envelope)
        => envelope.Event switch
        {
            ProjectCreated created => Item(
                envelope,
                created,
                created.ActorPrincipalId,
                "project.created",
                string.IsNullOrWhiteSpace(created.FolderId) ? null : FolderReferenceKind,
                string.IsNullOrWhiteSpace(created.FolderId) ? null : created.FolderId,
                null,
                created.Lifecycle.ToString(),
                null,
                null,
                null),
            ProjectCreationAbandoned abandoned => Item(
                envelope,
                abandoned,
                abandoned.ActorPrincipalId,
                "project.creation_abandoned",
                string.IsNullOrWhiteSpace(abandoned.FolderId) ? null : FolderReferenceKind,
                string.IsNullOrWhiteSpace(abandoned.FolderId) ? null : abandoned.FolderId,
                null,
                "Abandoned",
                abandoned.ReasonCode,
                null,
                null),
            ProjectSetupUpdated updated => Item(envelope, updated, updated.ActorPrincipalId, "project.setup_updated", null, null, null, "Updated", null, null, null),
            ProjectArchived archived => Item(envelope, archived, archived.ActorPrincipalId, "project.archived", null, null, ProjectLifecycle.Active.ToString(), archived.Lifecycle.ToString(), null, null, null),
            ProjectRestored restored => Item(envelope, restored, restored.ActorPrincipalId, "project.restored", null, null, ProjectLifecycle.Archived.ToString(), restored.Lifecycle.ToString(), null, null, null),
            ProjectFolderCreationPending pending => Item(envelope, pending, pending.ActorPrincipalId, "project.folder_creation_pending", FolderReferenceKind, null, null, ReferenceState.Pending.ToString(), pending.ReasonCode, null, null),
            ProjectFolderSet folderSet => Item(envelope, folderSet, folderSet.ActorPrincipalId, "project.folder_set", FolderReferenceKind, folderSet.FolderId, null, ReferenceState.Included.ToString(), null, null, null),
            FileReferenceLinked linked => Item(envelope, linked, linked.ActorPrincipalId, "file_reference.linked", FileReferenceKind, linked.FileReferenceId, null, ReferenceState.Included.ToString(), null, null, null),
            FileReferenceUnlinked unlinked => Item(envelope, unlinked, unlinked.ActorPrincipalId, "file_reference.unlinked", FileReferenceKind, unlinked.FileReferenceId, ReferenceState.Included.ToString(), ReferenceState.Excluded.ToString(), null, null, null),
            MemoryLinked linked => Item(envelope, linked, linked.ActorPrincipalId, "memory.linked", MemoryReferenceKind, linked.MemoryReferenceId, null, ReferenceState.Included.ToString(), null, null, null),
            MemoryUnlinked unlinked => Item(envelope, unlinked, unlinked.ActorPrincipalId, "memory.unlinked", MemoryReferenceKind, unlinked.MemoryReferenceId, ReferenceState.Included.ToString(), ReferenceState.Excluded.ToString(), null, null, null),
            ProjectChatBotAuthorityGranted granted => Item(envelope, granted, granted.ActorPrincipalId, "chatbot.authority_granted", null, null, null, "Granted", null, null, null),
            ProjectChatBotAuthorityRevoked revoked => Item(envelope, revoked, revoked.ActorPrincipalId, "chatbot.authority_revoked", null, null, null, "Revoked", null, null, null),
            ProjectResolutionConfirmed confirmed => Item(envelope, confirmed, confirmed.ActorPrincipalId, "project.resolution_confirmed", ConversationReferenceKind, confirmed.ConversationId, null, "Confirmed", null, confirmed.ConversationId, confirmed.SourceProjectId),
            _ => throw new InvalidOperationException(
                $"ProjectAuditTimelineProjection received an unsupported event type "
                + $"'{envelope.Event.GetType().FullName}' at sequence {envelope.Sequence}."),
        };

    private static ProjectAuditTimelineItem Item(
        ProjectProjectionEnvelope envelope,
        IProjectEvent projectEvent,
        string actorPrincipalId,
        string operationType,
        string? referenceKind,
        string? referenceId,
        string? previousState,
        string? newState,
        string? reasonCode,
        string? conversationId,
        string? sourceProjectId)
        => new(
            projectEvent.TenantId,
            projectEvent.ProjectId,
            AuditEventId(envelope, operationType, referenceKind, referenceId),
            operationType,
            projectEvent.OccurredAt,
            actorPrincipalId,
            projectEvent.CorrelationId,
            projectEvent.TaskId,
            projectEvent.IdempotencyKey,
            referenceKind,
            referenceId,
            previousState,
            newState,
            reasonCode,
            conversationId,
            sourceProjectId,
            envelope.Sequence);

    private static string AuditEventId(
        ProjectProjectionEnvelope envelope,
        string operationType,
        string? referenceKind,
        string? referenceId)
    {
        string material = string.Join(
            "|",
            envelope.TenantId,
            envelope.Event.ProjectId,
            envelope.Event.GetType().FullName,
            envelope.Sequence.ToString(CultureInfo.InvariantCulture),
            envelope.Event.IdempotencyKey,
            envelope.Event.IdempotencyFingerprint,
            operationType,
            referenceKind ?? string.Empty,
            referenceId ?? string.Empty);

        return "audit_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }
}
