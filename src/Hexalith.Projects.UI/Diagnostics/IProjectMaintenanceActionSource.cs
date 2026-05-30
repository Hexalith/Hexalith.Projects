// <copyright file="IProjectMaintenanceActionSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Executes metadata-only maintenance actions through existing Projects clients.</summary>
public interface IProjectMaintenanceActionSource
{
    /// <summary>Executes a confirmed maintenance action.</summary>
    /// <param name="request">The action request.</param>
    /// <param name="lifecycleProgress">
    /// Optional reporter for intermediate command-lifecycle states
    /// (<c>Acknowledged(202)</c> after the 202 AcceptedCommand, then <c>Syncing</c> while the
    /// audit projection is polled) so the 202 acknowledgement is observably distinct from the
    /// final projection/audit confirmation.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The safe execution outcome.</returns>
    Task<ProjectMaintenanceActionExecutionResult> ExecuteAsync(
        ProjectMaintenanceActionExecutionRequest request,
        IProgress<string>? lifecycleProgress,
        CancellationToken cancellationToken);
}

/// <summary>Safe metadata-only maintenance execution request.</summary>
/// <param name="ProjectId">The Project identifier.</param>
/// <param name="Action">The maintenance action name.</param>
/// <param name="ReferenceKind">The optional reference kind.</param>
/// <param name="ReferenceId">The optional reference identifier.</param>
/// <param name="ReferenceDisplayLabel">The optional safe reference display label.</param>
/// <param name="ReplacementConfirmed">Whether an explicit replacement confirmation was captured.</param>
/// <param name="TransientFolderId">The transient folder identifier required by Folders ACL validation.</param>
/// <param name="TransientWorkspaceId">The transient workspace identifier required by Folders ACL validation.</param>
/// <param name="TransientFilePath">The transient workspace-relative path required by Folders ACL validation.</param>
/// <param name="ExpectedAuditOperation">The expected audit operation used for confirmation polling.</param>
public sealed record ProjectMaintenanceActionExecutionRequest(
    string ProjectId,
    string Action,
    string? ReferenceKind,
    string? ReferenceId,
    string? ReferenceDisplayLabel,
    bool ReplacementConfirmed,
    string? TransientFolderId,
    string? TransientWorkspaceId,
    string? TransientFilePath,
    string ExpectedAuditOperation);

/// <summary>Safe metadata-only maintenance execution result.</summary>
/// <param name="Succeeded">Whether the final projection/audit confirmation was observed.</param>
/// <param name="LifecycleState">The final command lifecycle state.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="TaskId">The task identifier.</param>
/// <param name="AuditEventId">The confirmed audit event identifier.</param>
/// <param name="FeedbackCode">The safe feedback code.</param>
public sealed record ProjectMaintenanceActionExecutionResult(
    bool Succeeded,
    string LifecycleState,
    string? CorrelationId,
    string? TaskId,
    string? AuditEventId,
    string? FeedbackCode)
{
    /// <summary>Creates a confirmed result whose audit evidence was observed in the Projects audit timeline.</summary>
    public static ProjectMaintenanceActionExecutionResult Confirmed(string correlationId, string taskId, string? auditEventId)
        => new(true, ProjectMaintenanceCommandLifecycleStates.Confirmed, correlationId, taskId, auditEventId, null);

    /// <summary>
    /// Creates an accepted result for a Conversations-owned action whose audit evidence lives in the
    /// Conversations assignment timeline rather than the Projects audit timeline; confirmation is keyed
    /// on the 202 AcceptedCommand acknowledgement, so no Projects audit event id is available.
    /// </summary>
    public static ProjectMaintenanceActionExecutionResult Accepted(string correlationId, string taskId, string feedbackCode)
        => new(true, ProjectMaintenanceCommandLifecycleStates.Acknowledged, correlationId, taskId, null, feedbackCode);

    /// <summary>
    /// Creates a result for the read-only re-evaluate recompute. Re-evaluate is not a state-changing
    /// command, so it produces no audit event and reports a distinct <c>diagnostics_reloaded</c> feedback
    /// code rather than a committed-mutation outcome.
    /// </summary>
    public static ProjectMaintenanceActionExecutionResult Reloaded(string correlationId, string taskId)
        => new(true, ProjectMaintenanceCommandLifecycleStates.Confirmed, correlationId, taskId, null, "diagnostics_reloaded");

    /// <summary>Creates a rejected result.</summary>
    public static ProjectMaintenanceActionExecutionResult Rejected(string? correlationId, string? taskId, string feedbackCode)
        => new(false, ProjectMaintenanceCommandLifecycleStates.Rejected, correlationId, taskId, null, feedbackCode);
}

/// <summary>Generated-client backed source for maintenance actions.</summary>
public sealed class ProjectMaintenanceActionSource(Hexalith.Projects.Client.Generated.IClient client) : IProjectMaintenanceActionSource
{
    private const int ConfirmationAttempts = 5;

    private readonly Hexalith.Projects.Client.Generated.IClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<ProjectMaintenanceActionExecutionResult> ExecuteAsync(
        ProjectMaintenanceActionExecutionRequest request,
        IProgress<string>? lifecycleProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string correlationId = Guid.NewGuid().ToString("N");
        string taskId = "maintenance-" + correlationId[..16];
        string idempotencyKey = "maintenance-" + Guid.NewGuid().ToString("N");

        try
        {
            if (string.Equals(request.Action, ProjectMaintenanceActions.Archive, StringComparison.Ordinal))
            {
                Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.ArchiveProjectAsync(
                    request.ProjectId,
                    idempotencyKey,
                    correlationId,
                    taskId,
                    new Hexalith.Projects.Client.Generated.ArchiveProjectRequest
                    {
                        ArchiveIntent = Hexalith.Projects.Client.Generated.ArchiveProjectRequestArchiveIntent.Archive,
                        RequestSchemaVersion = Hexalith.Projects.Client.Generated.ArchiveProjectRequestRequestSchemaVersion.V1,
                    },
                    cancellationToken).ConfigureAwait(false);

                return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(request.Action, ProjectMaintenanceActions.Restore, StringComparison.Ordinal))
            {
                Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.RestoreProjectAsync(
                    request.ProjectId,
                    idempotencyKey,
                    correlationId,
                    taskId,
                    new Hexalith.Projects.Client.Generated.RestoreProjectRequest
                    {
                        RequestSchemaVersion = Hexalith.Projects.Client.Generated.RestoreProjectRequestRequestSchemaVersion.V1,
                        RestoreIntent = Hexalith.Projects.Client.Generated.RestoreProjectRequestRestoreIntent.Restore,
                    },
                    cancellationToken).ConfigureAwait(false);

                return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(request.Action, ProjectMaintenanceActions.Reevaluate, StringComparison.Ordinal))
            {
                // Re-evaluate is a read-only recompute over the existing RefreshProjectContext query. It
                // submits no command, persists no trace/score/state, and emits no audit event.
                await _client.RefreshProjectContextAsync(
                    request.ProjectId,
                    correlationId,
                    Hexalith.Projects.Client.Generated.ReadConsistencyClass.Eventually_consistent,
                    cancellationToken).ConfigureAwait(false);
                return ProjectMaintenanceActionExecutionResult.Reloaded(correlationId, taskId);
            }

            if (string.Equals(request.Action, ProjectMaintenanceActions.Relink, StringComparison.Ordinal))
            {
                return await SubmitRelinkAsync(request, idempotencyKey, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(request.Action, ProjectMaintenanceActions.Unlink, StringComparison.Ordinal))
            {
                return await SubmitUnlinkAsync(request, idempotencyKey, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
            }

            return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "unsupported_operation");
        }
        catch (Hexalith.Projects.Client.Generated.HexalithProjectsApiException ex)
        {
            return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, MapStatusCode(ex.StatusCode));
        }
        catch (Exception)
        {
            return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "maintenance_action_failed");
        }
    }

    private async Task<ProjectMaintenanceActionExecutionResult> SubmitRelinkAsync(
        ProjectMaintenanceActionExecutionRequest request,
        string idempotencyKey,
        string correlationId,
        string taskId,
        IProgress<string>? lifecycleProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceKind) || string.IsNullOrWhiteSpace(request.ReferenceId))
        {
            return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "invalid_reference");
        }

        if (string.Equals(request.ReferenceKind, "folder", StringComparison.Ordinal))
        {
            Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.SetProjectFolderAsync(
                request.ProjectId,
                idempotencyKey,
                correlationId,
                taskId,
                new Hexalith.Projects.Client.Generated.SetProjectFolderRequest
                {
                    RequestSchemaVersion = Hexalith.Projects.Client.Generated.SetProjectFolderRequestRequestSchemaVersion.V1,
                    Operation = Hexalith.Projects.Client.Generated.SetProjectFolderRequestOperation.Set,
                    ProjectId = request.ProjectId,
                    FolderId = request.ReferenceId,
                    FolderMetadata = new Hexalith.Projects.Client.Generated.ProjectFolderMetadata
                    {
                        DisplayName = SafeDisplayName(request.ReferenceDisplayLabel, request.ReferenceId),
                    },
                    ReplacementConfirmed = request.ReplacementConfirmed,
                },
                cancellationToken).ConfigureAwait(false);

            return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(request.ReferenceKind, "memory", StringComparison.Ordinal))
        {
            Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.LinkMemoryAsync(
                request.ProjectId,
                request.ReferenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new Hexalith.Projects.Client.Generated.LinkMemoryRequest
                {
                    RequestSchemaVersion = Hexalith.Projects.Client.Generated.LinkMemoryRequestRequestSchemaVersion.V1,
                    Operation = Hexalith.Projects.Client.Generated.LinkMemoryRequestOperation.Link,
                    ProjectId = request.ProjectId,
                    MemoryReferenceId = request.ReferenceId,
                    MemoryMetadata = new Hexalith.Projects.Client.Generated.ProjectMemoryReferenceMetadata
                    {
                        DisplayName = SafeDisplayName(request.ReferenceDisplayLabel, request.ReferenceId),
                    },
                },
                cancellationToken).ConfigureAwait(false);

            return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(request.ReferenceKind, "file", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(request.TransientFolderId)
                || string.IsNullOrWhiteSpace(request.TransientWorkspaceId)
                || string.IsNullOrWhiteSpace(request.TransientFilePath))
            {
                return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "transient_validation_required");
            }

            Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.LinkFileReferenceAsync(
                request.ProjectId,
                request.ReferenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new Hexalith.Projects.Client.Generated.LinkFileReferenceRequest
                {
                    RequestSchemaVersion = Hexalith.Projects.Client.Generated.LinkFileReferenceRequestRequestSchemaVersion.V1,
                    Operation = Hexalith.Projects.Client.Generated.LinkFileReferenceRequestOperation.Link,
                    ProjectId = request.ProjectId,
                    FileReferenceId = request.ReferenceId,
                    FolderId = request.TransientFolderId,
                    WorkspaceId = request.TransientWorkspaceId,
                    FilePath = request.TransientFilePath,
                    FileMetadata = new Hexalith.Projects.Client.Generated.ProjectFileReferenceMetadata
                    {
                        DisplayName = SafeDisplayName(request.ReferenceDisplayLabel, request.ReferenceId),
                    },
                },
                cancellationToken).ConfigureAwait(false);

            return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
        }

        return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "transient_validation_required");
    }

    private async Task<ProjectMaintenanceActionExecutionResult> SubmitUnlinkAsync(
        ProjectMaintenanceActionExecutionRequest request,
        string idempotencyKey,
        string correlationId,
        string taskId,
        IProgress<string>? lifecycleProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceKind) || string.IsNullOrWhiteSpace(request.ReferenceId))
        {
            return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "invalid_reference");
        }

        if (string.Equals(request.ReferenceKind, "conversation", StringComparison.Ordinal))
        {
            Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.UnlinkProjectConversationAsync(
                request.ProjectId,
                request.ReferenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new Hexalith.Projects.Client.Generated.UnlinkProjectConversationRequest
                {
                    RequestSchemaVersion = Hexalith.Projects.Client.Generated.UnlinkProjectConversationRequestRequestSchemaVersion.V1,
                    Operation = Hexalith.Projects.Client.Generated.UnlinkProjectConversationRequestOperation.Unlink,
                    UnlinkIntent = Hexalith.Projects.Client.Generated.UnlinkProjectConversationRequestUnlinkIntent.Clear,
                    ProjectId = request.ProjectId,
                    ConversationId = request.ReferenceId,
                },
                cancellationToken).ConfigureAwait(false);

            // Conversation unlink is Conversations-owned via the assignment ACL: no Projects audit row
            // (e.g. "conversation.unlinked") is ever emitted by the Projects audit timeline, so confirmation
            // is keyed on the 202 AcceptedCommand acknowledgement rather than polling the Projects audit.
            string ackCorrelationId = ResolveId(accepted?.CorrelationId, correlationId);
            string ackTaskId = ResolveId(accepted?.TaskId, taskId);
            lifecycleProgress?.Report(ProjectMaintenanceCommandLifecycleStates.Acknowledged);
            return ProjectMaintenanceActionExecutionResult.Accepted(ackCorrelationId, ackTaskId, "conversation_unlink_accepted");
        }

        if (string.Equals(request.ReferenceKind, "file", StringComparison.Ordinal))
        {
            Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.UnlinkFileReferenceAsync(
                request.ProjectId,
                request.ReferenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new Hexalith.Projects.Client.Generated.UnlinkFileReferenceRequest
                {
                    RequestSchemaVersion = Hexalith.Projects.Client.Generated.UnlinkFileReferenceRequestRequestSchemaVersion.V1,
                    Operation = Hexalith.Projects.Client.Generated.UnlinkFileReferenceRequestOperation.Unlink,
                    UnlinkIntent = Hexalith.Projects.Client.Generated.UnlinkFileReferenceRequestUnlinkIntent.RemoveReference,
                    ProjectId = request.ProjectId,
                    FileReferenceId = request.ReferenceId,
                },
                cancellationToken).ConfigureAwait(false);

            return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(request.ReferenceKind, "memory", StringComparison.Ordinal))
        {
            Hexalith.Projects.Client.Generated.AcceptedCommand accepted = await _client.UnlinkMemoryAsync(
                request.ProjectId,
                request.ReferenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new Hexalith.Projects.Client.Generated.UnlinkMemoryRequest
                {
                    RequestSchemaVersion = Hexalith.Projects.Client.Generated.UnlinkMemoryRequestRequestSchemaVersion.V1,
                    Operation = Hexalith.Projects.Client.Generated.UnlinkMemoryRequestOperation.Unlink,
                    UnlinkIntent = Hexalith.Projects.Client.Generated.UnlinkMemoryRequestUnlinkIntent.RemoveReference,
                    ProjectId = request.ProjectId,
                    MemoryReferenceId = request.ReferenceId,
                },
                cancellationToken).ConfigureAwait(false);

            return await AcknowledgeAndConfirmAsync(accepted, request.ProjectId, request.ExpectedAuditOperation, correlationId, taskId, lifecycleProgress, cancellationToken).ConfigureAwait(false);
        }

        return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "unsupported_reference_kind");
    }

    /// <summary>
    /// Surfaces the 202 <c>Acknowledged</c> lifecycle state from the AcceptedCommand, then the
    /// <c>Syncing</c> state, then polls the operator audit timeline for the expected evidence. The
    /// server-acknowledged correlation/task identifiers are preferred over the locally generated ones so
    /// confirmation matching follows the accepted command.
    /// </summary>
    private async Task<ProjectMaintenanceActionExecutionResult> AcknowledgeAndConfirmAsync(
        Hexalith.Projects.Client.Generated.AcceptedCommand? accepted,
        string projectId,
        string expectedAuditOperation,
        string fallbackCorrelationId,
        string fallbackTaskId,
        IProgress<string>? lifecycleProgress,
        CancellationToken cancellationToken)
    {
        string correlationId = ResolveId(accepted?.CorrelationId, fallbackCorrelationId);
        string taskId = ResolveId(accepted?.TaskId, fallbackTaskId);

        lifecycleProgress?.Report(ProjectMaintenanceCommandLifecycleStates.Acknowledged);
        lifecycleProgress?.Report(ProjectMaintenanceCommandLifecycleStates.Syncing);

        return await ConfirmAsync(projectId, expectedAuditOperation, correlationId, taskId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectMaintenanceActionExecutionResult> ConfirmAsync(
        string projectId,
        string expectedAuditOperation,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < ConfirmationAttempts; attempt++)
        {
            // Space polling attempts across the 202-to-projection eventual-consistency window so a
            // slightly-delayed audit row is still observed before reporting a safe unavailable outcome.
            if (attempt > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
            }

            Hexalith.Projects.Client.Generated.ProjectOperatorDiagnostic diagnostic = await _client.GetProjectOperatorDiagnosticsAsync(
                projectId,
                auditLimit: 25,
                x_Correlation_Id: correlationId,
                x_Hexalith_Freshness: Hexalith.Projects.Client.Generated.ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);

            ProjectOperatorDiagnostic contract = ProjectGeneratedContractMapper.ToContract(diagnostic);
            ProjectOperatorAuditTimelineItem? audit = contract.AuditTimeline.FirstOrDefault(item =>
                string.Equals(item.OperationType, expectedAuditOperation, StringComparison.Ordinal)
                && string.Equals(item.CorrelationId, correlationId, StringComparison.Ordinal));

            if (audit is not null)
            {
                return ProjectMaintenanceActionExecutionResult.Confirmed(correlationId, taskId, audit.AuditEventId);
            }
        }

        return ProjectMaintenanceActionExecutionResult.Rejected(correlationId, taskId, "audit_confirmation_unavailable");
    }

    private static string ResolveId(string? acknowledged, string fallback)
        => string.IsNullOrWhiteSpace(acknowledged) ? fallback : acknowledged;

    private static string MapStatusCode(int statusCode)
        => statusCode switch
        {
            400 => "validation_error",
            404 => "safe_denial",
            409 => "idempotency_conflict",
            503 => "data_unavailable",
            _ => "maintenance_action_failed",
        };

    private static string SafeDisplayName(string? displayName, string referenceId)
        => string.IsNullOrWhiteSpace(displayName) ? referenceId : displayName.Trim();
}
