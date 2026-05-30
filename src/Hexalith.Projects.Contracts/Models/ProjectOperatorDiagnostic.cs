// <copyright file="ProjectOperatorDiagnostic.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System.Text.Json.Serialization;

/// <summary>Metadata-only operator diagnostic model for one Project.</summary>
/// <param name="ProjectId">The Project identifier.</param>
/// <param name="Name">The safe Project display name.</param>
/// <param name="Description">The safe Project description when available.</param>
/// <param name="LifecycleState">The Project lifecycle state.</param>
/// <param name="CreatedAt">The Project creation timestamp.</param>
/// <param name="UpdatedAt">The Project update timestamp.</param>
/// <param name="SetupMetadata">The existing bounded setup metadata field.</param>
/// <param name="ProjectSetup">The current bounded setup preferences.</param>
/// <param name="ContextActivation">The context activation summary.</param>
/// <param name="References">The current folder/file/memory reference summaries.</param>
/// <param name="AuditTimeline">The bounded safe audit timeline window.</param>
/// <param name="Freshness">The combined freshness evidence for the diagnostic model.</param>
public sealed record ProjectOperatorDiagnostic(
    string ProjectId,
    string Name,
    string? Description,
    string LifecycleState,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    string? SetupMetadata,
    ProjectSetup? ProjectSetup,
    ProjectOperatorContextActivation ContextActivation,
    IReadOnlyList<ProjectOperatorReferenceSummary> References,
    IReadOnlyList<ProjectOperatorAuditTimelineItem> AuditTimeline,
    ProjectOperatorFreshnessMetadata Freshness);
