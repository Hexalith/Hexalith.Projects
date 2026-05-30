// <copyright file="ProjectOperatorDiagnosticSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.UI.Rendering;

using GeneratedDiagnostic = Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic;

/// <summary>
/// Generated-client backed source for project-scoped operator diagnostics.
/// </summary>
public sealed class ProjectOperatorDiagnosticSource(IClient client) : IProjectOperatorDiagnosticSource
{
    /// <inheritdoc />
    public async Task<ProjectDiagnosticLoadResult> GetProjectDiagnosticsAsync(
        string projectId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        string correlationId = Guid.NewGuid().ToString("N");
        try
        {
            GeneratedDiagnostic diagnostic = await client.GetProjectOperatorDiagnosticsAsync(
                projectId,
                auditLimit: 25,
                x_Correlation_Id: correlationId,
                x_Hexalith_Freshness: ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);

            return ProjectDiagnosticLoadResult.FromDiagnostic(
                ProjectGeneratedContractMapper.ToContract(diagnostic),
                "server-derived tenant",
                ProjectConsoleModes.ReadOnly);
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.FailClosed("safe_denial", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Warning("data_unavailable", correlationId));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("validation_error", correlationId));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("diagnostic_query_failed", correlationId));
        }
        catch (Exception)
        {
            // Transport/timeout/deserialization failures must not crash the Blazor circuit or echo raw
            // exception text; collapse to the same safe reason code as an unclassified API failure.
            return ProjectDiagnosticLoadResult.FromFeedback(
                ProjectConsoleFeedback.Error("diagnostic_query_failed", correlationId));
        }
    }
}
