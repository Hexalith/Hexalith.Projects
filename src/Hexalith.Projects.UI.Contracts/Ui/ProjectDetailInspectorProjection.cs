// <copyright file="ProjectDetailInspectorProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// FrontComposer DetailRecord seed for the read-only Project inspector.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project detail inspector", Description = "Metadata-only project detail inspector seed")]
public partial class ProjectDetailInspectorProjection
{
    /// <summary>Gets or sets the stable detail identity.</summary>
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

    /// <summary>Gets or sets the detail update timestamp.</summary>
    [ColumnPriority(4)]
    [RelativeTime]
    [Display(Name = "Updated")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether context activation is enabled.</summary>
    [ProjectionFieldGroup("Context")]
    [Display(Name = "Context activation")]
    public bool ContextActivationEnabled { get; set; }

    /// <summary>Gets or sets the safe blocked reason code when context activation is disabled.</summary>
    [ProjectionFieldGroup("Context")]
    [Display(Name = "Blocked reason")]
    public string? ContextBlockedReasonCode { get; set; }

    /// <summary>Gets or sets the number of safe reference summaries.</summary>
    [ProjectionFieldGroup("References")]
    [Display(Name = "References")]
    public int ReferenceCount { get; set; }

    /// <summary>Gets or sets the number of bounded audit entries available to the inspector.</summary>
    [ProjectionFieldGroup("Audit")]
    [Display(Name = "Audit entries")]
    public int AuditEntryCount { get; set; }

    /// <summary>Gets or sets the freshness trust state.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness")]
    public string FreshnessTrustState { get; set; } = string.Empty;

    /// <summary>Creates the inspector seed from the approved operator diagnostic DTO.</summary>
    /// <param name="diagnostic">The metadata-only diagnostic DTO.</param>
    /// <returns>A FrontComposer DetailRecord seed.</returns>
    public static ProjectDetailInspectorProjection FromDiagnostic(ProjectOperatorDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new ProjectDetailInspectorProjection
        {
            Id = diagnostic.ProjectId ?? string.Empty,
            ProjectId = diagnostic.ProjectId ?? string.Empty,
            Name = diagnostic.Name ?? string.Empty,
            Lifecycle = ParseLifecycle(diagnostic.LifecycleState),
            UpdatedAt = diagnostic.UpdatedAt,
            ContextActivationEnabled = diagnostic.ContextActivation.Enabled,
            ContextBlockedReasonCode = diagnostic.ContextActivation.BlockedReasonCode,
            ReferenceCount = diagnostic.References.Count,
            AuditEntryCount = diagnostic.AuditTimeline.Count,
            FreshnessTrustState = diagnostic.Freshness.TrustState ?? string.Empty,
        };
    }

    private static ProjectLifecycle ParseLifecycle(string? lifecycleState)
        => Enum.TryParse(lifecycleState, ignoreCase: true, out ProjectLifecycle lifecycle)
            ? lifecycle
            : ProjectLifecycle.Archived;
}
