// <copyright file="ProjectOperationalDashboardProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer StatusOverview seed for metadata-only cross-project operational dashboard counts.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.StatusOverview)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project operational dashboard", Description = "Metadata-only aggregate health counts over visible Projects")]
public partial class ProjectOperationalDashboardProjection
{
    /// <summary>Gets the UI descriptor contract version for Story 5.8 dashboard parity.</summary>
    public const string ContractVersionValue = "projects.operational-dashboard.ui.v1";

    /// <summary>Gets or sets the stable descriptor identity.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = "projects-operational-dashboard-current";

    /// <summary>Gets or sets the descriptor contract version.</summary>
    [ProjectionFieldGroup("Descriptor")]
    [Display(Name = "Contract version")]
    public string ContractVersion { get; set; } = ContractVersionValue;

    /// <summary>Gets or sets the total number of visible Projects in the loaded tenant-scoped set.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Total visible projects")]
    public int TotalVisibleProjects { get; set; }

    /// <summary>Gets or sets the active Project count.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Active projects")]
    public int ActiveProjects { get; set; }

    /// <summary>Gets or sets the archived Project count.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Archived projects")]
    public int ArchivedProjects { get; set; }

    /// <summary>Gets or sets the number of Projects with at least one warning item.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Projects with warnings")]
    public int ProjectsWithWarnings { get; set; }

    /// <summary>Gets or sets the stale reference warning count.</summary>
    [ColumnPriority(5)]
    [Display(Name = "Stale references")]
    public int StaleReferences { get; set; }

    /// <summary>Gets or sets the conflict warning count.</summary>
    [ColumnPriority(6)]
    [Display(Name = "Conflicts")]
    public int Conflicts { get; set; }

    /// <summary>Gets or sets the invalid reference warning count.</summary>
    [ColumnPriority(7)]
    [Display(Name = "Invalid references")]
    public int InvalidReferences { get; set; }

    /// <summary>Gets or sets unauthorized or unavailable reference warning count.</summary>
    [ColumnPriority(8)]
    [Display(Name = "Unauthorized or unavailable")]
    public int UnauthorizedOrUnavailableReferences { get; set; }

    /// <summary>Gets or sets ambiguous, tenant-mismatch, or fail-closed evidence count.</summary>
    [ProjectionFieldGroup("Warnings")]
    [Display(Name = "Ambiguous or fail-closed")]
    public int AmbiguousOrFailClosed { get; set; }

    /// <summary>Gets or sets the number of diagnostic enrichments that were unavailable.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Diagnostic unavailable")]
    public int DiagnosticUnavailable { get; set; }

    /// <summary>Gets or sets the number of warnings with stale or unavailable freshness evidence.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness evidence warnings")]
    public int FreshnessEvidenceWarnings { get; set; }

    /// <summary>Gets or sets the server-derived tenant scope display label.</summary>
    [ProjectionFieldGroup("Scope")]
    [Display(Name = "Tenant scope")]
    public string TenantScope { get; set; } = "server-derived tenant";

    /// <summary>Gets or sets the newest observed warning timestamp when available.</summary>
    [ProjectionFieldGroup("Freshness")]
    [RelativeTime]
    [Display(Name = "Last observed warning")]
    public DateTimeOffset? LastObservedWarningAt { get; set; }
}
