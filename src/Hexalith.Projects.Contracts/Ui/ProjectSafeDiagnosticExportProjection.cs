// <copyright file="ProjectSafeDiagnosticExportProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer DetailRecord seed for the Story 5.7 safe diagnostic export contract.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project safe diagnostic export", Description = "Metadata-only diagnostic export descriptor")]
public partial class ProjectSafeDiagnosticExportProjection
{
    /// <summary>Gets the safe diagnostic export schema version.</summary>
    public const string ContractVersionValue = "projects.safe-diagnostic-export.v1";

    /// <summary>Gets the explicit export guarantee shown in Web and emitted in JSON.</summary>
    public const string PayloadExclusionGuaranteeText = "Payload-bearing data is excluded; this export contains metadata only.";

    /// <summary>Gets or sets the stable descriptor identity.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = "safe-diagnostic-export-current";

    /// <summary>Gets or sets the descriptor contract version.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Schema version")]
    public string ContractVersion { get; set; } = ContractVersionValue;

    /// <summary>Gets or sets the export generation timestamp.</summary>
    [ColumnPriority(2)]
    [RelativeTime]
    [Display(Name = "Generated")]
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Gets or sets the Project identifier.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe Project name.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Project name")]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Gets or sets the lifecycle state.</summary>
    [ColumnPriority(5)]
    [Display(Name = "Lifecycle")]
    public string LifecycleState { get; set; } = string.Empty;

    /// <summary>Gets or sets the server-derived tenant scope display label.</summary>
    [ProjectionFieldGroup("Scope")]
    [Display(Name = "Tenant scope label")]
    public string TenantScopeLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the freshness trust state.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness trust")]
    public string FreshnessTrustState { get; set; } = string.Empty;

    /// <summary>Gets or sets the projection watermark.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Projection watermark")]
    public string? ProjectionWatermark { get; set; }

    /// <summary>Gets or sets the number of reference-health rows included.</summary>
    [ProjectionFieldGroup("Contents")]
    [Display(Name = "Reference rows")]
    public int ReferenceHealthRowCount { get; set; }

    /// <summary>Gets or sets the number of audit rows included.</summary>
    [ProjectionFieldGroup("Contents")]
    [Display(Name = "Audit rows")]
    public int AuditRowCount { get; set; }

    /// <summary>Gets or sets the safe feedback reason codes included.</summary>
    [ProjectionFieldGroup("Contents")]
    [Display(Name = "Safe feedback codes")]
    public string SafeFeedbackCodes { get; set; } = string.Empty;

    /// <summary>Gets or sets the deterministic included field list.</summary>
    [ProjectionFieldGroup("Contract")]
    [Display(Name = "Included fields")]
    public string IncludedFieldNames { get; set; } = string.Empty;

    /// <summary>Gets or sets the deterministic excluded data category list.</summary>
    [ProjectionFieldGroup("Contract")]
    [Display(Name = "Excluded payload categories")]
    public string ExcludedPayloadCategories { get; set; } = string.Empty;

    /// <summary>Gets or sets the explicit payload-exclusion guarantee.</summary>
    [ProjectionFieldGroup("Contract")]
    [Display(Name = "Payload exclusion guarantee")]
    public string PayloadExclusionGuarantee { get; set; } = PayloadExclusionGuaranteeText;
}
