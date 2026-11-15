// <copyright file="ProjectResolutionTraceCandidateProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer DetailRecord seed for one transient resolution candidate comparison row.
/// Candidate score and rank are safe only in this computed trace context.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project resolution trace candidate", Description = "Metadata-only transient candidate comparison row")]
public partial class ProjectResolutionTraceCandidateProjection
{
    /// <summary>Gets or sets the stable row identity within the current rendered trace.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the opaque candidate Project identifier.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional safe Project display name.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the one-based engine rank for this computed trace result.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Rank")]
    public int Rank { get; set; }

    /// <summary>Gets or sets the engine score for this computed trace result.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Score")]
    public int Score { get; set; }

    /// <summary>Gets or sets the shared-vocabulary reason codes that contributed to the score.</summary>
    [ProjectionFieldGroup("Reasons")]
    [Display(Name = "Reason codes")]
    public string ReasonCodes { get; set; } = string.Empty;
}
