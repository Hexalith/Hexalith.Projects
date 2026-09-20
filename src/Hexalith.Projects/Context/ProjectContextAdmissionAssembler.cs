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
                projectCurrent: true,
                folderIncluded: context.ProjectFolder is { ReferenceState: ReferenceState.Included },
                setupCurrent: true,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        if (IsRequiredEvidenceStale(tenantAccess, context.Freshness))
        {
            return Unavailable(
                context.ProjectId,
                context.Lifecycle,
                asOf,
                projectVersion,
                projectCurrent: true,
                folderIncluded: context.ProjectFolder is not null,
                setupCurrent: true,
                authorizationCurrent: false,
                ProjectContextUnavailableCause.MissingOrStaleRequiredContext);
        }

        if (HasDisclosureUnsafeDenial(assembled.Evaluations))
        {
            return Unavailable(
                context.ProjectId,
                context.Lifecycle,
                asOf,
                projectVersion,
                projectCurrent: true,
                folderIncluded: context.ProjectFolder is { ReferenceState: ReferenceState.Included },
                setupCurrent: true,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        if (!ownerBackedTrustAvailable && references.Conversations.Count > 0)
        {
            return Unavailable(
                context.ProjectId,
                context.Lifecycle,
                asOf,
                projectVersion,
                projectCurrent: true,
                folderIncluded: context.ProjectFolder is { ReferenceState: ReferenceState.Included },
                setupCurrent: true,
                authorizationCurrent: true,
                ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty);
        }

        List<ProjectContextReference> files = [.. context.FileReferences];
        List<ProjectContextReference> memories = [.. context.MemoryReferences];
        List<ProjectContextReference> conversations = [.. context.Conversations];
        List<ProjectContextExclusion> excluded = [.. context.Excluded];
        List<ProjectContextEvaluation> evaluations = [.. assembled.Evaluations];

        ProjectSetup setup = context.Setup ?? ProjectSetup.Empty;
        ApplyExcludedSourceKinds(setup, files, memories, conversations, excluded, evaluations);

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
                projectCurrent: true,
                folderIncluded: false,
                setupCurrent: false,
                authorizationCurrent: true,
                RequiredReferenceCause(assembled.Evaluations));
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
                    projectCurrent: true,
                    folderIncluded: true,
                    setupCurrent: true,
                    authorizationCurrent: true,
                    referencesUsable: true,
                    referencesReason: hasOptionalOmission ? "optional-omission" : "current"),
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
        => Unavailable(
            projectId,
            lifecycle,
            asOf,
            projectVersion,
            projectCurrent: true,
            folderIncluded,
            setupCurrent,
            authorizationCurrent,
            overflow
                ? ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty
                : ProjectContextUnavailableCause.MissingOrStaleRequiredContext);

    /// <summary>Builds a cause-specific minimal unavailable admission.</summary>
    /// <param name="projectId">The opaque Project identifier.</param>
    /// <param name="lifecycle">The owning Project lifecycle.</param>
    /// <param name="asOf">The authoritative evidence cutoff.</param>
    /// <param name="projectVersion">The authorized persisted Project version.</param>
    /// <param name="projectCurrent">Whether current Project evidence was established.</param>
    /// <param name="folderIncluded">Whether a current authorized Folder was confirmed.</param>
    /// <param name="setupCurrent">Whether Setup evidence is current.</param>
    /// <param name="authorizationCurrent">Whether authorization evidence is current.</param>
    /// <param name="cause">The closed recovery cause.</param>
    /// <returns>An unavailable admission that discloses no candidate identity.</returns>
    public static ProjectContextAdmission Unavailable(
        string projectId,
        ProjectLifecycle lifecycle,
        DateTimeOffset asOf,
        long projectVersion,
        bool projectCurrent,
        bool folderIncluded,
        bool setupCurrent,
        bool authorizationCurrent,
        ProjectContextUnavailableCause cause)
    {
        // Unavailable admissions intentionally strip Folder and Setup payloads. Their component
        // flags must describe the returned evidence rather than evidence observed before stripping.
        _ = folderIncluded;
        _ = setupCurrent;

        return new(
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
                BuildComponents(
                    projectCurrent,
                    folderIncluded: false,
                    setupCurrent: false,
                    authorizationCurrent,
                    referencesUsable: false,
                    referencesReason: ReferencesReason(cause)),
                UnavailableRecoveryActions(cause)));
    }

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

    private static bool HasDisclosureUnsafeDenial(IReadOnlyList<ProjectContextEvaluation> evaluations)
        => evaluations.Any(static evaluation => evaluation.ResultState is
            ReferenceState.Unauthorized or ReferenceState.TenantMismatch);

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
        HashSet<string> selected = new(StringComparer.Ordinal);
        for (int index = 0; index < excluded.Count; index++)
        {
            string? action = excluded[index].ReferenceState switch
            {
                ReferenceState.Pending => AdmissionRecoveryAction.PollTask,
                ReferenceState.Stale or ReferenceState.Unavailable => AdmissionRecoveryAction.RefreshContext,
                ReferenceState.Archived or ReferenceState.Ambiguous => AdmissionRecoveryAction.SelectAlternative,
                ReferenceState.Conflict or ReferenceState.InvalidReference => AdmissionRecoveryAction.ResolveNeedsAttention,
                ReferenceState.Unauthorized or ReferenceState.TenantMismatch => AdmissionRecoveryAction.ContactAdministrator,
                _ => null,
            };
            if (action is not null)
            {
                selected.Add(action);
            }
        }

        string[] ordered = AdmissionRecoveryAction.Values
            .Where(action => selected.Contains(action))
            .ToArray();
        return ordered.Length == 0 ? [AdmissionRecoveryAction.None] : ordered;
    }

    private static IReadOnlyList<string> UnavailableRecoveryActions(ProjectContextUnavailableCause cause)
        => cause switch
        {
            ProjectContextUnavailableCause.StoreFault => [AdmissionRecoveryAction.Retry],
            ProjectContextUnavailableCause.MissingOrStaleRequiredContext => [AdmissionRecoveryAction.RefreshContext],
            ProjectContextUnavailableCause.MaterializationInProgress => [AdmissionRecoveryAction.PollTask],
            ProjectContextUnavailableCause.AlternativeRequired => [AdmissionRecoveryAction.SelectAlternative],
            _ => [AdmissionRecoveryAction.ContactAdministrator],
        };

    private static ProjectContextUnavailableCause RequiredReferenceCause(
        IReadOnlyList<ProjectContextEvaluation> evaluations)
    {
        ProjectContextEvaluation? folder = evaluations.FirstOrDefault(static item =>
            string.Equals(item.ReferenceKind, FolderKind, StringComparison.Ordinal));
        return folder?.ResultState switch
        {
            null => ProjectContextUnavailableCause.MissingOrStaleRequiredContext,
            ReferenceState.Pending => ProjectContextUnavailableCause.MaterializationInProgress,
            ReferenceState.Stale or ReferenceState.Unavailable => ProjectContextUnavailableCause.MissingOrStaleRequiredContext,
            ReferenceState.Archived or ReferenceState.Ambiguous => ProjectContextUnavailableCause.AlternativeRequired,
            _ => ProjectContextUnavailableCause.CorruptionOrAuthorizationUncertainty,
        };
    }

    private static IReadOnlyList<AdmissionComponent> BuildComponents(
        bool projectCurrent,
        bool folderIncluded,
        bool setupCurrent,
        bool authorizationCurrent,
        bool referencesUsable,
        string referencesReason)
        =>
        [
            new AdmissionComponent(
                "Project",
                projectCurrent,
                projectCurrent ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable,
                projectCurrent ? "current" : "unavailable"),
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
                referencesUsable,
                referencesUsable ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable,
                referencesReason),
        ];

    private static string ReferencesReason(ProjectContextUnavailableCause cause)
        => cause switch
        {
            ProjectContextUnavailableCause.StoreFault => "store-fault",
            ProjectContextUnavailableCause.MissingOrStaleRequiredContext => "required-evidence-unavailable",
            ProjectContextUnavailableCause.MaterializationInProgress => "materialization-in-progress",
            ProjectContextUnavailableCause.AlternativeRequired => "alternative-required",
            _ => "corruption-or-authorization-uncertain",
        };

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
