// <copyright file="LegacyProjectFolderClaimReconstructor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Reconstruction;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.SdkWriter.Claims;

/// <summary>
/// Pure AD-35 legacy-history claim reconstructor (Story 7.1).
/// Uses collision-free length-prefixed keys, deduplicates Project snapshots, scans incomplete/corrupt
/// event streams for referenced Folders, closes/quarantines stale pre-existing active claims, and
/// preserves historical <see cref="ProjectFolderCreationPending"/> records.
/// </summary>
public static class LegacyProjectFolderClaimReconstructor
{
    /// <summary>Builds a collision-free length-prefixed composite Folder key.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="folderId">The Folder identifier.</param>
    /// <returns>The length-prefixed Folder key.</returns>
    public static string FolderKey(string tenantId, string folderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);
        return $"{tenantId.Length}:{tenantId}:{folderId.Length}:{folderId}";
    }

    /// <summary>Builds a collision-free length-prefixed composite Project key.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="projectId">The Project identifier.</param>
    /// <returns>The length-prefixed Project key.</returns>
    public static string ProjectKey(string tenantId, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        return $"{tenantId.Length}:{tenantId}:{projectId.Length}:{projectId}";
    }

    /// <summary>Reconstructs AD-35 claim dispositions from authoritative legacy Project streams.</summary>
    /// <param name="snapshots">The authoritative Project stream snapshots.</param>
    /// <param name="knownFolders">Known <c>(TenantId, FolderId)</c> pairs in scope.</param>
    /// <param name="existingClaimStates">Optional pre-existing AD-35 claim states keyed by <see cref="FolderKey"/>.</param>
    /// <returns>The deterministic AD-35 reconstruction plan.</returns>
    public static LegacyProjectFolderClaimPlan Reconstruct(
        IEnumerable<LegacyProjectHistorySnapshot> snapshots,
        IEnumerable<(string TenantId, string FolderId)>? knownFolders = null,
        IReadOnlyDictionary<string, ProjectFolderClaimState>? existingClaimStates = null)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        Dictionary<string, LegacyProjectHistorySnapshot> uniqueSnapshots = new(StringComparer.Ordinal);
        HashSet<string> duplicateConflictingProjects = new(StringComparer.Ordinal);
        HashSet<string> closedFolderKeys = new(StringComparer.Ordinal);
        HashSet<string> quarantinedProjectKeys = new(StringComparer.Ordinal);
        HashSet<string> legacyPendingProjects = new(StringComparer.Ordinal);

        // Step 1: Deduplicate snapshots per length-prefixed ProjectKey.
        foreach (LegacyProjectHistorySnapshot snapshot in snapshots)
        {
            if (snapshot is null
                || string.IsNullOrWhiteSpace(snapshot.TenantId)
                || string.IsNullOrWhiteSpace(snapshot.ProjectId))
            {
                continue;
            }

            string projectKey = ProjectKey(snapshot.TenantId, snapshot.ProjectId);
            if (uniqueSnapshots.TryGetValue(projectKey, out LegacyProjectHistorySnapshot? existing))
            {
                if (!AreEquivalentSnapshots(existing, snapshot))
                {
                    duplicateConflictingProjects.Add(projectKey);
                    quarantinedProjectKeys.Add(projectKey);
                    foreach (string folderId in ExtractReferencedFolderIds(existing.Events))
                    {
                        closedFolderKeys.Add(FolderKey(existing.TenantId, folderId));
                    }

                    foreach (string folderId in ExtractReferencedFolderIds(snapshot.Events))
                    {
                        closedFolderKeys.Add(FolderKey(snapshot.TenantId, folderId));
                    }
                }

                continue;
            }

            uniqueSnapshots[projectKey] = snapshot;
        }

        // Step 2: Fold each unique snapshot and collect Active Folder claimants.
        Dictionary<string, List<(string ProjectKey, string TenantId, string ProjectId)>> activeClaimantsByFolder = new(StringComparer.Ordinal);
        Dictionary<string, ProjectState> foldedStatesByProjectKey = new(StringComparer.Ordinal);

        foreach ((string projectKey, LegacyProjectHistorySnapshot snapshot) in uniqueSnapshots)
        {
            if (duplicateConflictingProjects.Contains(projectKey))
            {
                continue;
            }

            if (snapshot.IsIncompleteHistory || snapshot.Events is null)
            {
                quarantinedProjectKeys.Add(projectKey);
                if (snapshot.Events is not null)
                {
                    foreach (string incompleteFolderId in ExtractReferencedFolderIds(snapshot.Events))
                    {
                        closedFolderKeys.Add(FolderKey(snapshot.TenantId, incompleteFolderId));
                    }
                }

                continue;
            }

            ProjectState folded;
            try
            {
                ProjectIdentity identity = new(snapshot.TenantId, new ProjectId(snapshot.ProjectId));
                folded = ProjectState.Empty.Apply(snapshot.Events, identity);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                // Corrupt or inconsistent stream: quarantine the Project and close every referenced FolderId.
                quarantinedProjectKeys.Add(projectKey);
                foreach (string corruptFolderId in ExtractReferencedFolderIds(snapshot.Events))
                {
                    closedFolderKeys.Add(FolderKey(snapshot.TenantId, corruptFolderId));
                }

                continue;
            }

            foldedStatesByProjectKey[projectKey] = folded;

            if (!folded.IsCreated || folded.IsCreationAbandoned || folded.Lifecycle != ProjectLifecycle.Active)
            {
                continue;
            }

            if (folded.ProjectFolder is { ReferenceState: ReferenceState.Included, FolderId: { Length: > 0 } folderId }
                && !string.IsNullOrWhiteSpace(folderId))
            {
                string folderKey = FolderKey(snapshot.TenantId, folderId);
                if (!activeClaimantsByFolder.TryGetValue(folderKey, out List<(string ProjectKey, string TenantId, string ProjectId)>? claimants))
                {
                    claimants = [];
                    activeClaimantsByFolder[folderKey] = claimants;
                }

                claimants.Add((projectKey, snapshot.TenantId, snapshot.ProjectId));
            }
            else if (folded.ProjectFolder is { ReferenceState: ReferenceState.Pending })
            {
                legacyPendingProjects.Add(projectKey);
            }
        }

        // Step 3: Resolve Folder claimants -> unambiguous incumbents vs conflicts.
        Dictionary<string, (string TenantId, string ProjectId)> incumbentEntries = new(StringComparer.Ordinal);
        foreach ((string folderKey, List<(string ProjectKey, string TenantId, string ProjectId)> claimants) in activeClaimantsByFolder)
        {
            if (closedFolderKeys.Contains(folderKey))
            {
                foreach ((string projectKey, _, _) in claimants)
                {
                    quarantinedProjectKeys.Add(projectKey);
                }

                continue;
            }

            if (claimants.Count > 1)
            {
                closedFolderKeys.Add(folderKey);
                foreach ((string projectKey, _, _) in claimants)
                {
                    quarantinedProjectKeys.Add(projectKey);
                }

                continue;
            }

            incumbentEntries[folderKey] = (claimants[0].TenantId, claimants[0].ProjectId);
        }

        // Step 4: Reconcile pre-existing AD-35 claim states.
        HashSet<string> preExistingOccupiedOrClosedFolders = new(StringComparer.Ordinal);
        if (existingClaimStates is not null)
        {
            foreach ((string folderKey, ProjectFolderClaimState claimState) in existingClaimStates)
            {
                if (claimState is null || string.IsNullOrWhiteSpace(folderKey))
                {
                    continue;
                }

                // Validate existingClaimStates key against populated claimState identity.
                if (!string.IsNullOrWhiteSpace(claimState.TenantId)
                    && !string.IsNullOrWhiteSpace(claimState.FolderId))
                {
                    string expectedFolderKey = FolderKey(claimState.TenantId, claimState.FolderId);
                    if (!string.Equals(folderKey, expectedFolderKey, StringComparison.Ordinal))
                    {
                        preExistingOccupiedOrClosedFolders.Add(folderKey);
                        preExistingOccupiedOrClosedFolders.Add(expectedFolderKey);
                        closedFolderKeys.Add(folderKey);
                        closedFolderKeys.Add(expectedFolderKey);
                        if (incumbentEntries.Remove(folderKey, out (string TenantId, string ProjectId) displacedKey))
                        {
                            quarantinedProjectKeys.Add(ProjectKey(displacedKey.TenantId, displacedKey.ProjectId));
                        }

                        if (incumbentEntries.Remove(expectedFolderKey, out (string TenantId, string ProjectId) displacedExpected))
                        {
                            quarantinedProjectKeys.Add(ProjectKey(displacedExpected.TenantId, displacedExpected.ProjectId));
                        }

                        if (claimState.ActiveClaim is { } badActive
                            && !string.IsNullOrWhiteSpace(badActive.TenantId)
                            && !string.IsNullOrWhiteSpace(badActive.ProjectId))
                        {
                            quarantinedProjectKeys.Add(ProjectKey(badActive.TenantId, badActive.ProjectId));
                        }

                        continue;
                    }
                }

                if (claimState.IsClosed)
                {
                    preExistingOccupiedOrClosedFolders.Add(folderKey);
                    closedFolderKeys.Add(folderKey);
                    if (incumbentEntries.Remove(folderKey, out (string TenantId, string ProjectId) displaced))
                    {
                        quarantinedProjectKeys.Add(ProjectKey(displaced.TenantId, displaced.ProjectId));
                    }

                    continue;
                }

                if (!claimState.IsActive || claimState.ActiveClaim is not { } activeClaim)
                {
                    continue;
                }

                preExistingOccupiedOrClosedFolders.Add(folderKey);
                string claimedProjectKey = ProjectKey(activeClaim.TenantId, activeClaim.ProjectId);

                // Does this pre-existing active claim match the unambiguous Active incumbent?
                if (incumbentEntries.TryGetValue(folderKey, out (string TenantId, string ProjectId) expectedIncumbent))
                {
                    if (!string.Equals(expectedIncumbent.ProjectId, activeClaim.ProjectId, StringComparison.Ordinal))
                    {
                        // Pre-existing active claim conflicts with history incumbent: close Folder and quarantine both!
                        incumbentEntries.Remove(folderKey);
                        closedFolderKeys.Add(folderKey);
                        quarantinedProjectKeys.Add(ProjectKey(expectedIncumbent.TenantId, expectedIncumbent.ProjectId));
                        quarantinedProjectKeys.Add(claimedProjectKey);
                    }
                }
                else
                {
                    // Pre-existing active claim belongs to an archived, changed-folder, corrupt, or absent Project:
                    // close & quarantine this stale active claim (never omit it and never copy into IncumbentClaims).
                    closedFolderKeys.Add(folderKey);
                    if (foldedStatesByProjectKey.ContainsKey(claimedProjectKey)
                        || uniqueSnapshots.ContainsKey(claimedProjectKey))
                    {
                        quarantinedProjectKeys.Add(claimedProjectKey);
                    }
                }
            }
        }

        // Revoke/close any incumbent claim owned by a Project that was quarantined during Step 4.
        List<string> revokedFolders = [];
        foreach ((string folderKey, (string TenantId, string ProjectId) incumbent) in incumbentEntries)
        {
            if (quarantinedProjectKeys.Contains(ProjectKey(incumbent.TenantId, incumbent.ProjectId)))
            {
                revokedFolders.Add(folderKey);
            }
        }

        foreach (string revokedFolderKey in revokedFolders)
        {
            incumbentEntries.Remove(revokedFolderKey);
            closedFolderKeys.Add(revokedFolderKey);
        }

        Dictionary<string, string> incumbentClaims = incumbentEntries.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ProjectId,
            StringComparer.Ordinal);

        // Step 5: Compute truly free Folder keys.
        HashSet<string> freeFolderKeys = new(StringComparer.Ordinal);
        if (knownFolders is not null)
        {
            foreach ((string tenantId, string folderId) in knownFolders)
            {
                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(folderId))
                {
                    continue;
                }

                string folderKey = FolderKey(tenantId, folderId);
                if (!incumbentClaims.ContainsKey(folderKey)
                    && !closedFolderKeys.Contains(folderKey)
                    && !preExistingOccupiedOrClosedFolders.Contains(folderKey))
                {
                    freeFolderKeys.Add(folderKey);
                }
            }
        }

        return new LegacyProjectFolderClaimPlan(
            incumbentClaims,
            closedFolderKeys,
            quarantinedProjectKeys,
            freeFolderKeys,
            legacyPendingProjects);
    }

    private static bool AreEquivalentSnapshots(LegacyProjectHistorySnapshot left, LegacyProjectHistorySnapshot right)
    {
        if (left.IsIncompleteHistory != right.IsIncompleteHistory)
        {
            return false;
        }

        if (left.Events is null || right.Events is null)
        {
            return left.Events is null && right.Events is null;
        }

        if (left.Events.Count != right.Events.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Events.Count; index++)
        {
            if (!Equals(left.Events[index], right.Events[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> ExtractReferencedFolderIds(IEnumerable<IProjectEvent>? events)
    {
        if (events is null)
        {
            return [];
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (IProjectEvent projectEvent in events)
        {
            switch (projectEvent)
            {
                case ProjectCreated { FolderId: { } createdFolderId } when !string.IsNullOrWhiteSpace(createdFolderId):
                    ids.Add(createdFolderId);
                    break;
                case ProjectFolderSet { FolderId: { } setFolderId } when !string.IsNullOrWhiteSpace(setFolderId):
                    ids.Add(setFolderId);
                    break;
                case FileReferenceLinked { FolderId: { } fileFolderId } when !string.IsNullOrWhiteSpace(fileFolderId):
                    ids.Add(fileFolderId);
                    break;
                case ProjectCreationAbandoned { FolderId: { } abandonedFolderId } when !string.IsNullOrWhiteSpace(abandonedFolderId):
                    ids.Add(abandonedFolderId);
                    break;
            }
        }

        return ids;
    }
}
