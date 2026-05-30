// <copyright file="ProjectsMcpModels.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

using System.ComponentModel.DataAnnotations;

/// <summary>Safe MCP inventory row.</summary>
public sealed record ProjectsMcpInventoryItem(
    string ProjectId,
    string Name,
    string LifecycleState,
    DateTimeOffset UpdatedAt,
    string FreshnessTrustState,
    string? ProjectionWatermark,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP project detail row.</summary>
public sealed record ProjectsMcpProjectDetailItem(
    string ProjectId,
    string Name,
    string? Description,
    string LifecycleState,
    DateTimeOffset UpdatedAt,
    int ReferenceCount,
    string FreshnessTrustState,
    string? ProjectionWatermark,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP operator diagnostic row.</summary>
public sealed record ProjectsMcpOperatorDiagnosticItem(
    string ProjectId,
    string Name,
    string LifecycleState,
    int ReferenceCount,
    int AuditRowCount,
    string FreshnessTrustState,
    string? ProjectionWatermark,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP reference health row.</summary>
public sealed record ProjectsMcpReferenceHealthItem(
    string ProjectId,
    string ReferenceKind,
    string? ReferenceId,
    string ReferenceState,
    string? ReasonCode,
    DateTimeOffset LastCheckedAt,
    string FreshnessTrustState,
    string? ProjectionWatermark,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP resolution trace row.</summary>
public sealed record ProjectsMcpResolutionTraceItem(
    string Mode,
    string? ProjectId,
    string ResultState,
    int CandidateCount,
    int ExclusionCount,
    DateTimeOffset ObservedAt,
    string? CorrelationId,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP audit timeline row.</summary>
public sealed record ProjectsMcpAuditTimelineItem(
    string ProjectId,
    string AuditEventId,
    string OperationType,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string TaskId,
    string? ReferenceKind,
    string? ReferenceId,
    string? ReasonCode,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP diagnostic export summary.</summary>
public sealed record ProjectsMcpSafeDiagnosticExportItem(
    string ProjectId,
    string ContractVersion,
    int ReferenceHealthRowCount,
    int AuditRowCount,
    string FreshnessTrustState,
    string? ProjectionWatermark,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP warning queue row.</summary>
public sealed record ProjectsMcpWarningQueueItem(
    string ProjectId,
    string ProjectName,
    string LifecycleState,
    string ReferenceKind,
    string? ReferenceId,
    string ReferenceState,
    string? ReasonCode,
    DateTimeOffset LastObservedAt,
    string FreshnessTrustState,
    string? ProjectionWatermark,
    int DiagnosticUnavailable,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP operational dashboard counters.</summary>
public sealed record ProjectsMcpOperationalDashboardItem(
    int TotalVisibleProjects,
    int ActiveProjects,
    int ArchivedProjects,
    int ProjectsWithWarnings,
    int DiagnosticUnavailable,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>Safe MCP maintenance action preview descriptor.</summary>
public sealed record ProjectsMcpMaintenanceActionItem(
    string Action,
    string LifecycleWireStates,
    string WebLifecycleLabels,
    bool RequiresProjectId,
    bool RequiresConfirmation,
    bool RequiresDryRunEvidence,
    bool RequiresIdempotencyKey,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);

/// <summary>MCP maintenance command shape consumed by FrontComposer MCP command invocation.</summary>
public sealed class ProjectsMcpMaintenanceCommand
{
    /// <summary>Gets or sets the action name.</summary>
    [Required]
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the target Project identifier.</summary>
    [Required]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional reference kind.</summary>
    public string? ReferenceKind { get; set; }

    /// <summary>Gets or sets the optional reference identifier.</summary>
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets the optional safe reference display label.</summary>
    public string? ReferenceDisplayLabel { get; set; }

    /// <summary>Gets or sets a value indicating whether replacement was confirmed.</summary>
    public bool ReplacementConfirmed { get; set; }

    /// <summary>Gets or sets a transient folder identifier used only for file ACL validation.</summary>
    public string? TransientFolderId { get; set; }

    /// <summary>Gets or sets a transient workspace identifier used only for file ACL validation.</summary>
    public string? TransientWorkspaceId { get; set; }

    /// <summary>Gets or sets a transient file path used only for file ACL validation.</summary>
    public string? TransientFilePath { get; set; }

    /// <summary>Gets or sets a value indicating whether the caller confirmed the state change.</summary>
    public bool Confirmed { get; set; }

    /// <summary>Gets or sets the caller-supplied dry-run evidence token.</summary>
    [Required]
    public string DryRunEvidence { get; set; } = string.Empty;

    /// <summary>Gets or sets the caller-supplied idempotency key. It is never returned in MCP output.</summary>
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-derived command identifier populated by FrontComposer MCP.</summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-derived correlation identifier populated by FrontComposer MCP.</summary>
    public string CorrelationId { get; set; } = string.Empty;
}
