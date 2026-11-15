// <copyright file="ProjectMaintenanceActionProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer descriptor for metadata-only audit-first Project maintenance actions.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = "Preview,DryRunRequired,DryRunPassed,DryRunBlocked,ConfirmationRequired,Executing,Succeeded,Failed")]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project maintenance action", Description = "Metadata-only preview, dry-run, confirmation, command lifecycle, and audit evidence descriptor")]
public partial class ProjectMaintenanceActionProjection
{
    /// <summary>Gets the UI descriptor contract version for Story 5.9 maintenance parity.</summary>
    public const string ContractVersionValue = "projects.maintenance-action.ui.v1";

    /// <summary>Gets the payload-exclusion guarantee rendered by Web and future MCP/CLI descriptors.</summary>
    public const string PayloadExclusionGuaranteeText =
        "Payload-bearing data is excluded from UI, command evidence, audit rows, exports, and parity descriptors.";

    /// <summary>Gets or sets the stable descriptor identity.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the descriptor contract version.</summary>
    [ProjectionFieldGroup("Descriptor")]
    [Display(Name = "Contract version")]
    public string ContractVersion { get; set; } = ContractVersionValue;

    /// <summary>Gets or sets the maintenance action name.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Action")]
    public string Action { get; set; } = ProjectMaintenanceActions.Archive;

    /// <summary>Gets or sets the explicit panel state.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Panel state")]
    public ProjectMaintenancePanelState State { get; set; } = ProjectMaintenancePanelState.Preview;

    /// <summary>Gets or sets the explicit panel state label for cross-surface parity payloads.</summary>
    [ProjectionFieldGroup("State")]
    [Display(Name = "Panel state label")]
    public string PanelState { get; set; } = ProjectMaintenancePanelStates.Preview;

    /// <summary>Gets or sets the command lifecycle state.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Command lifecycle state")]
    public string CommandLifecycleState { get; set; } = ProjectMaintenanceCommandLifecycleStates.Idle;

    /// <summary>Gets or sets the owning Project identifier.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-derived tenant scope display label.</summary>
    [ProjectionFieldGroup("Scope")]
    [Display(Name = "Tenant scope")]
    public string TenantScope { get; set; } = "server-derived tenant";

    /// <summary>Gets or sets the target reference kind when applicable.</summary>
    [ProjectionFieldGroup("Target")]
    [Display(Name = "Reference type")]
    public string? ReferenceKind { get; set; }

    /// <summary>Gets or sets the target reference identifier when applicable.</summary>
    [ProjectionFieldGroup("Target")]
    [Display(Name = "Reference ID")]
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets the current safe state label.</summary>
    [ProjectionFieldGroup("Preview")]
    [Display(Name = "Current state")]
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>Gets or sets the proposed safe state label.</summary>
    [ProjectionFieldGroup("Preview")]
    [Display(Name = "Proposed state")]
    public string ProposedState { get; set; } = string.Empty;

    /// <summary>Gets or sets safe warning text for non-color-only risk communication.</summary>
    [ProjectionFieldGroup("Preview")]
    [Display(Name = "Warning")]
    public string Warning { get; set; } = string.Empty;

    /// <summary>Gets or sets the dry-run status/result label.</summary>
    [ProjectionFieldGroup("Dry run")]
    [Display(Name = "Dry-run status")]
    public string DryRunStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected audit operation.</summary>
    [ProjectionFieldGroup("Audit")]
    [Display(Name = "Expected audit operation")]
    public string ExpectedAuditOperation { get; set; } = string.Empty;

    /// <summary>Gets or sets whether explicit confirmation is required before submission.</summary>
    [ProjectionFieldGroup("Confirmation")]
    [Display(Name = "Confirmation required")]
    public bool ConfirmationRequired { get; set; } = true;

    /// <summary>Gets or sets the safe feedback code.</summary>
    [ProjectionFieldGroup("Feedback")]
    [Display(Name = "Safe feedback code")]
    public string? SafeFeedbackCode { get; set; }

    /// <summary>Gets or sets the command correlation identifier after submission.</summary>
    [ProjectionFieldGroup("Audit")]
    [Display(Name = "Correlation ID")]
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the task identifier after submission.</summary>
    [ProjectionFieldGroup("Audit")]
    [Display(Name = "Task ID")]
    public string? TaskId { get; set; }

    /// <summary>Gets or sets the confirmed audit event identifier when observed.</summary>
    [ProjectionFieldGroup("Audit")]
    [Display(Name = "Audit event ID")]
    public string? AuditEventId { get; set; }
}
