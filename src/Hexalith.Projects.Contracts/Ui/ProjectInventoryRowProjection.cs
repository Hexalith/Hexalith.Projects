// <copyright file="ProjectInventoryRowProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer Level-1 metadata-only inventory row over the existing Projects list response shape.
/// </summary>
[Projection]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project inventory", Description = "Metadata-only project inventory row")]
public partial class ProjectInventoryRowProjection
{
    /// <summary>
    /// The single canonical placeholder shown when the warning summary cannot be derived from the
    /// current metadata-only list row (no additive summary field exists yet).
    /// </summary>
    public const string WarningSummaryUnavailable = "Not available on list row";

    /// <summary>Gets or sets the stable FrontComposer row identity.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the Project identifier.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe Project display name.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the shared lifecycle vocabulary value.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Lifecycle")]
    public ProjectLifecycle Lifecycle { get; set; }

    /// <summary>Gets or sets the safe warning summary available on the list row.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Warnings")]
    public string WarningSummary { get; set; } = WarningSummaryUnavailable;

    /// <summary>Gets or sets the Project update timestamp.</summary>
    [ColumnPriority(5)]
    [RelativeTime]
    [Display(Name = "Updated")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the Project creation timestamp.</summary>
    [ColumnPriority(6)]
    [RelativeTime]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the server-derived tenant scope display label.</summary>
    [ProjectionFieldGroup("Scope")]
    [Display(Name = "Tenant scope")]
    public string TenantScope { get; set; } = "server-derived tenant";

    /// <summary>Gets or sets the freshness trust state.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness")]
    public string FreshnessTrustState { get; set; } = string.Empty;

    /// <summary>Gets or sets the projection watermark when available.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Projection watermark")]
    public string? ProjectionWatermark { get; set; }

    /// <summary>Gets or sets a value indicating whether the row was reported stale.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Stale")]
    public bool Stale { get; set; }
}
