// <copyright file="ProjectReferenceIndexProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectReferenceIndex;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>
/// Tenant-scoped, deterministic metadata-only index of Project sibling references.
/// </summary>
public sealed record ProjectReferenceIndexProjection
{
    private const string FolderReferenceKind = "folder";
    private const string PendingFolderReferenceKey = "_pending_project_folder";

    private ProjectReferenceIndexProjection(IReadOnlyDictionary<string, ProjectReferenceIndexItem> references)
    {
        References = references;
    }

    /// <summary>Gets the indexed references keyed by tenant/project/kind/reference.</summary>
    public IReadOnlyDictionary<string, ProjectReferenceIndexItem> References { get; }

    /// <summary>Gets the empty starting projection.</summary>
    public static ProjectReferenceIndexProjection Empty { get; } = new(FrozenDictionary<string, ProjectReferenceIndexItem>.Empty);

    /// <summary>Folds reference events into the index.</summary>
    /// <param name="envelopes">The projection envelopes.</param>
    /// <returns>The updated projection.</returns>
    public ProjectReferenceIndexProjection Apply(IEnumerable<ProjectProjectionEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        Dictionary<string, ProjectReferenceIndexItem> references = new(References, StringComparer.Ordinal);
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

            switch (envelope.Event)
            {
                case ProjectCreated or ProjectSetupUpdated or ProjectArchived:
                    break;

                case ProjectFolderSet folderSet:
                    RemoveProjectFolderReferences(references, folderSet.TenantId, folderSet.ProjectId);
                    references[Key(folderSet.TenantId, folderSet.ProjectId, folderSet.FolderId)] = new ProjectReferenceIndexItem(
                        folderSet.TenantId,
                        folderSet.ProjectId,
                        FolderReferenceKind,
                        folderSet.FolderId,
                        ReferenceState.Included,
                        folderSet.FolderMetadata.DisplayName,
                        null,
                        folderSet.OccurredAt,
                        envelope.Sequence);
                    break;

                case ProjectFolderCreationPending pending:
                    if (!HasIncludedProjectFolder(references, pending.TenantId, pending.ProjectId))
                    {
                        RemoveProjectFolderReferences(references, pending.TenantId, pending.ProjectId);
                        references[Key(pending.TenantId, pending.ProjectId, PendingFolderReferenceKey)] = new ProjectReferenceIndexItem(
                            pending.TenantId,
                            pending.ProjectId,
                            FolderReferenceKind,
                            null,
                            ReferenceState.Pending,
                            pending.DisplayNameIntent,
                            pending.ReasonCode,
                            pending.OccurredAt,
                            envelope.Sequence);
                    }

                    break;

                default:
                    throw new InvalidOperationException(
                        $"ProjectReferenceIndexProjection received an unsupported event type "
                        + $"'{envelope.Event.GetType().FullName}' at sequence {envelope.Sequence}.");
            }
        }

        return new ProjectReferenceIndexProjection(references.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Lists references for a tenant/project.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="projectId">The project identifier.</param>
    /// <returns>The matching reference rows.</returns>
    public IReadOnlyList<ProjectReferenceIndexItem> List(string tenantId, string projectId)
    {
        string? prefix = TryPrefix(tenantId, projectId);
        if (prefix is null)
        {
            return [];
        }

        return References
            .Where(entry => entry.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(entry => entry.Value)
            .OrderBy(item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(item => item.ReferenceId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void RemoveProjectFolderReferences(
        Dictionary<string, ProjectReferenceIndexItem> references,
        string tenantId,
        string projectId)
    {
        string? prefix = TryPrefix(tenantId, projectId);
        if (prefix is null)
        {
            return;
        }

        foreach (string key in references.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
        {
            references.Remove(key);
        }
    }

    private static bool HasIncludedProjectFolder(
        IReadOnlyDictionary<string, ProjectReferenceIndexItem> references,
        string tenantId,
        string projectId)
    {
        string? prefix = TryPrefix(tenantId, projectId);
        return prefix is not null
            && references.Any(entry =>
                entry.Key.StartsWith(prefix, StringComparison.Ordinal)
                && entry.Value.ReferenceState == ReferenceState.Included);
    }

    private static string Key(string tenantId, string projectId, string referenceId)
    {
        string? prefix = TryPrefix(tenantId, projectId);
        return prefix is null ? referenceId : prefix + referenceId;
    }

    private static string? TryPrefix(string tenantId, string projectId)
    {
        try
        {
            string identity = new ProjectIdentity(tenantId, new ProjectId(projectId)).GlobalId;
            return identity + ":references:" + FolderReferenceKind + ":";
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            return null;
        }
    }
}
