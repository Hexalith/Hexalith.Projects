// <copyright file="ProjectMaintenanceActionProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

/// <summary>Shared action names for Web now and MCP/CLI parity later.</summary>
public static class ProjectMaintenanceActions
{
    public const string Archive = "archive";
    public const string Restore = "restore";
    public const string Relink = "relink";
    public const string Unlink = "unlink";
    public const string Reevaluate = "reevaluate";
}

/// <summary>Shared maintenance panel states.</summary>
public static class ProjectMaintenancePanelStates
{
    public const string Preview = "Preview";
    public const string DryRunRequired = "DryRunRequired";
    public const string DryRunPassed = "DryRunPassed";
    public const string DryRunBlocked = "DryRunBlocked";
    public const string ConfirmationRequired = "ConfirmationRequired";
    public const string Executing = "Executing";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}

/// <summary>Shared maintenance panel states for FrontComposer status filtering.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectMaintenancePanelState>))]
public enum ProjectMaintenancePanelState
{
    [ProjectionBadge(BadgeSlot.Info)]
    Preview,

    [ProjectionBadge(BadgeSlot.Warning)]
    DryRunRequired,

    [ProjectionBadge(BadgeSlot.Success)]
    DryRunPassed,

    [ProjectionBadge(BadgeSlot.Danger)]
    DryRunBlocked,

    [ProjectionBadge(BadgeSlot.Warning)]
    ConfirmationRequired,

    [ProjectionBadge(BadgeSlot.Info)]
    Executing,

    [ProjectionBadge(BadgeSlot.Success)]
    Succeeded,

    [ProjectionBadge(BadgeSlot.Danger)]
    Failed,
}

/// <summary>Shared command lifecycle states for command-async maintenance actions.</summary>
public static class ProjectMaintenanceCommandLifecycleStates
{
    public const string Idle = "Idle";
    public const string Submitting = "Submitting";
    public const string Acknowledged = "Acknowledged(202)";
    public const string Syncing = "Syncing";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";
}
