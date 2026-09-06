// <copyright file="ProjectContextAdmissionAssembler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Maps a pure allowlist assembly onto AD-32 usability without re-deciding include/exclude verdicts.
/// </summary>
public static class ProjectContextAdmissionAssembler
{
    private const string FileKind = "file";
    private const string MemoryKind = "memory";
    private const string ConversationKind = "conversation";
    private const string FolderKind = "folder";

    /// <summary>
    /// Builds an AD-32 admission from an already-evaluated allowlist result and required-evidence checks.
    /// </summary>
    /// <param name="assembled">The pure allowlist assembly result.</param>
    /// <param name="tenantAccess">The tenant-access evidence used for the assembly.</param>
    /// <param name="references">The candidate reference evidence supplied to the allowlist.</param>
    /// <param name="projectVersion">The authorized persisted Project version.</param>
    /// <param name="asOf">The authoritative evidence cutoff.</param>
    /// <param name="ownerBackedTrustAvailable">Whether an accepted Reference Trust Index is available.</param>
    /// <returns>The AD-32 admission.</returns>
    public static ProjectContextAdmission FromAssembled(
        ProjectContextAssemblyResult assembled,
        ProjectContextTenantAccess tenantAccess,
        ProjectContextReferenceEvidence references,
        long projectVersion,
        DateTimeOffset asOf,
        bool ownerBackedTrustAvailable)
    {
        ArgumentNullException.ThrowIfNull(assembled);
        ArgumentNullException.ThrowIfNull(tenantAccess);
        ArgumentNullException.ThrowIfNull(references);

        ProjectContext context = assembled.Context;
        if (context.AssemblyOutcome is ProjectContextAssemblyOutcome.Unauthorized
            or ProjectContextAssemblyOutcome.ProjectUnavailable)
        {
            return ProjectContextAdmission.SafeDenial(context.ProjectId);
        }

        int candidateCount = CountCandidates(references);
        if (candidateCount > ProjectContextReadLimits.MaxReferences
            || HasDuplicateIdentities(references)
            || HasUnknownKind(assembled))
        {
            return Unavailable(
                context.ProjectId,
                context.Lifecycle,
                asOf,
                projectVersion,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: false,
                overflow: candidateCount > ProjectContextReadLimits.MaxReferences);
        }

        if (IsRequiredEvidenceStale(tenantAccess, context.Freshness))
        {
            return Unavailable(
                context.ProjectId,
                context.Lifecycle,
                asOf,
                projectVersion,
                folderIncluded: context.ProjectFolder is not null,
                setupCurrent: true,
                authorizationCurrent: false,
                overflow: false);
        }

        List<ProjectContextReference> files = [.. context.FileReferences];
        List<ProjectContextReference> memories = [.. context.MemoryReferences];
        List<ProjectContextReference> conversations = [.. context.Conversations];
        List<ProjectContextExclusion> excluded = [.. context.Excluded];
        List<ProjectContextEvaluation> evaluations = [.. assembled.Evaluations];

        ProjectSetup setup = context.Setup ?? ProjectSetup.Empty;
        ApplyExcludedSourceKinds(setup, files, memories, conversations, excluded, evaluations);
        ApplyMissingOwnerBackedTrust(
            ownerBackedTrustAvailable,
            files,
            memories,
            conversations,
            excluded,
            evaluations);

        files = SortRefs(files);
        memories = SortRefs(memories);
        conversations = SortRefs(conversations);
        excluded = SortExclusions(excluded);
        evaluations = SortEvaluations(evaluations);

        bool folderCurrent = context.ProjectFolder is { ReferenceState: ReferenceState.Included };
        if (!folderCurrent)
        {
            return Unavailable(
                context.ProjectId,
                context.Lifecycle,
                asOf,
                projectVersion,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                overflow: false);
        }

        bool hasOptionalOmission = excluded.Count > 0;
        AdmissionResponseState responseState = hasOptionalOmission
            ? AdmissionResponseState.Partial
            : AdmissionResponseState.Complete;
        IReadOnlyList<string> recovery = hasOptionalOmission
            ? PartialRecoveryActions(excluded)
            : [AdmissionRecoveryAction.None];

        return new ProjectContextAdmission(
            false,
            context.ProjectId,
            context.Lifecycle,
            setup,
            context.ProjectFolder,
            conversations,
            files,
            memories,
            excluded,
            evaluations,
            new AdmissionSnapshot(
                responseState,
                asOf,
                projectVersion,
                BuildComponents(
                    folderIncluded: true,
                    setupCurrent: true,
                    authorizationCurrent: true,
                    referencesComplete: !hasOptionalOmission),
                recovery));
    }

    /// <summary>Builds a required-evidence <see cref="AdmissionResponseState.Unavailable"/> admission.</summary>
    /// <param name="projectId">The opaque Project identifier.</param>
    /// <param name="lifecycle">The owning Project lifecycle.</param>
    /// <param name="asOf">The authoritative evidence cutoff.</param>
    /// <param name="projectVersion">The authorized persisted Project version.</param>
    /// <param name="folderIncluded">Whether a current authorized Folder was confirmed.</param>
    /// <param name="setupCurrent">Whether Setup evidence is current.</param>
    /// <param name="authorizationCurrent">Whether authorization evidence is current.</param>
    /// <param name="overflow">Whether the candidate set exceeded the approved bound.</param>
    /// <returns>An unavailable admission that discloses no fabricated collections.</returns>
    public static ProjectContextAdmission Unavailable(
        string projectId,
        ProjectLifecycle lifecycle,
        DateTimeOffset asOf,
        long projectVersion,
        bool folderIncluded,
        bool setupCurrent,
        bool authorizationCurrent,
        bool overflow)
        => new(
            false,
            projectId,
            lifecycle,
            Setup: null,
            ProjectFolder: null,
            Conversations: [],
            FileReferences: [],
            MemoryReferences: [],
            Excluded: [],
            Evaluations: [],
            new AdmissionSnapshot(
                AdmissionResponseState.Unavailable,
                asOf,
                projectVersion,
                BuildComponents(folderIncluded, setupCurrent, authorizationCurrent, referencesComplete: false),
                overflow
                    ? [AdmissionRecoveryAction.Retry, AdmissionRecoveryAction.ContactAdministrator]
                    : [AdmissionRecoveryAction.RefreshContext, AdmissionRecoveryAction.ContactAdministrator]));

    /// <summary>Counts folder, file, memory, and conversation candidates.</summary>
    /// <param name="references">The candidate evidence.</param>
    /// <returns>The candidate count.</returns>
    public static int CountCandidates(ProjectContextReferenceEvidence references)
    {
        ArgumentNullException.ThrowIfNull(references);
        return (references.ProjectFolder is null ? 0 : 1)
            + references.FileReferences.Count
            + references.MemoryReferences.Count
            + references.Conversations.Count;
    }

    /// <summary>Returns whether any per-kind candidate identity is duplicated.</summary>
    /// <param name="references">The candidate evidence.</param>
    /// <returns><see langword="true"/> when a duplicate identity exists.</returns>
    public static bool HasDuplicateIdentities(ProjectContextReferenceEvidence references)
    {
        ArgumentNullException.ThrowIfNull(references);
        return HasDuplicates(references.FileReferences.Select(static file => file.FileReferenceId))
            || HasDuplicates(references.MemoryReferences.Select(static memory => memory.MemoryReferenceId))
            || HasDuplicates(references.Conversations.Select(static conversation => conversation.ConversationId));
    }

    private static bool HasUnknownKind(ProjectContextAssemblyResult assembled)
        => assembled.Evaluations.Any(static evaluation =>
            evaluation.FailedCheck == ProjectContextInclusionCheck.ReferenceKindAllowlist
            && string.Equals(
                evaluation.Diagnostic,
                ProjectContextInclusionDiagnostic.ReferenceKindNotAllowlisted,
                StringComparison.Ordinal));

    private static bool IsRequiredEvidenceStale(
        ProjectContextTenantAccess tenantAccess,
        ProjectContextFreshness freshness)
        => tenantAccess.Result.Outcome == TenantAccessOutcome.StaleProjection
            || tenantAccess.Result.FreshnessStatus == TenantProjectionFreshnessStatus.Stale
            || freshness == ProjectContextFreshness.Stale;

    private static void ApplyExcludedSourceKinds(
        ProjectSetup setup,
        List<ProjectContextReference> files,
        List<ProjectContextReference> memories,
        List<ProjectContextReference> conversations,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextEvaluation> evaluations)
    {
        HashSet<string> excludedKinds = new(StringComparer.Ordinal);
        foreach (ProjectContextSourceKind kind in setup.ExcludedSourceKinds)
        {
            string? mapped = MapSourceKind(kind);
            if (mapped is not null && mapped != FolderKind)
            {
                excludedKinds.Add(mapped);
            }
        }

        if (excludedKinds.Count == 0)
        {
            return;
        }

        RelocateByKind(FileKind, excludedKinds, files, excluded, evaluations);
        RelocateByKind(MemoryKind, excludedKinds, memories, excluded, evaluations);
        RelocateByKind(ConversationKind, excludedKinds, conversations, excluded, evaluations);
    }

    private static void ApplyMissingOwnerBackedTrust(
        bool ownerBackedTrustAvailable,
        List<ProjectContextReference> files,
        List<ProjectContextReference> memories,
        List<ProjectContextReference> conversations,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextEvaluation> evaluations)
    {
        if (ownerBackedTrustAvailable)
        {
            return;
        }

        RelocateMissingTrust(ConversationKind, conversations, excluded, evaluations);
    }

    private static void RelocateByKind(
        string kind,
        HashSet<string> excludedKinds,
        List<ProjectContextReference> included,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextEvaluation> evaluations)
    {
        if (!excludedKinds.Contains(kind))
        {
            return;
        }

        foreach (ProjectContextReference reference in included.ToArray())
        {
            included.Remove(reference);
            Exclude(
                reference,
                ReferenceState.Excluded,
                ProjectContextInclusionCheck.ReferenceKindAllowlist,
                diagnostic: null,
                excluded,
                evaluations);
        }
    }

    private static void RelocateMissingTrust(
        string kind,
        List<ProjectContextReference> included,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextEvaluation> evaluations)
    {
        foreach (ProjectContextReference reference in included.Where(item => string.Equals(item.ReferenceKind, kind, StringComparison.Ordinal)).ToArray())
        {
            included.Remove(reference);
            Exclude(
                reference,
                ReferenceState.Unavailable,
                ProjectContextInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceUnavailable,
                excluded,
                evaluations);
        }
    }

    private static void Exclude(
        ProjectContextReference reference,
        ReferenceState state,
        ProjectContextInclusionCheck failedCheck,
        string? diagnostic,
        List<ProjectContextExclusion> excluded,
        List<ProjectContextEvaluation> evaluations)
    {
        excluded.RemoveAll(item =>
            string.Equals(item.ReferenceKind, reference.ReferenceKind, StringComparison.Ordinal)
            && string.Equals(item.ReferenceId, reference.ReferenceId, StringComparison.Ordinal));
        excluded.Add(new ProjectContextExclusion(
            reference.ReferenceKind,
            reference.ReferenceId,
            state,
            ReasonCode: null,
            failedCheck,
            diagnostic));

        evaluations.RemoveAll(item =>
            string.Equals(item.ReferenceKind, reference.ReferenceKind, StringComparison.Ordinal)
            && string.Equals(item.ReferenceId, reference.ReferenceId, StringComparison.Ordinal));
        evaluations.Add(new ProjectContextEvaluation(
            reference.ReferenceKind,
            reference.ReferenceId,
            state,
            failedCheck,
            ReasonCode: null,
            diagnostic,
            reference.ObservedAt));
    }

    private static IReadOnlyList<string> PartialRecoveryActions(List<ProjectContextExclusion> excluded)
    {
        bool hasUnauthorized = false;
        bool hasOther = false;
        for (int index = 0; index < excluded.Count; index++)
        {
            if (excluded[index].ReferenceState == ReferenceState.Unauthorized)
            {
                hasUnauthorized = true;
            }
            else
            {
                hasOther = true;
            }
        }

        if (hasUnauthorized && hasOther)
        {
            return [AdmissionRecoveryAction.RefreshContext, AdmissionRecoveryAction.ContactAdministrator];
        }

        return hasUnauthorized
            ? [AdmissionRecoveryAction.ContactAdministrator]
            : [AdmissionRecoveryAction.RefreshContext];
    }

    private static IReadOnlyList<AdmissionComponent> BuildComponents(
        bool folderIncluded,
        bool setupCurrent,
        bool authorizationCurrent,
        bool referencesComplete)
        =>
        [
            new AdmissionComponent("Project", true, EvidenceFreshnessState.Current, "current"),
            new AdmissionComponent(
                "Folder",
                folderIncluded,
                folderIncluded ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable,
                folderIncluded ? "current" : "missing"),
            new AdmissionComponent(
                "Setup",
                setupCurrent,
                setupCurrent ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable,
                setupCurrent ? "current" : "unavailable"),
            new AdmissionComponent(
                "FirstResponseAuthorization",
                authorizationCurrent,
                authorizationCurrent ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Stale,
                authorizationCurrent ? "envelope-authorized" : "stale"),
            new AdmissionComponent(
                "References",
                referencesComplete,
                referencesComplete ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable,
                referencesComplete ? "current" : "optional-omission"),
        ];

    private static List<ProjectContextReference> SortRefs(List<ProjectContextReference> refs)
        => [.. refs
            .OrderBy(static item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static item => item.ReferenceId, StringComparer.Ordinal)];

    private static List<ProjectContextExclusion> SortExclusions(List<ProjectContextExclusion> exclusions)
        => [.. exclusions
            .OrderBy(static item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static item => item.ReferenceId, StringComparer.Ordinal)];

    private static List<ProjectContextEvaluation> SortEvaluations(List<ProjectContextEvaluation> evaluations)
        => [.. evaluations
            .OrderBy(static item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static item => item.ReferenceId, StringComparer.Ordinal)];

    private static string? MapSourceKind(ProjectContextSourceKind kind)
        => kind switch
        {
            ProjectContextSourceKind.FileReference => FileKind,
            ProjectContextSourceKind.Memory => MemoryKind,
            ProjectContextSourceKind.Conversation => ConversationKind,
            ProjectContextSourceKind.ProjectFolder => FolderKind,
            _ => null,
        };

    private static bool HasDuplicates(IEnumerable<string> identities)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string identity in identities)
        {
            if (string.IsNullOrWhiteSpace(identity) || !seen.Add(identity))
            {
                return true;
            }
        }

        return false;
    }
}
