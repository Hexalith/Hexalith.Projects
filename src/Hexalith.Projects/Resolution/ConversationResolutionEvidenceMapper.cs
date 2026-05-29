// <copyright file="ConversationResolutionEvidenceMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Pure, deterministic mapper that turns the safe metadata of a single conversation plus the
/// tenant-authorized candidate Projects into <see cref="ProjectResolutionCandidateEvidence"/> for the
/// Story 4.1 <see cref="ProjectResolutionEngine"/> (Story 4.2 host composition). It never scores,
/// ranks, or decides an outcome — that is the engine's exclusive responsibility. It only translates
/// pre-fetched evidence into the engine's input shape.
/// </summary>
/// <remarks>
/// <para>
/// For conversation resolution only two reason codes are ever produced — exactly as Story 4.2
/// requires:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="ProjectReasonCode.ConversationLinked"/> when the conversation metadata
/// records or hints this candidate project (explicit assignment or response-scoped hydration).</description></item>
/// <item><description><see cref="ProjectReasonCode.MetadataMatched"/> when the conversation's safe label
/// matches the candidate's safe display name under a deterministic, metadata-only comparison
/// (trim + ordinal-ignore-case equality).</description></item>
/// </list>
/// <para>
/// Every signal carries the conversation read's <see cref="ConversationResolutionMetadata.ReferenceState"/>:
/// only <see cref="ReferenceState.Included"/> contributes to a positive match; any degraded state
/// (Stale / Unavailable / Unauthorized / …) is surfaced by the engine as an exclusion rather than a
/// match. Candidates that produce no signal are omitted entirely (they are not near-misses). Folder /
/// file / memory reason codes are deliberately never produced here — those belong to Story 4.3.
/// </para>
/// </remarks>
public static class ConversationResolutionEvidenceMapper
{
    private const string ConversationReferenceKind = "conversation";
    private const string MetadataReferenceKind = "metadata";

    /// <summary>
    /// Maps the conversation metadata and candidate Projects to engine evidence.
    /// </summary>
    /// <param name="conversation">The safe single-conversation metadata read through the Pattern-A ACL.</param>
    /// <param name="candidates">The tenant-authorized candidate Projects (already tenant-scoped by the host).</param>
    /// <param name="now">The deterministic observation instant stamped onto every emitted signal.</param>
    /// <returns>One <see cref="ProjectResolutionCandidateEvidence"/> per candidate that produced at least one match signal.</returns>
    public static IReadOnlyList<ProjectResolutionCandidateEvidence> Map(
        ConversationResolutionMetadata conversation,
        IReadOnlyList<ConversationResolutionProjectCandidate> candidates,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(candidates);

        List<ProjectResolutionCandidateEvidence> evidence = new(candidates.Count);
        foreach (ConversationResolutionProjectCandidate candidate in candidates)
        {
            if (candidate is null)
            {
                continue;
            }

            List<ProjectResolutionMatchSignal> signals = [];

            if (IsLinked(conversation.LinkedProjectId, candidate.ProjectId))
            {
                signals.Add(new ProjectResolutionMatchSignal(
                    ConversationReferenceKind,
                    conversation.ConversationId,
                    ProjectReasonCode.ConversationLinked,
                    conversation.ReferenceState,
                    now));
            }

            if (IsMetadataMatch(conversation.SafeLabel, candidate.DisplayName))
            {
                signals.Add(new ProjectResolutionMatchSignal(
                    MetadataReferenceKind,
                    conversation.ConversationId,
                    ProjectReasonCode.MetadataMatched,
                    conversation.ReferenceState,
                    now));
            }

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

    private static bool IsLinked(string? linkedProjectId, string candidateProjectId)
        => !string.IsNullOrWhiteSpace(linkedProjectId)
            && string.Equals(linkedProjectId, candidateProjectId, StringComparison.Ordinal);

    private static bool IsMetadataMatch(string? safeLabel, string? candidateDisplayName)
        => !string.IsNullOrWhiteSpace(safeLabel)
            && !string.IsNullOrWhiteSpace(candidateDisplayName)
            && string.Equals(safeLabel.Trim(), candidateDisplayName.Trim(), StringComparison.OrdinalIgnoreCase);
}
