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
/// <remarks>
/// Reference rows are keyed by tenant/project/<b>kind</b>/reference so the folder and file reference
/// lanes are disjoint: replacing or pending-flagging the single Project Folder only ever touches the
/// <c>folder</c>-kind rows, and linking/unlinking an optional File Reference only ever touches a single
/// <c>file</c>-kind row. File unlink can never remove the Project Folder row and folder replacement can
/// never remove file rows.
/// </remarks>
public sealed record ProjectReferenceIndexProjection
{
    private const string FolderReferenceKind = "folder";
    private const string FileReferenceKind = "file";
    private const string MemoryReferenceKind = "memory";
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
                    RemoveReferences(references, folderSet.TenantId, folderSet.ProjectId, FolderReferenceKind);
                    references[Key(folderSet.TenantId, folderSet.ProjectId, FolderReferenceKind, folderSet.FolderId)] = new ProjectReferenceIndexItem(
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
                        RemoveReferences(references, pending.TenantId, pending.ProjectId, FolderReferenceKind);
                        references[Key(pending.TenantId, pending.ProjectId, FolderReferenceKind, PendingFolderReferenceKey)] = new ProjectReferenceIndexItem(
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

                case FileReferenceLinked linked:
                    references[Key(linked.TenantId, linked.ProjectId, FileReferenceKind, linked.FileReferenceId)] = new ProjectReferenceIndexItem(
                        linked.TenantId,
                        linked.ProjectId,
                        FileReferenceKind,
                        linked.FileReferenceId,
                        ReferenceState.Included,
                        linked.FileMetadata.DisplayName,
                        null,
                        linked.OccurredAt,
                        envelope.Sequence);
                    break;

                case FileReferenceUnlinked unlinked:
                    references.Remove(Key(unlinked.TenantId, unlinked.ProjectId, FileReferenceKind, unlinked.FileReferenceId));
                    break;

                case MemoryLinked memoryLinked:
                    references[Key(memoryLinked.TenantId, memoryLinked.ProjectId, MemoryReferenceKind, memoryLinked.MemoryReferenceId)] = new ProjectReferenceIndexItem(
                        memoryLinked.TenantId,
                        memoryLinked.ProjectId,
                        MemoryReferenceKind,
                        memoryLinked.MemoryReferenceId,
                        ReferenceState.Included,
                        memoryLinked.MemoryMetadata.DisplayName,
                        null,
                        memoryLinked.OccurredAt,
                        envelope.Sequence);
                    break;

                case MemoryUnlinked memoryUnlinked:
                    references.Remove(Key(memoryUnlinked.TenantId, memoryUnlinked.ProjectId, MemoryReferenceKind, memoryUnlinked.MemoryReferenceId));
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
    /// <returns>The matching reference rows ordered by reference kind then reference id.</returns>
    public IReadOnlyList<ProjectReferenceIndexItem> List(string tenantId, string projectId)
    {
        string? prefix = TryProjectReferencesPrefix(tenantId, projectId);
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

    /// <summary>Lists tenant-scoped folder/file reference rows that match the presented attachment identifiers.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="folderIds">The presented folder identifiers.</param>
    /// <param name="fileReferenceIds">The presented file reference identifiers.</param>
    /// <returns>The matching reference rows ordered by project, reference kind, and reference id.</returns>
    public IReadOnlyList<ProjectReferenceIndexItem> ListByReference(
        string tenantId,
        IEnumerable<string> folderIds,
        IEnumerable<string> fileReferenceIds)
    {
        ArgumentNullException.ThrowIfNull(folderIds);
        ArgumentNullException.ThrowIfNull(fileReferenceIds);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return [];
        }

        HashSet<string> folders = folderIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> files = fileReferenceIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        if (folders.Count == 0 && files.Count == 0)
        {
            return [];
        }

        string tenant = tenantId.Trim();
        return References.Values
            .Where(item => string.Equals(item.TenantId, tenant, StringComparison.Ordinal))
            .Where(item => item.ReferenceId is not null)
            .Where(item =>
                (string.Equals(item.ReferenceKind, FolderReferenceKind, StringComparison.Ordinal) && folders.Contains(item.ReferenceId!))
                || (string.Equals(item.ReferenceKind, FileReferenceKind, StringComparison.Ordinal) && files.Contains(item.ReferenceId!)))
            .OrderBy(item => item.ProjectId, StringComparer.Ordinal)
            .ThenBy(item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(item => item.ReferenceId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void RemoveReferences(
        Dictionary<string, ProjectReferenceIndexItem> references,
        string tenantId,
        string projectId,
        string referenceKind)
    {
        string? prefix = TryKindPrefix(tenantId, projectId, referenceKind);
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
        string? prefix = TryKindPrefix(tenantId, projectId, FolderReferenceKind);
        return prefix is not null
            && references.Any(entry =>
                entry.Key.StartsWith(prefix, StringComparison.Ordinal)
                && entry.Value.ReferenceState == ReferenceState.Included);
    }

    private static string Key(string tenantId, string projectId, string referenceKind, string referenceId)
    {
        string? prefix = TryKindPrefix(tenantId, projectId, referenceKind);
        return prefix is null ? referenceId : prefix + referenceId;
    }

    private static string? TryKindPrefix(string tenantId, string projectId, string referenceKind)
    {
        string? projectPrefix = TryProjectReferencesPrefix(tenantId, projectId);
        return projectPrefix is null ? null : projectPrefix + referenceKind + ":";
    }

    private static string? TryProjectReferencesPrefix(string tenantId, string projectId)
    {
        try
        {
            string identity = new ProjectIdentity(tenantId, new ProjectId(projectId)).GlobalId;
            return identity + ":references:";
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            return null;
        }
    }
}
