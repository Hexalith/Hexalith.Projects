// <copyright file="ProjectWarningsDashboardMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Maps existing operator diagnostic metadata to warning queue rows and dashboard aggregates.
/// </summary>
public static class ProjectWarningsDashboardMapper
{
    private const string TenantScopeLabel = "server-derived tenant";

    public static IReadOnlyList<ProjectWarningQueueItemProjection> BuildQueueItems(
        ProjectInventoryRowProjection project,
        ProjectOperatorDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(diagnostic);

        return diagnostic.References
            .Select(reference => BuildQueueItem(project, reference))
            .Where(item => item.State is not (ReferenceState.Included or ReferenceState.Pending))
            .OrderBy(item => WarningSortRank(item.State))
            .ThenByDescending(item => item.LastObservedAt)
            .ThenBy(item => item.ProjectId, StringComparer.Ordinal)
            .ThenBy(item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(item => item.ReferenceId, StringComparer.Ordinal)
            .ToArray();
    }

    public static ProjectWarningQueueItemProjection DiagnosticUnavailableItem(
        ProjectInventoryRowProjection project,
        string safeReasonCode)
    {
        ArgumentNullException.ThrowIfNull(project);

        string reason = SafeToken(safeReasonCode);
        return new ProjectWarningQueueItemProjection
        {
            Id = $"{project.ProjectId}:diagnostic:{reason}",
            ProjectId = project.ProjectId,
            ProjectName = project.Name,
            Lifecycle = project.Lifecycle,
            State = ReferenceState.Unavailable,
            ReferenceKind = string.Empty,
            ReferenceId = string.Empty,
            OwnerContext = "Projects",
            TenantScope = TenantScopeLabel,
            LastObservedAt = project.UpdatedAt,
            FreshnessTrustState = string.IsNullOrWhiteSpace(project.FreshnessTrustState)
                ? "unavailable"
                : project.FreshnessTrustState,
            ProjectionWatermark = project.ProjectionWatermark,
            SourceSection = $"operator-diagnostics:{reason}",
            SafeActionAvailabilityLabel = "Open project; diagnostics unavailable; maintenance handled by Story 5.9",
        };
    }

    public static ProjectOperationalDashboardProjection BuildDashboard(
        IReadOnlyList<ProjectInventoryRowProjection> projects,
        IReadOnlyList<ProjectWarningQueueItemProjection> queueItems,
        int diagnosticUnavailableCount)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(queueItems);

        return new ProjectOperationalDashboardProjection
        {
            TotalVisibleProjects = projects.Count,
            ActiveProjects = projects.Count(p => p.Lifecycle == ProjectLifecycle.Active),
            ArchivedProjects = projects.Count(p => p.Lifecycle == ProjectLifecycle.Archived),
            ProjectsWithWarnings = queueItems
                .Select(item => item.ProjectId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Count(),
            StaleReferences = queueItems.Count(item => item.State == ReferenceState.Stale),
            Conflicts = queueItems.Count(item => item.State == ReferenceState.Conflict),
            InvalidReferences = queueItems.Count(item => item.State == ReferenceState.InvalidReference),
            UnauthorizedOrUnavailableReferences = queueItems.Count(item =>
                item.State is ReferenceState.Unauthorized or ReferenceState.Unavailable),
            AmbiguousOrFailClosed = queueItems.Count(item =>
                item.State is ReferenceState.Ambiguous or ReferenceState.TenantMismatch),
            DiagnosticUnavailable = diagnosticUnavailableCount,
            FreshnessEvidenceWarnings = queueItems.Count(item =>
                string.Equals(item.FreshnessTrustState, "stale", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.FreshnessTrustState, "unavailable", StringComparison.OrdinalIgnoreCase)),
            TenantScope = TenantScopeLabel,
            LastObservedWarningAt = queueItems.Count == 0 ? null : queueItems.Max(item => item.LastObservedAt),
        };
    }

    private static ProjectWarningQueueItemProjection BuildQueueItem(
        ProjectInventoryRowProjection project,
        ProjectOperatorReferenceSummary reference)
    {
        string referenceKind = ProjectReferenceHealthRowProjection.NormalizeCode(reference.ReferenceKind);
        string referenceId = reference.ReferenceId ?? string.Empty;
        bool stateKnown = TryParseSharedEnum(reference.ReferenceState, out ReferenceState state);
        bool reasonKnown = TryParseSharedEnum(reference.ReasonCode, out ProjectReasonCode reason);
        string sourceSection = stateKnown
            ? "operator-diagnostics.references"
            : "operator-diagnostics.references:unknown-state";
        if (!string.IsNullOrWhiteSpace(reference.ReasonCode) && !reasonKnown)
        {
            sourceSection = $"{sourceSection}:unknown-reason";
        }

        ReferenceState warningState = stateKnown ? state : ReferenceState.Unavailable;
        return new ProjectWarningQueueItemProjection
        {
            Id = ProjectReferenceHealthRowProjection.BuildId(project.ProjectId, referenceKind, referenceId),
            ProjectId = project.ProjectId,
            ProjectName = project.Name,
            Lifecycle = project.Lifecycle,
            State = warningState,
            ReasonCode = reasonKnown ? reason : null,
            ReferenceKind = referenceKind,
            ReferenceId = referenceId,
            OwnerContext = ProjectReferenceHealthRowProjection.OwnerContextFor(referenceKind),
            TenantScope = TenantScopeLabel,
            LastObservedAt = reference.Freshness.ObservedAt,
            FreshnessTrustState = string.IsNullOrWhiteSpace(reference.Freshness.TrustState)
                ? "unavailable"
                : reference.Freshness.TrustState,
            ProjectionWatermark = reference.Freshness.ProjectionWatermark,
            SourceSection = sourceSection,
        };
    }

    private static bool TryParseSharedEnum<TEnum>(string? value, out TEnum parsed)
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

    private static int WarningSortRank(ReferenceState state)
        => state switch
        {
            ReferenceState.Unauthorized or ReferenceState.TenantMismatch => 0,
            ReferenceState.Unavailable => 1,
            ReferenceState.Conflict or ReferenceState.InvalidReference => 2,
            ReferenceState.Ambiguous => 3,
            ReferenceState.Stale or ReferenceState.Archived => 4,
            ReferenceState.Excluded or ReferenceState.Pending => 5,
            _ => 6,
        };

    private static string NormalizeEnumToken(string value)
        => value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

    private static string SafeToken(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToLowerInvariant().Replace(' ', '_');
}
