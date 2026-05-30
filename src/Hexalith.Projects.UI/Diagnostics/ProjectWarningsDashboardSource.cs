// <copyright file="ProjectWarningsDashboardSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Rendering;

using ContractDiagnostic = Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic;
using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;
using GeneratedListItem = Hexalith.Projects.Client.Generated.ProjectListItem;

/// <summary>
/// Generated-client backed source for the warnings queue and operational dashboard.
/// </summary>
public sealed class ProjectWarningsDashboardSource(IClient client) : IProjectWarningsDashboardSource
{
    private const int DiagnosticAuditLimit = 25;

    /// <inheritdoc />
    public async Task<ProjectWarningsDashboardLoadResult> LoadAsync(
        ProjectLifecycle? lifecycle,
        CancellationToken cancellationToken)
    {
        string correlationId = Guid.NewGuid().ToString("N");
        ProjectInventoryRowProjection[] projects;
        try
        {
            ProjectListResponse response = await client.ListProjectsAsync(
                ToGeneratedLifecycle(lifecycle),
                correlationId,
                ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);
            projects = response.Items
                .Select(item => ToProjection(item, response.Freshness))
                .ToArray();
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 404)
        {
            return ProjectWarningsDashboardLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectWarningsDashboardLoadResult.FromFeedback(
                ProjectConsoleFeedback.Warning("data_unavailable", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectWarningsDashboardLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("validation_error", correlationId));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectWarningsDashboardLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("warnings_dashboard_query_failed", correlationId));
        }
        catch (Exception)
        {
            return ProjectWarningsDashboardLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("warnings_dashboard_query_failed", correlationId));
        }

        var queueItems = new List<ProjectWarningQueueItemProjection>();
        int diagnosticUnavailableCount = 0;
        foreach (ProjectInventoryRowProjection project in projects)
        {
            try
            {
                GeneratedDiagnostic generatedDiagnostic = await client.GetProjectOperatorDiagnosticsAsync(
                    project.ProjectId,
                    DiagnosticAuditLimit,
                    correlationId,
                    ReadConsistencyClass.Eventually_consistent,
                    cancellationToken).ConfigureAwait(false);
                ContractDiagnostic diagnostic = ProjectGeneratedContractMapper.ToContract(generatedDiagnostic);
                queueItems.AddRange(ProjectWarningsDashboardMapper.BuildQueueItems(project, diagnostic));
            }
            catch (HexalithProjectsApiException ex)
            {
                diagnosticUnavailableCount++;
                queueItems.Add(ProjectWarningsDashboardMapper.DiagnosticUnavailableItem(
                    project,
                    MapDiagnosticFailure(ex.StatusCode)));
            }
            catch (Exception)
            {
                diagnosticUnavailableCount++;
                queueItems.Add(ProjectWarningsDashboardMapper.DiagnosticUnavailableItem(
                    project,
                    "diagnostic_query_failed"));
            }
        }

        ProjectOperationalDashboardProjection dashboard =
            ProjectWarningsDashboardMapper.BuildDashboard(projects, queueItems, diagnosticUnavailableCount);
        return ProjectWarningsDashboardLoadResult.FromRows(projects, queueItems, dashboard);
    }

    private static string MapDiagnosticFailure(int statusCode)
        => statusCode switch
        {
            400 => "validation_error",
            404 => "safe_denial",
            503 => "data_unavailable",
            _ => "diagnostic_query_failed",
        };

    private static ProjectInventoryRowProjection ToProjection(GeneratedListItem item, FreshnessMetadata listFreshness)
    {
        FreshnessMetadata freshness = item.Freshness ?? listFreshness;
        return new ProjectInventoryRowProjection
        {
            Id = item.ProjectId ?? string.Empty,
            ProjectId = item.ProjectId ?? string.Empty,
            Name = item.Name ?? string.Empty,
            Lifecycle = ToContractLifecycle(item.LifecycleState),
            WarningSummary = ProjectInventoryRowProjection.WarningSummaryUnavailable,
            UpdatedAt = item.UpdatedAt,
            CreatedAt = item.CreatedAt,
            TenantScope = "server-derived tenant",
            FreshnessTrustState = ProjectGeneratedContractMapper.EnumCode(freshness.TrustState),
            ProjectionWatermark = freshness.ProjectionWatermark,
            Stale = freshness.Stale,
        };
    }

    private static Lifecycle? ToGeneratedLifecycle(ProjectLifecycle? lifecycle)
        => lifecycle switch
        {
            ProjectLifecycle.Active => Lifecycle.Active,
            ProjectLifecycle.Archived => Lifecycle.Archived,
            _ => null,
        };

    private static ProjectLifecycle ToContractLifecycle(ProjectLifecycleState lifecycle)
        => lifecycle switch
        {
            ProjectLifecycleState.Active => ProjectLifecycle.Active,
            ProjectLifecycleState.Archived => ProjectLifecycle.Archived,
            _ => ProjectLifecycle.Archived,
        };
}
