// <copyright file="ProjectReferenceHealthRowProjection.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// FrontComposer DetailRecord seed for one metadata-only reference-health matrix row.
/// </summary>
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
[BoundedContext("Projects", DisplayLabel = "Projects")]
[Display(Name = "Project reference health row", Description = "Metadata-only reference health matrix row")]
public partial class ProjectReferenceHealthRowProjection
{
    private string? _diagnosticCode;

    /// <summary>Gets the stable matrix row identity.</summary>
    [ColumnPriority(0)]
    [Display(Name = "ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets the owning Project identifier.</summary>
    [ColumnPriority(1)]
    [Display(Name = "Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Gets the reference kind: conversation, folder, file, or memory.</summary>
    [ColumnPriority(2)]
    [Display(Name = "Reference type")]
    public string ReferenceKind { get; set; } = string.Empty;

    /// <summary>Gets the bounded-context owner for the reference.</summary>
    [ColumnPriority(3)]
    [Display(Name = "Owner context")]
    public string OwnerContext { get; set; } = string.Empty;

    /// <summary>Gets the opaque sibling-owned reference identifier.</summary>
    [ColumnPriority(4)]
    [Display(Name = "Reference ID")]
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>Gets the optional safe display label.</summary>
    [ColumnPriority(5)]
    [Display(Name = "Label")]
    public string? DisplayLabel { get; set; }

    /// <summary>Gets the shared inclusion state.</summary>
    [ColumnPriority(6)]
    [Display(Name = "Inclusion state")]
    public ReferenceState InclusionState { get; set; } = ReferenceState.Unavailable;

    /// <summary>Gets the shared health state.</summary>
    [ColumnPriority(7)]
    [Display(Name = "Health state")]
    public ReferenceState HealthState { get; set; } = ReferenceState.Unavailable;

    /// <summary>Gets the optional shared reason code.</summary>
    [ProjectionFieldGroup("Diagnostics")]
    [Display(Name = "Reason code")]
    public ProjectReasonCode? ReasonCode { get; set; }

    /// <summary>Gets the optional failed inclusion check.</summary>
    [ProjectionFieldGroup("Diagnostics")]
    [Display(Name = "Failed check")]
    public ProjectContextInclusionCheck? InclusionCheck { get; set; }

    /// <summary>Gets the optional closed-vocabulary diagnostic code.</summary>
    [ProjectionFieldGroup("Diagnostics")]
    [Display(Name = "Diagnostic code")]
    public string? DiagnosticCode
    {
        get => _diagnosticCode;
        set
        {
            if (!ProjectContextInclusionDiagnostic.IsKnown(value))
            {
                throw new ArgumentException(
                    "Diagnostic value is not a member of the closed ProjectContextInclusionDiagnostic vocabulary.",
                    nameof(value));
            }

            _diagnosticCode = value;
        }
    }

    /// <summary>Gets the last observed check timestamp.</summary>
    [ColumnPriority(8)]
    [RelativeTime]
    [Display(Name = "Last checked")]
    public DateTimeOffset LastCheckedAt { get; set; }

    /// <summary>Gets the safe freshness trust state.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Freshness trust")]
    public string FreshnessTrustState { get; set; } = string.Empty;

    /// <summary>Gets the optional projection watermark.</summary>
    [ProjectionFieldGroup("Freshness")]
    [Display(Name = "Projection watermark")]
    public string? ProjectionWatermark { get; set; }

    /// <summary>Gets a read-only safe action availability label.</summary>
    [ProjectionFieldGroup("Actions")]
    [Display(Name = "Safe actions")]
    public string SafeActionAvailabilityLabel { get; set; } = "Inspect metadata; copy id; maintenance handled by Story 5.9";

    /// <summary>Creates a row from the existing metadata-only operator reference summary.</summary>
    /// <param name="projectId">The owning Project identifier.</param>
    /// <param name="summary">The approved reference summary DTO.</param>
    /// <returns>A metadata-only reference-health row.</returns>
    public static ProjectReferenceHealthRowProjection FromReferenceSummary(
        string projectId,
        ProjectOperatorReferenceSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(summary);

        string referenceKind = NormalizeCode(summary.ReferenceKind);
        string referenceId = summary.ReferenceId ?? string.Empty;
        ReferenceState state = ParseEnum(summary.ReferenceState, ReferenceState.Unavailable);

        return new ProjectReferenceHealthRowProjection
        {
            Id = BuildId(projectId, referenceKind, referenceId),
            ProjectId = projectId,
            ReferenceKind = referenceKind,
            OwnerContext = OwnerContextFor(referenceKind),
            ReferenceId = referenceId,
            DisplayLabel = summary.DisplayName,
            InclusionState = state,
            HealthState = state,
            ReasonCode = ParseNullableEnum<ProjectReasonCode>(summary.ReasonCode),
            LastCheckedAt = summary.Freshness.ObservedAt,
            FreshnessTrustState = summary.Freshness.TrustState,
            ProjectionWatermark = summary.Freshness.ProjectionWatermark,
        };
    }

    /// <summary>Builds a stable row id from the safe row key.</summary>
    public static string BuildId(string projectId, string referenceKind, string referenceId)
        => $"{projectId}:{NormalizeCode(referenceKind)}:{referenceId}";

    /// <summary>Gets the bounded-context owner for a reference kind.</summary>
    public static string OwnerContextFor(string referenceKind)
        => NormalizeCode(referenceKind) switch
        {
            "conversation" => "Conversations",
            "folder" => "Folders",
            "file" => "Folders",
            "memory" => "Memories",
            _ => "Projects",
        };

    /// <summary>Normalizes generated and hand-authored kind codes to lower invariant row keys.</summary>
    public static string NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

    /// <summary>Parses shared-vocabulary values from Pascal, camel, snake, or kebab spellings.</summary>
    public static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
        => TryParseEnum(value, out TEnum parsed) ? parsed : fallback;

    /// <summary>Parses nullable shared-vocabulary values from Pascal, camel, snake, or kebab spellings.</summary>
    public static TEnum? ParseNullableEnum<TEnum>(string? value)
        where TEnum : struct, Enum
        => TryParseEnum(value, out TEnum parsed) ? parsed : null;

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            string normalized = NormalizeEnumToken(value);
            foreach (TEnum member in (TEnum[])Enum.GetValues(typeof(TEnum)))
            {
                if (string.Equals(NormalizeEnumToken(member.ToString()), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    parsed = member;
                    return true;
                }
            }
        }

        parsed = default;
        return false;
    }

    private static string NormalizeEnumToken(string value)
        => value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
}
