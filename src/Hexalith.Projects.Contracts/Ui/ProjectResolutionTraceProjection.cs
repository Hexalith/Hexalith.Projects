// <copyright file="ProjectResolutionTraceProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer DetailRecord seed for the transient, compute-on-demand resolution trace workbench.
/// This is a UI descriptor only; it is not a persisted trace projection or trace history row.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project resolution trace", Description = "Metadata-only transient resolution trace descriptor")]
public partial class ProjectResolutionTraceProjection
{
    /// <summary>Gets the UI descriptor contract version for Story 5.6 trace parity.</summary>
    public const string ContractVersionValue = "projects.resolution-trace.ui.v1";

    /// <summary>Gets or sets the stable descriptor identity. This is not a persisted trace id.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = "resolution-trace-current";

    /// <summary>Gets or sets the descriptor contract version.</summary>
    [ProjectionFieldGroup("Descriptor")]
    [Display(Name = "Contract version")]
    public string ContractVersion { get; set; } = ContractVersionValue;

    /// <summary>Gets or sets the trace input mode: conversation or attachments.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Input mode")]
    public string InputMode { get; set; } = string.Empty;

    /// <summary>Gets or sets the presented conversation id for conversation traces.</summary>
    [ProjectionFieldGroup("Input")]
    [Display(Name = "Conversation ID")]
    public string? PresentedConversationId { get; set; }

    /// <summary>Gets or sets deterministic, deduplicated folder ids for attachment traces.</summary>
    [ProjectionFieldGroup("Input")]
    [Display(Name = "Folder IDs")]
    public string PresentedFolderIds { get; set; } = string.Empty;

    /// <summary>Gets or sets deterministic, deduplicated file ids for attachment traces.</summary>
    [ProjectionFieldGroup("Input")]
    [Display(Name = "File IDs")]
    public string PresentedFileIds { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether archived Projects were included by the query.</summary>
    [ProjectionFieldGroup("Input")]
    [Display(Name = "Include archived")]
    public bool IncludeArchived { get; set; }

    /// <summary>Gets or sets the observed timestamp supplied by the resolution query response.</summary>
    [ColumnPriority(2)]
    [RelativeTime]
    [Display(Name = "Observed")]
    public DateTimeOffset ObservedAt { get; set; }

    /// <summary>Gets or sets the shared resolution result vocabulary value.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Result")]
    public ResolutionResult Result { get; set; } = ResolutionResult.NoMatch;

    /// <summary>Gets or sets the number of candidate rows in this transient trace result.</summary>
    [ProjectionFieldGroup("Result")]
    [Display(Name = "Candidates")]
    public int CandidateCount { get; set; }

    /// <summary>Gets or sets the number of exclusion rows in this transient trace result.</summary>
    [ProjectionFieldGroup("Result")]
    [Display(Name = "Exclusions")]
    public int ExclusionCount { get; set; }
}
