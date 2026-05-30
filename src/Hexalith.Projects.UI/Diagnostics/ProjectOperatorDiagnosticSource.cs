// <copyright file="ProjectOperatorDiagnosticSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using System.Globalization;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.UI.Rendering;

using ContractAuditItem = Hexalith.Projects.Contracts.Models.ProjectOperatorAuditTimelineItem;
using ContractContextActivation = Hexalith.Projects.Contracts.Models.ProjectOperatorContextActivation;
using ContractDiagnostic = Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic;
using ContractFreshness = Hexalith.Projects.Contracts.Models.ProjectOperatorFreshnessMetadata;
using ContractReferenceSummary = Hexalith.Projects.Contracts.Models.ProjectOperatorReferenceSummary;

using GeneratedAuditItem = Hexalith.Projects.Client.Generated.ProjectOperatorAuditTimelineItem;
using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;
using GeneratedFreshness = Hexalith.Projects.Client.Generated.FreshnessMetadata;
using GeneratedReferenceSummary = Hexalith.Projects.Client.Generated.ProjectReferenceSummary;

/// <summary>
/// Generated-client backed source for project-scoped operator diagnostics.
/// </summary>
public sealed class ProjectOperatorDiagnosticSource(IClient client) : IProjectOperatorDiagnosticSource
{
    /// <inheritdoc />
    public async Task<ProjectDiagnosticLoadResult> GetProjectDiagnosticsAsync(
        string projectId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        string correlationId = Guid.NewGuid().ToString("N");
        try
        {
            GeneratedDiagnostic diagnostic = await client.GetProjectOperatorDiagnosticsAsync(
                projectId,
                auditLimit: 25,
                x_Correlation_Id: correlationId,
                x_Hexalith_Freshness: ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);

            return ProjectDiagnosticLoadResult.FromDiagnostic(
                ToContract(diagnostic),
                "server-derived tenant",
                ProjectConsoleModes.ReadOnly);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 404)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Warning("data_unavailable", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("validation_error", correlationId));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("diagnostic_query_failed", correlationId));
        }
    }

    private static ContractDiagnostic ToContract(GeneratedDiagnostic diagnostic)
        => new(
            diagnostic.ProjectId ?? string.Empty,
            diagnostic.Name ?? string.Empty,
            diagnostic.Description,
            EnumCode(diagnostic.LifecycleState),
            diagnostic.CreatedAt,
            diagnostic.UpdatedAt,
            diagnostic.SetupMetadata,
            null,
            new ContractContextActivation(
                diagnostic.ContextActivation?.Enabled ?? false,
                diagnostic.ContextActivation?.BlockedReasonCode),
            diagnostic.References.Select(ToContract).ToArray(),
            diagnostic.AuditTimeline.Select(ToContract).ToArray(),
            ToContract(diagnostic.Freshness));

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

    private static ContractFreshness ToContract(GeneratedFreshness? freshness)
        => freshness is null
            ? new ContractFreshness("eventually_consistent", DateTimeOffset.UnixEpoch, null, false, "trusted")
            : new ContractFreshness(
                EnumCode(freshness.ReadConsistency),
                freshness.ObservedAt,
                freshness.ProjectionWatermark,
                freshness.Stale,
                EnumCode(freshness.TrustState));

    private static string EnumCode<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value.ToString().ToLower(CultureInfo.InvariantCulture);
}
