// <copyright file="ProjectSafeDiagnosticExportBuilder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Builds deterministic metadata-only safe diagnostic exports from already-authorized UI context.
/// </summary>
public static class ProjectSafeDiagnosticExportBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    // This list MUST enumerate every leaf field emitted by BuildJson in serialization order so CLI/MCP
    // parity (AC8) and the included-field guarantee (AC6) stay accurate. ProjectAuditExportProjectionTests
    // walks the serialized document and asserts this set matches the emitted leaves exactly.
    private static readonly string[] IncludedFields =
    [
        "schemaVersion",
        "generatedAt",
        "project.projectId",
        "project.projectName",
        "project.lifecycleState",
        "project.tenantScopeLabel",
        "project.setupPreferenceSummary.goalsCount",
        "project.setupPreferenceSummary.userInstructionsCount",
        "project.setupPreferenceSummary.preferredSourceKinds",
        "project.setupPreferenceSummary.excludedSourceKinds",
        "project.setupPreferenceSummary.conversationStartLinkedSourcePolicy",
        "freshness.readConsistency",
        "freshness.observedAt",
        "freshness.projectionWatermark",
        "freshness.stale",
        "freshness.trustState",
        "referenceHealthRows.referenceKind",
        "referenceHealthRows.referenceId",
        "referenceHealthRows.ownerContext",
        "referenceHealthRows.displayLabel",
        "referenceHealthRows.inclusionState",
        "referenceHealthRows.healthState",
        "referenceHealthRows.reasonCode",
        "referenceHealthRows.inclusionCheck",
        "referenceHealthRows.diagnosticCode",
        "referenceHealthRows.lastCheckedAt",
        "referenceHealthRows.freshnessTrustState",
        "referenceHealthRows.projectionWatermark",
        "auditRows.auditEventId",
        "auditRows.operationType",
        "auditRows.occurredAt",
        "auditRows.actorPrincipalId",
        "auditRows.correlationId",
        "auditRows.taskId",
        "auditRows.referenceKind",
        "auditRows.referenceId",
        "auditRows.previousState",
        "auditRows.newState",
        "auditRows.reasonCode",
        "auditRows.conversationId",
        "auditRows.sourceProjectId",
        "auditRows.projectionSequence",
        "safeFeedbackCodes",
        "includedFieldNames",
        "excludedPayloadCategories",
        "payloadExclusionGuarantee",
    ];

    private static readonly string[] ExcludedCategories =
    [
        "conversation-text",
        "file-data",
        "memory-data",
        "raw-instruction-material",
        "credential-material",
        "request-material",
        "proposal-material",
        "dedupe-material",
        "resolution-metrics",
        "denial-detail-material",
        "local-machine-locators",
    ];

    /// <summary>Builds deterministic compact JSON for Web copy/download.</summary>
    public static string BuildJson(ProjectDetailLoadResult result, DateTimeOffset generatedAt)
        => JsonSerializer.Serialize(BuildDocument(result, generatedAt), SerializerOptions);

    /// <summary>Builds the FrontComposer-safe export descriptor projection.</summary>
    public static ProjectSafeDiagnosticExportProjection BuildProjection(
        ProjectDetailLoadResult result,
        DateTimeOffset generatedAt)
    {
        ProjectSafeDiagnosticExportDocument document = BuildDocument(result, generatedAt);
        return new ProjectSafeDiagnosticExportProjection
        {
            GeneratedAt = document.GeneratedAt,
            ProjectId = document.Project.ProjectId,
            ProjectName = document.Project.ProjectName,
            LifecycleState = document.Project.LifecycleState,
            TenantScopeLabel = document.Project.TenantScopeLabel,
            FreshnessTrustState = document.Freshness.TrustState,
            ProjectionWatermark = document.Freshness.ProjectionWatermark,
            ReferenceHealthRowCount = document.ReferenceHealthRows.Count,
            AuditRowCount = document.AuditRows.Count,
            SafeFeedbackCodes = string.Join(", ", document.SafeFeedbackCodes),
            IncludedFieldNames = string.Join(", ", document.IncludedFieldNames),
            ExcludedPayloadCategories = string.Join(", ", document.ExcludedPayloadCategories),
            PayloadExclusionGuarantee = document.PayloadExclusionGuarantee,
        };
    }

    /// <summary>Builds the structured export document for tests and download serialization.</summary>
    public static ProjectSafeDiagnosticExportDocument BuildDocument(
        ProjectDetailLoadResult result,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(result);
        ProjectOperatorDiagnostic detail = result.Detail
            ?? throw new InvalidOperationException("Safe diagnostic export requires an authorized Project detail context.");

        return new ProjectSafeDiagnosticExportDocument(
            ProjectSafeDiagnosticExportProjection.ContractVersionValue,
            generatedAt,
            new ProjectSafeDiagnosticExportProject(
                detail.ProjectId,
                detail.Name,
                detail.LifecycleState,
                result.TenantScope,
                ProjectSafeDiagnosticExportSetupSummary.FromSetup(detail.ProjectSetup)),
            ProjectSafeDiagnosticExportFreshness.FromFreshness(detail.Freshness),
            result.ReferenceHealthRows.Select(ProjectSafeDiagnosticExportReferenceRow.FromRow).ToArray(),
            detail.AuditTimeline.Select(ProjectSafeDiagnosticExportAuditRow.FromItem).ToArray(),
            FeedbackCodes(result).ToArray(),
            IncludedFields,
            ExcludedCategories,
            ProjectSafeDiagnosticExportProjection.PayloadExclusionGuaranteeText);
    }

    private static IEnumerable<string> FeedbackCodes(ProjectDetailLoadResult result)
    {
        if (result.Feedback is not null)
        {
            yield return result.Feedback.SafeReasonCode;
        }

        if (result.DiagnosticFeedback is not null)
        {
            yield return result.DiagnosticFeedback.SafeReasonCode;
        }
    }
}

/// <summary>Structured safe diagnostic export document.</summary>
public sealed record ProjectSafeDiagnosticExportDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    ProjectSafeDiagnosticExportProject Project,
    ProjectSafeDiagnosticExportFreshness Freshness,
    IReadOnlyList<ProjectSafeDiagnosticExportReferenceRow> ReferenceHealthRows,
    IReadOnlyList<ProjectSafeDiagnosticExportAuditRow> AuditRows,
    IReadOnlyList<string> SafeFeedbackCodes,
    IReadOnlyList<string> IncludedFieldNames,
    IReadOnlyList<string> ExcludedPayloadCategories,
    string PayloadExclusionGuarantee);

/// <summary>Safe project header metadata for diagnostic export.</summary>
public sealed record ProjectSafeDiagnosticExportProject(
    string ProjectId,
    string ProjectName,
    string LifecycleState,
    string TenantScopeLabel,
    ProjectSafeDiagnosticExportSetupSummary SetupPreferenceSummary);

/// <summary>Bounded setup preference counts and enum metadata only.</summary>
public sealed record ProjectSafeDiagnosticExportSetupSummary(
    int GoalsCount,
    int UserInstructionsCount,
    IReadOnlyList<string> PreferredSourceKinds,
    IReadOnlyList<string> ExcludedSourceKinds,
    string? ConversationStartLinkedSourcePolicy)
{
    /// <summary>Builds the setup summary without exporting free-text setup values.</summary>
    public static ProjectSafeDiagnosticExportSetupSummary FromSetup(ProjectSetup? setup)
        => setup is null
            ? new ProjectSafeDiagnosticExportSetupSummary(0, 0, [], [], null)
            : new ProjectSafeDiagnosticExportSetupSummary(
                setup.Goals.Count,
                setup.UserInstructions.Count,
                setup.PreferredSourceKinds.Select(source => source.ToString()).ToArray(),
                setup.ExcludedSourceKinds.Select(source => source.ToString()).ToArray(),
                setup.ConversationStartDefaults?.LinkedSourcePolicy.ToString());
}

/// <summary>Safe freshness metadata for diagnostic export.</summary>
public sealed record ProjectSafeDiagnosticExportFreshness(
    string ReadConsistency,
    DateTimeOffset ObservedAt,
    string? ProjectionWatermark,
    bool Stale,
    string TrustState)
{
    /// <summary>Builds export freshness from operator diagnostic metadata.</summary>
    public static ProjectSafeDiagnosticExportFreshness FromFreshness(ProjectOperatorFreshnessMetadata freshness)
    {
        ArgumentNullException.ThrowIfNull(freshness);
        return new ProjectSafeDiagnosticExportFreshness(
            freshness.ReadConsistency,
            freshness.ObservedAt,
            freshness.ProjectionWatermark,
            freshness.Stale,
            freshness.TrustState);
    }
}

/// <summary>Safe reference-health row for diagnostic export.</summary>
public sealed record ProjectSafeDiagnosticExportReferenceRow(
    string ReferenceKind,
    string ReferenceId,
    string OwnerContext,
    string? DisplayLabel,
    string InclusionState,
    string HealthState,
    string? ReasonCode,
    string? InclusionCheck,
    string? DiagnosticCode,
    DateTimeOffset LastCheckedAt,
    string FreshnessTrustState,
    string? ProjectionWatermark)
{
    /// <summary>Builds an export row from the UI reference-health projection.</summary>
    public static ProjectSafeDiagnosticExportReferenceRow FromRow(ProjectReferenceHealthRowProjection row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new ProjectSafeDiagnosticExportReferenceRow(
            row.ReferenceKind,
            row.ReferenceId,
            row.OwnerContext,
            row.DisplayLabel,
            row.InclusionState.ToString(),
            row.HealthState.ToString(),
            row.ReasonCode?.ToString(),
            row.InclusionCheck?.ToString(),
            row.DiagnosticCode,
            row.LastCheckedAt,
            row.FreshnessTrustState,
            row.ProjectionWatermark);
    }
}

/// <summary>Safe audit row for diagnostic export.</summary>
public sealed record ProjectSafeDiagnosticExportAuditRow(
    string AuditEventId,
    string OperationType,
    DateTimeOffset OccurredAt,
    string ActorPrincipalId,
    string CorrelationId,
    string TaskId,
    string? ReferenceKind,
    string? ReferenceId,
    string? PreviousState,
    string? NewState,
    string? ReasonCode,
    string? ConversationId,
    string? SourceProjectId,
    long ProjectionSequence)
{
    /// <summary>Builds an export row from the operator diagnostic audit DTO.</summary>
    public static ProjectSafeDiagnosticExportAuditRow FromItem(ProjectOperatorAuditTimelineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new ProjectSafeDiagnosticExportAuditRow(
            item.AuditEventId,
            item.OperationType,
            item.OccurredAt,
            item.ActorPrincipalId,
            item.CorrelationId,
            item.TaskId,
            item.ReferenceKind,
            item.ReferenceId,
            item.PreviousState,
            item.NewState,
            item.ReasonCode,
            item.ConversationId,
            item.SourceProjectId,
            item.ProjectionSequence);
    }
}
