// <copyright file="ProjectAuditTimelineSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.UI.Rendering;

using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;

/// <summary>
/// Generated-client backed source for bounded audit timeline reloads.
/// </summary>
public sealed class ProjectAuditTimelineSource(IClient client) : IProjectAuditTimelineSource
{
    /// <summary>Gets the default audit limit used by Story 5.2 diagnostics.</summary>
    public const int DefaultAuditLimit = 25;

    /// <summary>Gets the maximum audit limit supported by the operator diagnostic endpoint.</summary>
    public const int MaximumAuditLimit = 100;

    /// <inheritdoc />
    public async Task<ProjectAuditTimelineLoadResult> GetAuditTimelineAsync(
        string projectId,
        int? auditLimit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        string correlationId = Guid.NewGuid().ToString("N");
        int boundedLimit = BoundAuditLimit(auditLimit);
        try
        {
            GeneratedDiagnostic diagnostic = await client.GetProjectOperatorDiagnosticsAsync(
                projectId,
                boundedLimit,
                correlationId,
                ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);
            Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic contract =
                ProjectGeneratedContractMapper.ToContract(diagnostic);
            return ProjectAuditTimelineLoadResult.FromRows(contract.AuditTimeline, contract.Freshness);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            return ProjectAuditTimelineLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectAuditTimelineLoadResult.FromFeedback(
                ProjectConsoleFeedback.Warning("data_unavailable", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectAuditTimelineLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("validation_error", correlationId));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectAuditTimelineLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("audit_timeline_query_failed", correlationId));
        }
        catch (Exception)
        {
            return ProjectAuditTimelineLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("audit_timeline_query_failed", correlationId));
        }
    }

    /// <summary>Bounds an audit limit to the endpoint-supported range.</summary>
    public static int BoundAuditLimit(int? auditLimit)
        => auditLimit is null or < 1
            ? DefaultAuditLimit
            : Math.Min(auditLimit.Value, MaximumAuditLimit);
}
