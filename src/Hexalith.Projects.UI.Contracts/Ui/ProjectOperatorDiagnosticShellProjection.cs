// <copyright file="ProjectOperatorDiagnosticShellProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Minimal FrontComposer projection seed for Projects operator diagnostics.
/// </summary>
/// <remarks>
/// This type is a metadata-only shell/navigation seed over <see cref="ProjectOperatorDiagnostic"/>.
/// Later Epic 5 stories own the full inventory, detail, health matrix, trace, audit, warning, and
/// mutation surfaces.
/// </remarks>
[Projection]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project diagnostics", Description = "Metadata-only operator diagnostic shell seed")]
public partial class ProjectOperatorDiagnosticShellProjection
{
    /// <summary>Gets or sets the stable row identity used by FrontComposer.</summary>
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

    /// <summary>Gets or sets the number of diagnostic references requiring operator attention.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Warnings")]
    public int WarningCount { get; set; }

    /// <summary>Gets or sets the diagnostic update timestamp.</summary>
    [ColumnPriority(5)]
    [RelativeTime]
    [Display(Name = "Last updated")]
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>Gets or sets the console operating mode label.</summary>
    [ColumnPriority(6)]
    [Display(Name = "Mode")]
    public string Mode { get; set; } = "read-only";

    /// <summary>Gets or sets the freshness trust evidence exposed by the diagnostic DTO.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness")]
    public string FreshnessTrustState { get; set; } = string.Empty;

    /// <summary>
    /// Creates the shell seed row from the existing Story 5.2 operator diagnostic DTO.
    /// </summary>
    /// <param name="diagnostic">The metadata-only operator diagnostic.</param>
    /// <param name="mode">The console operating mode label.</param>
    /// <returns>A FrontComposer projection seed row.</returns>
    public static ProjectOperatorDiagnosticShellProjection FromDiagnostic(
        ProjectOperatorDiagnostic diagnostic,
        string mode = "read-only")
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        string projectId = diagnostic.ProjectId ?? string.Empty;
        return new ProjectOperatorDiagnosticShellProjection
        {
            Id = projectId,
            ProjectId = projectId,
            Name = diagnostic.Name ?? string.Empty,
            Lifecycle = ParseLifecycle(diagnostic.LifecycleState),
            WarningCount = diagnostic.References.Count(IsWarningReference),
            LastUpdated = diagnostic.UpdatedAt,
            Mode = string.IsNullOrWhiteSpace(mode) ? "read-only" : mode,
            FreshnessTrustState = diagnostic.Freshness.TrustState ?? string.Empty,
        };
    }

    private static ProjectLifecycle ParseLifecycle(string? value)
        => Enum.TryParse(value, ignoreCase: true, out ProjectLifecycle lifecycle)
            ? lifecycle
            : ProjectLifecycle.Archived;

    private static bool IsWarningReference(ProjectOperatorReferenceSummary reference)
        => !Enum.TryParse(reference.ReferenceState, ignoreCase: true, out ReferenceState state)
            || state is not ReferenceState.Included;
}

