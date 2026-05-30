// <copyright file="ProjectResolutionTraceExclusionProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer DetailRecord seed for one transient resolution exclusion evidence row.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project resolution trace exclusion", Description = "Metadata-only transient exclusion evidence row")]
public partial class ProjectResolutionTraceExclusionProjection
{
    private string? _diagnostic;

    /// <summary>Gets or sets the stable row identity within the current rendered trace.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the opaque excluded Project identifier.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional safe Project display name.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the shared reference-state evidence for the exclusion.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Reference state")]
    public ReferenceState ReferenceState { get; set; } = ReferenceState.Unavailable;

    /// <summary>Gets or sets the optional shared reason code associated with the failed signal.</summary>
    [ProjectionFieldGroup("Diagnostics")]
    [Display(Name = "Reason code")]
    public ProjectReasonCode? ReasonCode { get; set; }

    /// <summary>Gets or sets the optional closed-vocabulary inclusion diagnostic.</summary>
    [ProjectionFieldGroup("Diagnostics")]
    [Display(Name = "Diagnostic")]
    public string? Diagnostic
    {
        get => _diagnostic;
        set
        {
            if (!ProjectContextInclusionDiagnostic.IsKnown(value))
            {
                throw new ArgumentException(
                    "Diagnostic value is not a member of the closed ProjectContextInclusionDiagnostic vocabulary.",
                    nameof(value));
            }

            _diagnostic = value;
        }
    }
}
