// <copyright file="AttachmentResolutionEvidenceMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure, deterministic mapper that turns presented attachment references plus reverse-index candidate
/// rows into <see cref="ProjectResolutionCandidateEvidence"/> for the Story 4.1 engine.
/// </summary>
/// <remarks>
/// The mapper never scores, ranks, or decides an outcome. It emits only
/// <see cref="ProjectReasonCode.ProjectFolderMatched"/> for <c>folder</c> references and
/// <see cref="ProjectReasonCode.FileReferenceMatched"/> for <c>file</c> references. Every signal
/// carries the cached <see cref="ReferenceState"/> from the project reference-index row; only
/// <see cref="ReferenceState.Included"/> can qualify in the engine.
/// </remarks>
public static class AttachmentResolutionEvidenceMapper
{
    /// <summary>Gets the folder reference kind used by the reference index and engine signals.</summary>
    public const string FolderReferenceKind = "folder";

    /// <summary>Gets the file reference kind used by the reference index and engine signals.</summary>
    public const string FileReferenceKind = "file";

    /// <summary>Maps attachment metadata and candidate Projects to engine evidence.</summary>
    /// <param name="attachments">The metadata-only presented attachment references.</param>
    /// <param name="candidates">Tenant-authorized candidate Projects from the reverse reference index.</param>
    /// <param name="now">The deterministic observation instant stamped onto every emitted signal.</param>
    /// <returns>One candidate evidence row per Project that produced at least one match signal.</returns>
    public static IReadOnlyList<ProjectResolutionCandidateEvidence> Map(
        AttachmentResolutionMetadata attachments,
        IReadOnlyList<AttachmentResolutionProjectCandidate> candidates,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(candidates);

        HashSet<string> presentedFolders = PresentedIds(attachments.FolderReferences, FolderReferenceKind);
        HashSet<string> presentedFiles = PresentedIds(attachments.FileReferences, FileReferenceKind);

        List<ProjectResolutionCandidateEvidence> evidence = new(candidates.Count);
        foreach (AttachmentResolutionProjectCandidate candidate in candidates)
        {
            if (candidate is null)
            {
                continue;
            }

            List<ProjectResolutionMatchSignal> signals = [];
            AddSignals(
                signals,
                candidate.FolderReferences,
                presentedFolders,
                FolderReferenceKind,
                ProjectReasonCode.ProjectFolderMatched,
                now);
            AddSignals(
                signals,
                candidate.FileReferences,
                presentedFiles,
                FileReferenceKind,
                ProjectReasonCode.FileReferenceMatched,
                now);

            if (signals.Count == 0)
            {
                continue;
            }

            evidence.Add(new ProjectResolutionCandidateEvidence(
                candidate.ProjectId,
                candidate.DisplayName,
                candidate.Lifecycle,
                signals));
        }

        return evidence;
    }

    private static HashSet<string> PresentedIds(
        IEnumerable<AttachmentResolutionReference> references,
        string referenceKind)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (AttachmentResolutionReference reference in references)
        {
            if (string.Equals(reference.ReferenceKind, referenceKind, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(reference.ReferenceId))
            {
                ids.Add(reference.ReferenceId);
            }
        }

        return ids;
    }

    private static void AddSignals(
        List<ProjectResolutionMatchSignal> signals,
        IEnumerable<AttachmentResolutionReference> references,
        HashSet<string> presentedIds,
        string referenceKind,
        ProjectReasonCode reasonCode,
        DateTimeOffset now)
    {
        foreach (AttachmentResolutionReference reference in references)
        {
            // Defense-in-depth re-check: candidate references are already filtered to the presented
            // attachment ids by the reverse read model (ListByReference). This guard keeps the mapper
            // correct in isolation (and under direct Tier-1 testing) so a future enumeration change can
            // never silently emit a signal for an id the caller did not present.
            if (!string.Equals(reference.ReferenceKind, referenceKind, StringComparison.Ordinal)
                || !presentedIds.Contains(reference.ReferenceId))
            {
                continue;
            }

            signals.Add(new ProjectResolutionMatchSignal(
                referenceKind,
                reference.ReferenceId,
                reasonCode,
                reference.ReferenceState,
                now));
        }
    }
}
