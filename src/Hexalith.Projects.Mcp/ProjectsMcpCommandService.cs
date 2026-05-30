// <copyright file="ProjectsMcpCommandService.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Projects.Client.Generated;

/// <summary>
/// FrontComposer command service that routes approved Projects MCP tools to the generated client.
/// </summary>
public sealed class ProjectsMcpCommandService(IClient client) : ICommandServiceWithLifecycle
{
    private readonly IClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(command, null, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        if (command is not ProjectsMcpMaintenanceCommand maintenance)
        {
            throw SafeValidation();
        }

        Validate(maintenance);
        AcceptedCommand accepted = await SubmitAsync(maintenance, cancellationToken).ConfigureAwait(false);
        string messageId = Resolve(accepted.TaskId, maintenance.CommandId);
        string correlationId = Resolve(accepted.CorrelationId, maintenance.CorrelationId);

        onLifecycleChange?.Invoke(CommandLifecycleState.Acknowledged, messageId);
        if (string.Equals(maintenance.Action, "reevaluate", StringComparison.Ordinal))
        {
            onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, messageId);
        }
        else
        {
            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, messageId);
        }

        return new CommandResult(
            MessageId: messageId,
            Status: "Accepted",
            CorrelationId: correlationId);
    }

    private async Task<AcceptedCommand> SubmitAsync(ProjectsMcpMaintenanceCommand command, CancellationToken cancellationToken)
    {
        string correlationId = Resolve(command.CorrelationId, Guid.NewGuid().ToString("N"));
        string taskId = Resolve(command.CommandId, "mcp-" + correlationId[..Math.Min(16, correlationId.Length)]);
        return command.Action switch
        {
            "archive" => await _client.ArchiveProjectAsync(
                command.ProjectId,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new ArchiveProjectRequest
                {
                    ArchiveIntent = ArchiveProjectRequestArchiveIntent.Archive,
                    RequestSchemaVersion = ArchiveProjectRequestRequestSchemaVersion.V1,
                },
                cancellationToken).ConfigureAwait(false),
            "restore" => await _client.RestoreProjectAsync(
                command.ProjectId,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new RestoreProjectRequest
                {
                    RestoreIntent = RestoreProjectRequestRestoreIntent.Restore,
                    RequestSchemaVersion = RestoreProjectRequestRequestSchemaVersion.V1,
                },
                cancellationToken).ConfigureAwait(false),
            "relink" => await RelinkAsync(command, correlationId, taskId, cancellationToken).ConfigureAwait(false),
            "unlink" => await UnlinkAsync(command, correlationId, taskId, cancellationToken).ConfigureAwait(false),
            "reevaluate" => await ReevaluateAsync(command, correlationId, taskId, cancellationToken).ConfigureAwait(false),
            _ => throw SafeValidation(),
        };
    }

    private async Task<AcceptedCommand> RelinkAsync(
        ProjectsMcpMaintenanceCommand command,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken)
        => command.ReferenceKind switch
        {
            "folder" => await _client.SetProjectFolderAsync(
                command.ProjectId,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new SetProjectFolderRequest
                {
                    RequestSchemaVersion = SetProjectFolderRequestRequestSchemaVersion.V1,
                    Operation = SetProjectFolderRequestOperation.Set,
                    ProjectId = command.ProjectId,
                    FolderId = command.ReferenceId,
                    FolderMetadata = new ProjectFolderMetadata { DisplayName = SafeDisplayName(command) },
                    ReplacementConfirmed = command.ReplacementConfirmed,
                },
                cancellationToken).ConfigureAwait(false),
            "file" => await _client.LinkFileReferenceAsync(
                command.ProjectId,
                command.ReferenceId!,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new LinkFileReferenceRequest
                {
                    RequestSchemaVersion = LinkFileReferenceRequestRequestSchemaVersion.V1,
                    Operation = LinkFileReferenceRequestOperation.Link,
                    ProjectId = command.ProjectId,
                    FileReferenceId = command.ReferenceId,
                    FolderId = command.TransientFolderId,
                    WorkspaceId = command.TransientWorkspaceId,
                    FilePath = command.TransientFilePath,
                    FileMetadata = new ProjectFileReferenceMetadata { DisplayName = SafeDisplayName(command) },
                },
                cancellationToken).ConfigureAwait(false),
            "memory" => await _client.LinkMemoryAsync(
                command.ProjectId,
                command.ReferenceId!,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new LinkMemoryRequest
                {
                    RequestSchemaVersion = LinkMemoryRequestRequestSchemaVersion.V1,
                    Operation = LinkMemoryRequestOperation.Link,
                    ProjectId = command.ProjectId,
                    MemoryReferenceId = command.ReferenceId,
                    MemoryMetadata = new ProjectMemoryReferenceMetadata { DisplayName = SafeDisplayName(command) },
                },
                cancellationToken).ConfigureAwait(false),
            _ => throw SafeValidation(),
        };

    private async Task<AcceptedCommand> UnlinkAsync(
        ProjectsMcpMaintenanceCommand command,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken)
        => command.ReferenceKind switch
        {
            "conversation" => await _client.UnlinkProjectConversationAsync(
                command.ProjectId,
                command.ReferenceId!,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new UnlinkProjectConversationRequest
                {
                    RequestSchemaVersion = UnlinkProjectConversationRequestRequestSchemaVersion.V1,
                    Operation = UnlinkProjectConversationRequestOperation.Unlink,
                    UnlinkIntent = UnlinkProjectConversationRequestUnlinkIntent.Clear,
                    ProjectId = command.ProjectId,
                    ConversationId = command.ReferenceId,
                },
                cancellationToken).ConfigureAwait(false),
            "file" => await _client.UnlinkFileReferenceAsync(
                command.ProjectId,
                command.ReferenceId!,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new UnlinkFileReferenceRequest
                {
                    RequestSchemaVersion = UnlinkFileReferenceRequestRequestSchemaVersion.V1,
                    Operation = UnlinkFileReferenceRequestOperation.Unlink,
                    UnlinkIntent = UnlinkFileReferenceRequestUnlinkIntent.RemoveReference,
                    ProjectId = command.ProjectId,
                    FileReferenceId = command.ReferenceId,
                },
                cancellationToken).ConfigureAwait(false),
            "memory" => await _client.UnlinkMemoryAsync(
                command.ProjectId,
                command.ReferenceId!,
                command.IdempotencyKey,
                correlationId,
                taskId,
                new UnlinkMemoryRequest
                {
                    RequestSchemaVersion = UnlinkMemoryRequestRequestSchemaVersion.V1,
                    Operation = UnlinkMemoryRequestOperation.Unlink,
                    UnlinkIntent = UnlinkMemoryRequestUnlinkIntent.RemoveReference,
                    ProjectId = command.ProjectId,
                    MemoryReferenceId = command.ReferenceId,
                },
                cancellationToken).ConfigureAwait(false),
            _ => throw SafeValidation(),
        };

    private async Task<AcceptedCommand> ReevaluateAsync(
        ProjectsMcpMaintenanceCommand command,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken)
    {
        await _client.RefreshProjectContextAsync(
            command.ProjectId,
            correlationId,
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        return new AcceptedCommand
        {
            AcceptedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            TaskId = taskId,
            Status = AcceptedCommandStatus.Accepted,
            IdempotentReplay = false,
        };
    }

    private static void Validate(ProjectsMcpMaintenanceCommand command)
    {
        if (!ProjectsMcpDescriptors.IsApprovedMaintenanceAction(command.Action)
            || string.IsNullOrWhiteSpace(command.ProjectId)
            || string.IsNullOrWhiteSpace(command.DryRunEvidence))
        {
            throw SafeValidation();
        }

        if (command.Action is not "reevaluate"
            && (!command.Confirmed || string.IsNullOrWhiteSpace(command.IdempotencyKey)))
        {
            throw SafeValidation();
        }

        if (command.Action is "relink" or "unlink"
            && (string.IsNullOrWhiteSpace(command.ReferenceKind) || string.IsNullOrWhiteSpace(command.ReferenceId)))
        {
            throw SafeValidation();
        }

        if (command.Action == "relink"
            && string.Equals(command.ReferenceKind, "file", StringComparison.Ordinal)
            && (string.IsNullOrWhiteSpace(command.TransientFolderId)
                || string.IsNullOrWhiteSpace(command.TransientWorkspaceId)
                || string.IsNullOrWhiteSpace(command.TransientFilePath)))
        {
            throw SafeValidation();
        }
    }

    private static CommandValidationException SafeValidation()
        => new(new ProblemDetailsPayload(
            Title: "MCP maintenance validation failed.",
            Detail: "The maintenance action is missing required safe confirmation, target, or idempotency evidence.",
            Status: null,
            EntityLabel: null,
            ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
            GlobalErrors: ["The maintenance action is missing required safe confirmation, target, or idempotency evidence."]));

    private static string SafeDisplayName(ProjectsMcpMaintenanceCommand command)
        => string.IsNullOrWhiteSpace(command.ReferenceDisplayLabel)
            ? command.ReferenceId ?? string.Empty
            : command.ReferenceDisplayLabel.Trim();

    private static string Resolve(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
