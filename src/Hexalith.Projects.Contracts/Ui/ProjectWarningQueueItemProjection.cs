// <copyright file="ProjectWarningQueueItemProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer ActionQueue seed for one metadata-only project warning queue item.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = "Stale,Conflict,InvalidReference,Unauthorized,Unavailable,Archived,Ambiguous,TenantMismatch,Excluded")]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project warning queue item", Description = "Metadata-only warning queue item over visible Projects diagnostics")]
public partial class ProjectWarningQueueItemProjection
{
    /// <summary>Gets the UI descriptor contract version for Story 5.8 warning queue parity.</summary>
    public const string ContractVersionValue = "projects.warning-queue-item.ui.v1";

    /// <summary>Gets or sets the stable queue item identity.</summary>
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

    /// <summary>Gets or sets the safe Project display name.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Project name")]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Gets or sets the shared warning/reference state.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Warning state")]
    public ReferenceState State { get; set; } = ReferenceState.Unavailable;

    /// <summary>Gets or sets the shared Project lifecycle value.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Lifecycle")]
    public ProjectLifecycle Lifecycle { get; set; }

    /// <summary>Gets or sets the optional shared reason code.</summary>
    [ColumnPriority(5)]
    [Display(Name = "Reason code")]
    public ProjectReasonCode? ReasonCode { get; set; }

    /// <summary>Gets or sets the reference kind when the warning is tied to a reference.</summary>
    [ColumnPriority(6)]
    [Display(Name = "Reference type")]
    public string ReferenceKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the opaque reference identifier when available.</summary>
    [ColumnPriority(7)]
    [Display(Name = "Reference ID")]
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the bounded-context owner for the reference.</summary>
    [ProjectionFieldGroup("Reference")]
    [Display(Name = "Owner context")]
    public string OwnerContext { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-derived tenant scope display label.</summary>
    [ProjectionFieldGroup("Scope")]
    [Display(Name = "Tenant scope")]
    public string TenantScope { get; set; } = "server-derived tenant";

    /// <summary>Gets or sets the last observed timestamp for the warning evidence.</summary>
    [ColumnPriority(8)]
    [RelativeTime]
    [Display(Name = "Last observed")]
    public DateTimeOffset LastObservedAt { get; set; }

    /// <summary>Gets or sets the safe freshness trust state.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness trust")]
    public string FreshnessTrustState { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional projection watermark.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Projection watermark")]
    public string? ProjectionWatermark { get; set; }

    /// <summary>Gets or sets the source section that produced the safe warning evidence.</summary>
    [ProjectionFieldGroup("Diagnostics")]
    [Display(Name = "Source section")]
    public string SourceSection { get; set; } = "operator-diagnostics";

    /// <summary>Gets or sets a read-only safe action availability label.</summary>
    [ProjectionFieldGroup("Actions")]
    [Display(Name = "Safe actions")]
    public string SafeActionAvailabilityLabel { get; set; } = "Open project; inspect metadata; maintenance handled by Story 5.9";
}
