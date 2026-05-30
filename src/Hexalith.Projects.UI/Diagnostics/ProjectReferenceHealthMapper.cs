// <copyright file="ProjectReferenceHealthMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using ContractDiagnostic = Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic;
using ContractReferenceSummary = Hexalith.Projects.Contracts.Models.ProjectOperatorReferenceSummary;
using ContractFreshness = Hexalith.Projects.Contracts.Models.ProjectOperatorFreshnessMetadata;
using ContractInclusionCheck = Hexalith.Projects.Contracts.Ui.ProjectContextInclusionCheck;
using GeneratedContext = Hexalith.Projects.Client.Generated.ProjectContext;
using GeneratedContextEvaluation = Hexalith.Projects.Client.Generated.ProjectContextEvaluation;
using GeneratedContextExplanation = Hexalith.Projects.Client.Generated.ProjectContextExplanation;

/// <summary>
/// Builds the metadata-only reference health matrix from existing detail, context explanation, and
/// conversation ACL query shapes.
/// </summary>
internal static class ProjectReferenceHealthMapper
{
    private const string ConversationKind = "conversation";

    public static IReadOnlyList<ProjectReferenceHealthRowProjection> BuildRows(
        ContractDiagnostic detail,
        GeneratedContextExplanation? explanation,
        ProjectConversationsPage? conversations)
    {
        ArgumentNullException.ThrowIfNull(detail);

        var rows = new Dictionary<string, ProjectReferenceHealthRowProjection>(StringComparer.Ordinal);
        foreach (ContractReferenceSummary reference in detail.References)
        {
            Upsert(rows, ProjectReferenceHealthRowProjection.FromReferenceSummary(detail.ProjectId, reference));
        }

        if (explanation?.Evaluations is not null)
        {
            foreach (GeneratedContextEvaluation evaluation in explanation.Evaluations)
            {
                Upsert(rows, FromEvaluation(detail, explanation.Context, evaluation));
            }
        }

        if (conversations?.Items is not null)
        {
            foreach (ProjectConversationItem conversation in conversations.Items)
            {
                Upsert(rows, FromConversation(detail, conversation));
            }
        }

        return rows.Values
            .OrderBy(row => row.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(row => row.ReferenceId, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProjectReferenceHealthRowProjection FromEvaluation(
        ContractDiagnostic detail,
        GeneratedContext context,
        GeneratedContextEvaluation evaluation)
    {
        string referenceKind = ProjectReferenceHealthRowProjection.NormalizeCode(evaluation.ReferenceKind.ToString());
        string referenceId = evaluation.ReferenceId ?? string.Empty;
        ReferenceState state = ProjectReferenceHealthRowProjection.ParseEnum(
            evaluation.ResultState.ToString(),
            ReferenceState.Unavailable);

        return new ProjectReferenceHealthRowProjection
        {
            Id = ProjectReferenceHealthRowProjection.BuildId(detail.ProjectId, referenceKind, referenceId),
            ProjectId = detail.ProjectId,
            ReferenceKind = referenceKind,
            OwnerContext = ProjectReferenceHealthRowProjection.OwnerContextFor(referenceKind),
            ReferenceId = referenceId,
            InclusionState = state,
            HealthState = state,
            ReasonCode = ProjectReferenceHealthRowProjection.ParseNullableEnum<ProjectReasonCode>(
                evaluation.ReasonCode?.ToString()),
            InclusionCheck = ProjectReferenceHealthRowProjection.ParseNullableEnum<ContractInclusionCheck>(
                evaluation.FailedCheck?.ToString()),
            DiagnosticCode = ProjectContextInclusionDiagnostic.IsKnown(evaluation.Diagnostic)
                ? evaluation.Diagnostic
                : null,
            LastCheckedAt = evaluation.ObservedAt,
            FreshnessTrustState = context.Freshness.ToString().ToLowerInvariant(),
        };
    }

    private static ProjectReferenceHealthRowProjection FromConversation(
        ContractDiagnostic detail,
        ProjectConversationItem conversation)
    {
        string referenceId = conversation.ConversationId ?? string.Empty;
        (ReferenceState State, ContractInclusionCheck? Check, string? Diagnostic) = MapTrustSignal(conversation.TrustSignal);
        ContractFreshness freshness = detail.Freshness;

        return new ProjectReferenceHealthRowProjection
        {
            Id = ProjectReferenceHealthRowProjection.BuildId(detail.ProjectId, ConversationKind, referenceId),
            ProjectId = detail.ProjectId,
            ReferenceKind = ConversationKind,
            OwnerContext = ProjectReferenceHealthRowProjection.OwnerContextFor(ConversationKind),
            ReferenceId = referenceId,
            DisplayLabel = conversation.DisplayLabel,
            InclusionState = conversation.TrustSignal == ProjectConversationTrustSignal.Current
                ? ReferenceState.Included
                : State,
            HealthState = State,
            ReasonCode = conversation.TrustSignal == ProjectConversationTrustSignal.Current
                ? ProjectReasonCode.ConversationLinked
                : null,
            InclusionCheck = Check,
            DiagnosticCode = Diagnostic,
            LastCheckedAt = freshness.ObservedAt,
            FreshnessTrustState = conversation.TrustSignal.ToString().ToLowerInvariant(),
            ProjectionWatermark = freshness.ProjectionWatermark,
        };
    }

    private static void Upsert(
        Dictionary<string, ProjectReferenceHealthRowProjection> rows,
        ProjectReferenceHealthRowProjection incoming)
    {
        if (rows.TryGetValue(incoming.Id, out ProjectReferenceHealthRowProjection? existing))
        {
            rows[incoming.Id] = Merge(existing, incoming);
            return;
        }

        rows[incoming.Id] = incoming;
    }

    private static ProjectReferenceHealthRowProjection Merge(
        ProjectReferenceHealthRowProjection existing,
        ProjectReferenceHealthRowProjection incoming)
        => new()
        {
            Id = existing.Id,
            ProjectId = existing.ProjectId,
            ReferenceKind = existing.ReferenceKind,
            OwnerContext = existing.OwnerContext,
            ReferenceId = existing.ReferenceId,
            DisplayLabel = string.IsNullOrWhiteSpace(incoming.DisplayLabel) ? existing.DisplayLabel : incoming.DisplayLabel,
            InclusionState = incoming.InclusionState,
            HealthState = incoming.HealthState,
            ReasonCode = incoming.ReasonCode ?? existing.ReasonCode,
            InclusionCheck = incoming.InclusionCheck ?? existing.InclusionCheck,
            DiagnosticCode = incoming.DiagnosticCode ?? existing.DiagnosticCode,
            LastCheckedAt = incoming.LastCheckedAt == default ? existing.LastCheckedAt : incoming.LastCheckedAt,
            FreshnessTrustState = string.IsNullOrWhiteSpace(incoming.FreshnessTrustState)
                ? existing.FreshnessTrustState
                : incoming.FreshnessTrustState,
            ProjectionWatermark = incoming.ProjectionWatermark ?? existing.ProjectionWatermark,
            SafeActionAvailabilityLabel = string.IsNullOrWhiteSpace(incoming.SafeActionAvailabilityLabel)
                ? existing.SafeActionAvailabilityLabel
                : incoming.SafeActionAvailabilityLabel,
        };

    private static (ReferenceState State, ContractInclusionCheck? Check, string? Diagnostic) MapTrustSignal(
        ProjectConversationTrustSignal trustSignal)
        => trustSignal switch
        {
            ProjectConversationTrustSignal.Current => (ReferenceState.Included, null, null),
            ProjectConversationTrustSignal.Stale => (
                ReferenceState.Stale,
                ContractInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceStale),
            ProjectConversationTrustSignal.MixedGeneration => (
                ReferenceState.Stale,
                ContractInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceStale),
            ProjectConversationTrustSignal.Rebuilding => (
                ReferenceState.Unavailable,
                ContractInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceUnavailable),
            ProjectConversationTrustSignal.Unavailable => (
                ReferenceState.Unavailable,
                ContractInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceUnavailable),
            ProjectConversationTrustSignal.Forbidden => (
                ReferenceState.Unauthorized,
                ContractInclusionCheck.ReferenceAuthorization,
                ProjectContextInclusionDiagnostic.ReferenceUnauthorized),
            ProjectConversationTrustSignal.Redacted => (
                ReferenceState.Excluded,
                ContractInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceRedacted),
            _ => (
                ReferenceState.Unavailable,
                ContractInclusionCheck.ReferenceFreshness,
                ProjectContextInclusionDiagnostic.ReferenceUnavailable),
        };
}
