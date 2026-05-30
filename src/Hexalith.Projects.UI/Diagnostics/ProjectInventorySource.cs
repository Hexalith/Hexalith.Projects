// <copyright file="ProjectInventorySource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Rendering;

using GeneratedListItem = Hexalith.Projects.Client.Generated.ProjectListItem;

/// <summary>
/// Generated-client backed source for the Projects inventory.
/// </summary>
public sealed class ProjectInventorySource(IClient client) : IProjectInventorySource
{
    /// <inheritdoc />
    public async Task<ProjectInventoryLoadResult> ListProjectsAsync(
        ProjectLifecycle? lifecycle,
        CancellationToken cancellationToken)
    {
        string correlationId = Guid.NewGuid().ToString("N");
        try
        {
            ProjectListResponse response = await client.ListProjectsAsync(
                ToGeneratedLifecycle(lifecycle),
                correlationId,
                ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);

            ProjectInventoryRowProjection[] rows = response.Items
                .Select(item => ToProjection(item, response.Freshness))
                .ToArray();

            return ProjectInventoryLoadResult.FromRows(rows);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 404)
        {
            return ProjectInventoryLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectInventoryLoadResult.FromFeedback(
                ProjectConsoleFeedback.Warning("data_unavailable", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectInventoryLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("validation_error", correlationId));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectInventoryLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("inventory_query_failed", correlationId));
        }
        catch (Exception)
        {
            // Transport/timeout/deserialization failures must not crash the Blazor circuit or echo raw
            // exception text; collapse to the same safe reason code as an unclassified API failure.
            return ProjectInventoryLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("inventory_query_failed", correlationId));
        }
    }

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
