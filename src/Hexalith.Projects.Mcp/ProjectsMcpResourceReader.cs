// <copyright file="ProjectsMcpResourceReader.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

using System.Globalization;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Mcp;
using Hexalith.Projects.Client.Diagnostics;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Ui;

using EvidenceFreshnessStateCode = Hexalith.Projects.Contracts.Models.EvidenceFreshnessStateCode;

/// <summary>
/// Generated-client backed FrontComposer query service for Projects MCP resources.
/// </summary>
public sealed class ProjectsMcpResourceReader(IClient client) : IQueryService
{
    private const int DefaultAuditLimit = 25;
    private const int MaxAuditLimit = 100;
    private const int MaxRows = 100;
    private const string TenantScope = "server-derived tenant";
    private const string PayloadExcludedExplanation = "Payloads, raw problem details, idempotency keys, command bodies, paths, prompts, and sibling denial details are excluded.";

    private readonly IClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (typeof(T) == typeof(ProjectsMcpWarningQueueItem))
            {
                ProjectsMcpWarningScan scan = await BuildWarningScanAsync(cancellationToken).ConfigureAwait(false);
                IReadOnlyList<T> warningItems = Cast<T>(LimitWarningRows(scan, request.Criteria.Take));
                return new QueryResult<T>(warningItems, scan.Warnings.Count, ETag: null);
            }

            IReadOnlyList<T> items = typeof(T) switch
            {
                Type t when t == typeof(ProjectsMcpInventoryItem) => Cast<T>(await ReadInventoryAsync(request.Criteria.Take, cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpProjectDetailItem) => Cast<T>(await ReadDetailAsync(null, cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpOperatorDiagnosticItem) => Cast<T>(await ReadOperatorDiagnosticAsync(null, DefaultAuditLimit, cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpReferenceHealthItem) => Cast<T>(await ReadReferenceHealthAsync(null, DefaultAuditLimit, cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpAuditTimelineItem) => Cast<T>(await ReadAuditTimelineAsync(null, DefaultAuditLimit, cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpSafeDiagnosticExportItem) => Cast<T>(await ReadSafeDiagnosticExportAsync(null, DefaultAuditLimit, cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpWarningScanSummaryItem) => Cast<T>(await ReadWarningScanSummaryAsync(cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpOperationalDashboardItem) => Cast<T>(await ReadOperationalDashboardAsync(cancellationToken).ConfigureAwait(false)),
                Type t when t == typeof(ProjectsMcpMaintenanceActionItem) => Cast<T>(ReadMaintenanceActions()),
                Type t when t == typeof(ProjectsMcpResolutionTraceItem) => Cast<T>(ReadResolutionTraceMetadata()),
                _ => [],
            };

            return new QueryResult<T>(items, items.Count, ETag: null);
        }
        catch (OperationCanceledException)
        {
            // Let FrontComposer collapse cancellation/timeout to its safe Canceled/Timeout category.
            throw;
        }
        catch (FrontComposerMcpException)
        {
            throw;
        }
        catch (HexalithProjectsApiException api)
        {
            // Map REST status to a FrontComposer safe failure category so the projection reader
            // renders the safe envelope (validation / hidden-equivalent denial / data unavailable).
            // The raw ProblemDetails body and exception text never cross the boundary.
            throw new FrontComposerMcpException(MapApiStatus(api.StatusCode));
        }
        catch
        {
            // Transport / deserialization / unexpected failures fail closed as data-unavailable.
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    private static FrontComposerMcpFailureCategory MapApiStatus(int statusCode)
        => statusCode switch
        {
            400 => FrontComposerMcpFailureCategory.ValidationFailed,
            401 or 403 or 404 => FrontComposerMcpFailureCategory.UnknownResource,
            502 or 503 or 504 => FrontComposerMcpFailureCategory.DownstreamFailed,
            _ => FrontComposerMcpFailureCategory.DownstreamFailed,
        };

    /// <summary>Reads the safe inventory resource.</summary>
    public async Task<IReadOnlyList<ProjectsMcpInventoryItem>> ReadInventoryAsync(int? take, CancellationToken cancellationToken)
    {
        ProjectListResponse response = await _client.ListProjectsAsync(
            Lifecycle.All,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);

        int limit = Bound(take, MaxRows);
        return response.Items
            .Take(limit)
            .Select(static item => new ProjectsMcpInventoryItem(
                item.ProjectId ?? string.Empty,
                item.Name ?? string.Empty,
                EnumCode(item.LifecycleState),
                item.UpdatedAt,
                EnumCode(item.Freshness.TrustState),
                item.Freshness.ProjectionWatermark,
                TenantScope,
                "Visible project inventory row from the generated Projects client.",
                PayloadExcluded: true))
            .ToArray();
    }

    /// <summary>Reads safe project detail metadata.</summary>
    public async Task<IReadOnlyList<ProjectsMcpProjectDetailItem>> ReadDetailAsync(string? projectId, CancellationToken cancellationToken)
    {
        Project project = await LoadProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        return
        [
            new ProjectsMcpProjectDetailItem(
                project.ProjectId ?? string.Empty,
                project.Name ?? string.Empty,
                project.Description,
                EnumCode(project.LifecycleState),
                project.UpdatedAt,
                project.References.Count,
                EnumCode(project.Freshness.TrustState),
                project.Freshness.ProjectionWatermark,
                TenantScope,
                "Project detail contains safe metadata and reference summaries only. " + PayloadExcludedExplanation,
                PayloadExcluded: true),
        ];
    }

    /// <summary>Reads safe operator diagnostic metadata.</summary>
    public async Task<IReadOnlyList<ProjectsMcpOperatorDiagnosticItem>> ReadOperatorDiagnosticAsync(string? projectId, int? auditLimit, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, auditLimit, cancellationToken).ConfigureAwait(false);
        return
        [
            new ProjectsMcpOperatorDiagnosticItem(
                diagnostic.ProjectId ?? string.Empty,
                diagnostic.Name ?? string.Empty,
                EnumCode(diagnostic.LifecycleState),
                diagnostic.References.Count,
                diagnostic.AuditTimeline.Count,
                EnumCode(diagnostic.Freshness.TrustState),
                diagnostic.Freshness.ProjectionWatermark,
                TenantScope,
                "Operator diagnostics preserve lifecycle, warnings, freshness, correlation/task/audit identifiers, and exclude payloads.",
                PayloadExcluded: true),
        ];
    }

    /// <summary>Reads safe reference health rows.</summary>
    public async Task<IReadOnlyList<ProjectsMcpReferenceHealthItem>> ReadReferenceHealthAsync(string? projectId, int? auditLimit, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, auditLimit, cancellationToken).ConfigureAwait(false);
        return diagnostic.References
            .Take(MaxRows)
            .Select(reference => new ProjectsMcpReferenceHealthItem(
                diagnostic.ProjectId ?? string.Empty,
                EnumCode(reference.ReferenceKind),
                reference.ReferenceId,
                EnumCode(reference.ReferenceState),
                reference.ReasonCode,
                reference.Freshness.ObservedAt,
                EvidenceFreshnessStateCode.Normalize(EnumCode(reference.Freshness.TrustState)),
                reference.Freshness.ProjectionWatermark,
                TenantScope,
                "Reference health row uses shared lifecycle/reference/reason vocabulary and safe identifiers.",
                PayloadExcluded: true))
            .ToArray();
    }

    /// <summary>Reads safe audit timeline rows.</summary>
    public async Task<IReadOnlyList<ProjectsMcpAuditTimelineItem>> ReadAuditTimelineAsync(string? projectId, int? auditLimit, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, auditLimit, cancellationToken).ConfigureAwait(false);
        return diagnostic.AuditTimeline
            .Take(Bound(auditLimit, MaxAuditLimit))
            .Select(row => new ProjectsMcpAuditTimelineItem(
                diagnostic.ProjectId ?? string.Empty,
                row.AuditEventId ?? string.Empty,
                row.OperationType ?? string.Empty,
                row.OccurredAt,
                row.CorrelationId ?? string.Empty,
                row.TaskId ?? string.Empty,
                row.ReferenceKind is null ? null : EnumCode(row.ReferenceKind.Value),
                row.ReferenceId,
                row.ReasonCode,
                TenantScope,
                "Audit row exposes safe correlation/task/audit evidence and excludes idempotency keys and command bodies.",
                PayloadExcluded: true))
            .ToArray();
    }

    /// <summary>Reads safe diagnostic export metadata.</summary>
    public async Task<IReadOnlyList<ProjectsMcpSafeDiagnosticExportItem>> ReadSafeDiagnosticExportAsync(string? projectId, int? auditLimit, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, auditLimit, cancellationToken).ConfigureAwait(false);
        return
        [
            new ProjectsMcpSafeDiagnosticExportItem(
                diagnostic.ProjectId ?? string.Empty,
                "projects.safe-diagnostic-export.v1",
                diagnostic.References.Count,
                diagnostic.AuditTimeline.Count,
                EnumCode(diagnostic.Freshness.TrustState),
                diagnostic.Freshness.ProjectionWatermark,
                TenantScope,
                "Safe diagnostic export summary confirms metadata-only payload exclusion. " + PayloadExcludedExplanation,
                PayloadExcluded: true),
        ];
    }

    /// <summary>Reads the warning queue.</summary>
    public async Task<IReadOnlyList<ProjectsMcpWarningQueueItem>> ReadWarningQueueAsync(int? take, CancellationToken cancellationToken)
    {
        ProjectsMcpWarningScan scan = await BuildWarningScanAsync(cancellationToken).ConfigureAwait(false);
        return LimitWarningRows(scan, take);
    }

    /// <summary>Reads the always-emitted warning scan summary.</summary>
    public async Task<IReadOnlyList<ProjectsMcpWarningScanSummaryItem>> ReadWarningScanSummaryAsync(CancellationToken cancellationToken)
    {
        ProjectsMcpWarningScan scan = await BuildWarningScanAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            new ProjectsMcpWarningScanSummaryItem(
                scan.ScannedProjectCount,
                scan.DiagnosticUnavailable,
                TenantScope,
                "Warning scan summary reports the deterministic selected-project cardinality and unavailable diagnostic count.",
                PayloadExcluded: true),
        ];
    }

    /// <summary>Reads operational dashboard counters.</summary>
    public async Task<IReadOnlyList<ProjectsMcpOperationalDashboardItem>> ReadOperationalDashboardAsync(CancellationToken cancellationToken)
    {
        ProjectListResponse list = await _client.ListProjectsAsync(
            Lifecycle.All,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        ProjectsMcpWarningScan scan = await ScanWarningsAsync(list.Items, cancellationToken).ConfigureAwait(false);
        return
        [
            new ProjectsMcpOperationalDashboardItem(
                list.Items.Count,
                list.Items.Count(static item => item.LifecycleState == ProjectLifecycleState.Active),
                list.Items.Count(static item => item.LifecycleState == ProjectLifecycleState.Archived),
                scan.Warnings.Select(static item => item.ProjectId).Distinct(StringComparer.Ordinal).Count(),
                scan.DiagnosticUnavailable,
                TenantScope,
                "Operational dashboard counters are derived from visible inventory and bounded diagnostics; unavailable diagnostics are reported as an explicit count.",
                PayloadExcluded: true),
        ];
    }

    private async Task<ProjectsMcpWarningScan> BuildWarningScanAsync(CancellationToken cancellationToken)
    {
        ProjectListResponse list = await _client.ListProjectsAsync(
            Lifecycle.All,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        return await ScanWarningsAsync(list.Items, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectsMcpWarningScan> ScanWarningsAsync(
        IEnumerable<ProjectListItem> visibleProjects,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProjectListItem> selectedProjects = ProjectWarningScanWindow.Select(visibleProjects);
        var warnings = new List<ProjectsMcpWarningQueueItem>();
        int diagnosticUnavailable = 0;
        foreach (ProjectListItem item in selectedProjects)
        {
            ProjectOperatorDiagnostic diagnostic;
            try
            {
                diagnostic = await _client.GetProjectOperatorDiagnosticsAsync(
                    item.ProjectId ?? string.Empty,
                    DefaultAuditLimit,
                    NewCorrelationId(),
                    ReadConsistencyClass.Eventually_consistent,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Story 5.8: a single unavailable bounded diagnostic must not drop the whole queue.
                // Preserve partial failure as an explicit safe count instead of hiding or aborting.
                diagnosticUnavailable++;
                continue;
            }

            warnings.AddRange(diagnostic.References
                .Where(static reference => reference.ReferenceState is not (ProjectReferenceSummaryReferenceState.Included or ProjectReferenceSummaryReferenceState.Pending))
                .Select(reference => new ProjectsMcpWarningQueueItem(
                    diagnostic.ProjectId ?? string.Empty,
                    diagnostic.Name ?? string.Empty,
                    EnumCode(diagnostic.LifecycleState),
                    EnumCode(reference.ReferenceKind),
                    reference.ReferenceId,
                    EnumCode(reference.ReferenceState),
                    reference.ReasonCode,
                    reference.Freshness.ObservedAt,
                    EvidenceFreshnessStateCode.Normalize(EnumCode(reference.Freshness.TrustState)),
                    reference.Freshness.ProjectionWatermark,
                    0,
                    TenantScope,
                    "Warning queue row is bounded and contains only safe reference metadata.",
                    PayloadExcluded: true)));
        }

        IReadOnlyList<ProjectsMcpWarningQueueItem> orderedWarnings = warnings
            .OrderBy(static item => item.ProjectId, StringComparer.Ordinal)
            .ThenBy(static item => item.ReferenceKind, StringComparer.Ordinal)
            .ThenBy(static item => item.ReferenceId, StringComparer.Ordinal)
            .Select(item => item with { DiagnosticUnavailable = diagnosticUnavailable })
            .ToArray();
        return new ProjectsMcpWarningScan(
            orderedWarnings,
            selectedProjects.Count,
            diagnosticUnavailable);
    }

    private static IReadOnlyList<ProjectsMcpWarningQueueItem> LimitWarningRows(
        ProjectsMcpWarningScan scan,
        int? take)
        => scan.Warnings
            .Take(Bound(take, MaxRows))
            .ToArray();

    /// <summary>MCP wire lifecycle states, sourced from the canonical FrontComposer set (AC8 / no drift).</summary>
    private static readonly string McpWireLifecycleStates = string.Join(",", McpLifecycleStateNames.Canonical);

    /// <summary>Web command lifecycle labels, sourced from the shared maintenance constants (AC8 / no drift).</summary>
    private static readonly string WebLifecycleLabels = string.Join(
        ",",
        ProjectMaintenanceCommandLifecycleStates.Idle,
        ProjectMaintenanceCommandLifecycleStates.Submitting,
        ProjectMaintenanceCommandLifecycleStates.Acknowledged,
        ProjectMaintenanceCommandLifecycleStates.Syncing,
        ProjectMaintenanceCommandLifecycleStates.Confirmed,
        ProjectMaintenanceCommandLifecycleStates.Rejected);

    /// <summary>Returns static safe maintenance action preview metadata.</summary>
    public static IReadOnlyList<ProjectsMcpMaintenanceActionItem> ReadMaintenanceActions()
        => ProjectsMcpDescriptors.MaintenanceActionNames
            .Select(static action => new ProjectsMcpMaintenanceActionItem(
                action,
                McpWireLifecycleStates,
                WebLifecycleLabels,
                RequiresProjectId: true,
                RequiresConfirmation: action is not ProjectMaintenanceActions.Reevaluate,
                RequiresDryRunEvidence: true,
                RequiresIdempotencyKey: action is not ProjectMaintenanceActions.Reevaluate,
                TenantScope,
                "Maintenance action descriptor only; state changes require explicit tool invocation and confirmation evidence.",
                PayloadExcluded: true))
            .ToArray();

    /// <summary>Returns static resolution trace metadata for the plain resource catalog.</summary>
    public static IReadOnlyList<ProjectsMcpResolutionTraceItem> ReadResolutionTraceMetadata()
        =>
        [
            new ProjectsMcpResolutionTraceItem(
                "conversation|attachments",
                null,
                "metadata_only_descriptor",
                0,
                0,
                DateTimeOffset.UnixEpoch,
                null,
                TenantScope,
                "Resolution trace is available through generated conversation or attachment query modes.",
                PayloadExcluded: true),
        ];

    private async Task<Project> LoadProjectAsync(string? projectId, CancellationToken cancellationToken)
    {
        string id = string.IsNullOrWhiteSpace(projectId)
            ? await FirstVisibleProjectIdAsync(cancellationToken).ConfigureAwait(false)
            : projectId;
        return await _client.GetProjectAsync(
            id,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectOperatorDiagnostic> LoadDiagnosticAsync(string? projectId, int? auditLimit, CancellationToken cancellationToken)
    {
        string id = string.IsNullOrWhiteSpace(projectId)
            ? await FirstVisibleProjectIdAsync(cancellationToken).ConfigureAwait(false)
            : projectId;
        return await _client.GetProjectOperatorDiagnosticsAsync(
            id,
            Bound(auditLimit, MaxAuditLimit),
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> FirstVisibleProjectIdAsync(CancellationToken cancellationToken)
    {
        ProjectListResponse response = await _client.ListProjectsAsync(
            Lifecycle.All,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        return response.Items.FirstOrDefault()?.ProjectId ?? string.Empty;
    }

    private static IReadOnlyList<T> Cast<T>(IEnumerable<object> items)
        => items.Cast<T>().ToArray();

    private static int Bound(int? requested, int maximum)
        => Math.Max(1, Math.Min(requested.GetValueOrDefault(DefaultAuditLimit), maximum));

    private static string NewCorrelationId()
        => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    private static string EnumCode<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value.ToString().ToLower(CultureInfo.InvariantCulture);
}
