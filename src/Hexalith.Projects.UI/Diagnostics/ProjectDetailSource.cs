// <copyright file="ProjectDetailSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.UI.Rendering;

using ContractDiagnostic = Hexalith.Projects.Contracts.Models.ProjectOperatorDiagnostic;
using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;

/// <summary>
/// Generated-client backed source for the read-only Project detail inspector.
/// </summary>
public sealed class ProjectDetailSource(IClient client) : IProjectDetailSource
{
    /// <inheritdoc />
    public async Task<ProjectDetailLoadResult> GetProjectDetailAsync(
        string projectId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        string correlationId = Guid.NewGuid().ToString("N");
        ContractDiagnostic detail;
        try
        {
            Project project = await client.GetProjectAsync(
                projectId,
                correlationId,
                ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);
            detail = ProjectGeneratedContractMapper.ToContract(project);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 404)
        {
            return ProjectDetailLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectDetailLoadResult.FromFeedback(
                ProjectConsoleFeedback.Warning("data_unavailable", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectDetailLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("validation_error", correlationId));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectDetailLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("detail_query_failed", correlationId));
        }
        catch (Exception)
        {
            // Transport/timeout/deserialization failures must not crash the Blazor circuit or echo raw
            // exception text; collapse to the same safe reason code as an unclassified API failure.
            return ProjectDetailLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("detail_query_failed", correlationId));
        }

        ProjectConsoleFeedback? diagnosticFeedback = null;
        ContractDiagnostic? diagnostic = null;
        try
        {
            GeneratedDiagnostic generatedDiagnostic = await client.GetProjectOperatorDiagnosticsAsync(
                projectId,
                auditLimit: 25,
                x_Correlation_Id: correlationId,
                x_Hexalith_Freshness: ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);
            diagnostic = ProjectGeneratedContractMapper.ToContract(generatedDiagnostic);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 404)
        {
            diagnosticFeedback = ProjectConsoleFeedback.FailClosed("safe_denial", correlationId);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            diagnosticFeedback = ProjectConsoleFeedback.Warning("data_unavailable", correlationId);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            diagnosticFeedback = ProjectConsoleFeedback.Error("validation_error", correlationId);
        }
        catch (HexalithProjectsApiException)
        {
            diagnosticFeedback = ProjectConsoleFeedback.Error("diagnostic_query_failed", correlationId);
        }
        catch (Exception)
        {
            // Bounded diagnostics are non-blocking; any transport/timeout failure degrades to safe
            // feedback while the base detail stays rendered.
            diagnosticFeedback = ProjectConsoleFeedback.Error("diagnostic_query_failed", correlationId);
        }

        return ProjectDetailLoadResult.FromDetail(
            ProjectGeneratedContractMapper.Merge(detail, diagnostic),
            diagnosticFeedback);
    }
}
