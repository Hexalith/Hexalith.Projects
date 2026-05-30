// <copyright file="ProjectAuditTimelineRowProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// FrontComposer DetailRecord seed for one metadata-only audit timeline row.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project audit timeline row", Description = "Metadata-only audit timeline row from operator diagnostics")]
public partial class ProjectAuditTimelineRowProjection
{
    /// <summary>Gets the UI descriptor contract version for Story 5.7 audit timeline parity.</summary>
    public const string ContractVersionValue = "projects.audit-timeline-row.ui.v1";

    /// <summary>Gets or sets the stable row identity.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the descriptor contract version.</summary>
    [ProjectionFieldGroup("Descriptor")]
    [Display(Name = "Contract version")]
    public string ContractVersion { get; set; } = ContractVersionValue;

    /// <summary>Gets or sets the owning Project identifier.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the audit event identifier.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Audit event ID")]
    public string AuditEventId { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation type.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Operation")]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the occurrence timestamp.</summary>
    [ColumnPriority(4)]
    [RelativeTime]
    [Display(Name = "Occurred")]
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Gets or sets the actor/source principal identifier.</summary>
    [ColumnPriority(5)]
    [Display(Name = "Actor")]
    public string ActorPrincipalId { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation correlation identifier.</summary>
    [ProjectionFieldGroup("Traceability")]
    [Display(Name = "Correlation ID")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation task identifier.</summary>
    [ProjectionFieldGroup("Traceability")]
    [Display(Name = "Task ID")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>Gets or sets the affected reference kind.</summary>
    [ProjectionFieldGroup("Reference")]
    [Display(Name = "Reference kind")]
    public string? ReferenceKind { get; set; }

    /// <summary>Gets or sets the affected reference identifier.</summary>
    [ProjectionFieldGroup("Reference")]
    [Display(Name = "Reference ID")]
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets the previous safe state.</summary>
    [ProjectionFieldGroup("State")]
    [Display(Name = "Previous state")]
    public string? PreviousState { get; set; }

    /// <summary>Gets or sets the new safe state.</summary>
    [ProjectionFieldGroup("State")]
    [Display(Name = "New state")]
    public string? NewState { get; set; }

    /// <summary>Gets or sets the safe reason code.</summary>
    [ProjectionFieldGroup("State")]
    [Display(Name = "Reason code")]
    public string? ReasonCode { get; set; }

    /// <summary>Gets or sets the safe conversation identifier for resolution rows.</summary>
    [ProjectionFieldGroup("Resolution")]
    [Display(Name = "Conversation ID")]
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets the safe source Project identifier for resolution rows.</summary>
    [ProjectionFieldGroup("Resolution")]
    [Display(Name = "Source Project ID")]
    public string? SourceProjectId { get; set; }

    /// <summary>Gets or sets the projection sequence used as freshness evidence.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Projection sequence")]
    public long ProjectionSequence { get; set; }

    /// <summary>Creates a metadata-only UI row from the approved operator audit DTO.</summary>
    public static ProjectAuditTimelineRowProjection FromAuditItem(
        string projectId,
        ProjectOperatorAuditTimelineItem item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(item);

        return new ProjectAuditTimelineRowProjection
        {
            Id = $"{projectId}:{item.AuditEventId}",
            ProjectId = projectId,
            AuditEventId = item.AuditEventId,
            OperationType = item.OperationType,
            OccurredAt = item.OccurredAt,
            ActorPrincipalId = item.ActorPrincipalId,
            CorrelationId = item.CorrelationId,
            TaskId = item.TaskId,
            ReferenceKind = item.ReferenceKind,
            ReferenceId = item.ReferenceId,
            PreviousState = item.PreviousState,
            NewState = item.NewState,
            ReasonCode = item.ReasonCode,
            ConversationId = item.ConversationId,
            SourceProjectId = item.SourceProjectId,
            ProjectionSequence = item.ProjectionSequence,
        };
    }
}
