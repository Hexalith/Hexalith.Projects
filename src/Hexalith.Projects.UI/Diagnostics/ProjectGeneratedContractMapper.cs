// <copyright file="ProjectGeneratedContractMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using System.Globalization;

using Hexalith.Projects.Contracts.Models;

using ContractAuditItem = Hexalith.Projects.Contracts.Models.ProjectOperatorAuditTimelineItem;
using ContractContextActivation = Hexalith.Projects.Contracts.Models.ProjectOperatorContextActivation;
using ContractDiagnostic = Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic;
using ContractFreshness = Hexalith.Projects.Contracts.Models.ProjectOperatorFreshnessMetadata;
using ContractReferenceSummary = Hexalith.Projects.Contracts.Models.ProjectOperatorReferenceSummary;
using GeneratedAuditItem = Hexalith.Projects.Client.Generated.ProjectOperatorAuditTimelineItem;
using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;
using GeneratedFreshness = Hexalith.Projects.Client.Generated.FreshnessMetadata;
using GeneratedProject = Hexalith.Projects.Client.Generated.Project;
using GeneratedReferenceSummary = Hexalith.Projects.Client.Generated.ProjectReferenceSummary;
using GeneratedSetup = Hexalith.Projects.Client.Generated.ProjectSetup;

/// <summary>
/// Safe generated-client DTO to contract DTO mapper for Projects UI sources.
/// </summary>
internal static class ProjectGeneratedContractMapper
{
    public static ContractDiagnostic ToContract(GeneratedProject project)
        => new(
            project.ProjectId ?? string.Empty,
            project.Name ?? string.Empty,
            project.Description,
            EnumCode(project.LifecycleState),
            project.CreatedAt,
            project.UpdatedAt,
            project.SetupMetadata,
            ToContract(project.ProjectSetup),
            new ContractContextActivation(
                project.ContextActivation?.Enabled ?? false,
                project.ContextActivation?.BlockedReasonCode),
            project.References.Select(ToContract).ToArray(),
            [],
            ToContract(project.Freshness));

    public static ContractDiagnostic ToContract(GeneratedDiagnostic diagnostic)
        => new(
            diagnostic.ProjectId ?? string.Empty,
            diagnostic.Name ?? string.Empty,
            diagnostic.Description,
            EnumCode(diagnostic.LifecycleState),
            diagnostic.CreatedAt,
            diagnostic.UpdatedAt,
            diagnostic.SetupMetadata,
            ToContract(diagnostic.ProjectSetup),
            new ContractContextActivation(
                diagnostic.ContextActivation?.Enabled ?? false,
                diagnostic.ContextActivation?.BlockedReasonCode),
            diagnostic.References.Select(ToContract).ToArray(),
            diagnostic.AuditTimeline.Select(ToContract).ToArray(),
            ToContract(diagnostic.Freshness));

    public static ContractDiagnostic Merge(ContractDiagnostic detail, ContractDiagnostic? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(detail);
        if (diagnostic is null)
        {
            return detail;
        }

        return detail with
        {
            References = diagnostic.References.Count == 0 ? detail.References : diagnostic.References,
            AuditTimeline = diagnostic.AuditTimeline,
            Freshness = diagnostic.Freshness,
        };
    }

    public static ContractFreshness ToContract(GeneratedFreshness? freshness)
        => freshness is null
            ? new ContractFreshness("eventually_consistent", DateTimeOffset.UnixEpoch, null, false, "trusted")
            : new ContractFreshness(
                EnumCode(freshness.ReadConsistency),
                freshness.ObservedAt,
                freshness.ProjectionWatermark,
                freshness.Stale,
                EnumCode(freshness.TrustState));

    public static string EnumCode<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value.ToString().ToLower(CultureInfo.InvariantCulture);

    private static ContractReferenceSummary ToContract(GeneratedReferenceSummary reference)
        => new(
            EnumCode(reference.ReferenceKind),
            EnumCode(reference.ReferenceState),
            reference.ReferenceId,
            reference.DisplayName,
            reference.ReasonCode,
            ToContract(reference.Freshness));

    private static ContractAuditItem ToContract(GeneratedAuditItem item)
        => new(
            item.AuditEventId ?? string.Empty,
            item.OperationType ?? string.Empty,
            item.OccurredAt,
            item.ActorPrincipalId ?? string.Empty,
            item.CorrelationId ?? string.Empty,
            item.TaskId ?? string.Empty,
            item.ReferenceKind is null ? null : EnumCode(item.ReferenceKind.Value),
            item.ReferenceId,
            item.PreviousState,
            item.NewState,
            item.ReasonCode,
            item.ConversationId,
            item.SourceProjectId,
            item.ProjectionSequence);

    private static ProjectSetup? ToContract(GeneratedSetup? setup)
        => setup is null
            ? null
            : new ProjectSetup(
                setup.Goals.ToArray(),
                setup.UserInstructions.ToArray(),
                setup.PreferredSourceKinds.Select(ToContract).ToArray(),
                setup.ExcludedSourceKinds.Select(ToContract).ToArray(),
                setup.ConversationStartDefaults is null
                    ? null
                    : new ConversationStartDefaults(ToContract(setup.ConversationStartDefaults.LinkedSourcePolicy)));

    private static ProjectContextSourceKind ToContract(Hexalith.Projects.Client.Generated.ProjectContextSourceKind sourceKind)
        => Enum.TryParse(sourceKind.ToString(), ignoreCase: true, out ProjectContextSourceKind value)
            ? value
            : ProjectContextSourceKind.Conversation;

    private static LinkedSourcePolicy ToContract(Hexalith.Projects.Client.Generated.LinkedSourcePolicy policy)
        => Enum.TryParse(policy.ToString(), ignoreCase: true, out LinkedSourcePolicy value)
            ? value
            : LinkedSourcePolicy.None;
}
