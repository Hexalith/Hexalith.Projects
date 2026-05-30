// <copyright file="EventStoreProjectCommandSubmitter.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Projects.Contracts.Commands;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Production <see cref="IProjectCommandSubmitter"/> that submits the create command through the
/// EventStore gateway client (persist-then-publish; the gateway is the sole Dapr-backed boundary).
/// Maps gateway transport / problem responses to the safe submission-outcome categories the endpoint
/// renders as RFC 9457 ProblemDetails.
/// </summary>
public sealed class EventStoreProjectCommandSubmitter(IEventStoreGatewayClient gateway) : IProjectCommandSubmitter
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IEventStoreGatewayClient _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // The /process payload carries only the safe, metadata-only create fields; tenant authority is
        // the gateway's Tenant argument (sourced from the verified tenant context), never the payload.
        object payload = new
        {
            name = command.Name,
            description = command.Description,
            setupMetadata = command.SetupMetadata,
        };

        SubmitCommandResponse submitted;
        try
        {
            submitted = await _gateway.SubmitCommandAsync(
                new SubmitCommandRequest(
                    MessageId: command.IdempotencyKey,
                    Tenant: command.TenantId,
                    Domain: ProjectsServerModule.DomainName,
                    AggregateId: command.ProjectId.Value,
                    CommandType: ProjectsServerModule.CreateProjectCommandType,
                    Payload: JsonSerializer.SerializeToElement(payload, PayloadJsonOptions),
                    CorrelationId: command.CorrelationId,
                    Extensions: new Dictionary<string, string>
                    {
                        ["taskId"] = command.TaskId,
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        catch (EventStoreGatewayException ex)
        {
            return ToProblemOutcome(ex, command.CorrelationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Gateway transport / serialization / Dapr failures are retryable infrastructure faults,
            // never a 500 with internal stack.
            return new ProjectCommandSubmissionResult(ProjectCommandSubmissionOutcome.Unavailable, command.CorrelationId);
        }

        string correlationId = !string.IsNullOrWhiteSpace(submitted.CorrelationId)
            ? submitted.CorrelationId
            : command.CorrelationId;

        return ProjectCommandSubmissionResult.Accepted(correlationId, IsIdempotentReplay(submitted.ResultPayload));
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(
        UpdateProjectSetup command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            setup = command.Setup,
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.UpdateProjectSetupCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(
        ArchiveProject command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            archiveIntent = "archive",
            requestSchemaVersion = "v1",
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.ArchiveProjectCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitRestoreProjectAsync(
        RestoreProject command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            restoreIntent = "restore",
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.RestoreProjectCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(
        SetProjectFolder command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            operation = "set",
            projectId = command.ProjectId.Value,
            folderId = command.FolderId,
            folderMetadata = command.FolderMetadata,
            replacementConfirmed = command.ReplacementConfirmed,
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.SetProjectFolderCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(
        LinkFileReference command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            operation = "link",
            projectId = command.ProjectId.Value,
            fileReferenceId = command.FileReferenceId,
            folderId = command.FolderId,
            fileMetadata = command.FileMetadata,
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.LinkFileReferenceCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(
        UnlinkFileReference command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            operation = "unlink",
            unlinkIntent = "removeReference",
            projectId = command.ProjectId.Value,
            fileReferenceId = command.FileReferenceId,
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.UnlinkFileReferenceCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(
        LinkMemory command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            operation = "link",
            projectId = command.ProjectId.Value,
            memoryReferenceId = command.MemoryReferenceId,
            memoryMetadata = command.MemoryMetadata,
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.LinkMemoryCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(
        UnlinkMemory command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            operation = "unlink",
            unlinkIntent = "removeReference",
            projectId = command.ProjectId.Value,
            memoryReferenceId = command.MemoryReferenceId,
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.UnlinkMemoryCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
        ConfirmProjectResolution command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object payload = new
        {
            requestSchemaVersion = "v1",
            operation = "confirm",
            projectId = command.ProjectId.Value,
            conversationId = command.ConversationId,
            sourceProjectId = command.SourceProjectId?.Value,
            resolutionResult = "MultipleCandidates",
        };

        return await SubmitAsync(
            command,
            ProjectsServerModule.ConfirmProjectResolutionCommandType,
            payload,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectCommandSubmissionResult> SubmitAsync(
        IProjectCommand command,
        string commandType,
        object payload,
        CancellationToken cancellationToken)
    {
        SubmitCommandResponse submitted;
        try
        {
            submitted = await _gateway.SubmitCommandAsync(
                new SubmitCommandRequest(
                    MessageId: command.IdempotencyKey,
                    Tenant: command.TenantId,
                    Domain: ProjectsServerModule.DomainName,
                    AggregateId: command.ProjectId.Value,
                    CommandType: commandType,
                    Payload: JsonSerializer.SerializeToElement(payload, PayloadJsonOptions),
                    CorrelationId: command.CorrelationId,
                    Extensions: new Dictionary<string, string>
                    {
                        ["taskId"] = command.TaskId,
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        catch (EventStoreGatewayException ex)
        {
            return ToProblemOutcome(ex, command.CorrelationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new ProjectCommandSubmissionResult(ProjectCommandSubmissionOutcome.Unavailable, command.CorrelationId);
        }

        string correlationId = !string.IsNullOrWhiteSpace(submitted.CorrelationId)
            ? submitted.CorrelationId
            : command.CorrelationId;

        return ProjectCommandSubmissionResult.Accepted(correlationId, IsIdempotentReplay(submitted.ResultPayload));
    }

    private static ProjectCommandSubmissionResult ToProblemOutcome(EventStoreGatewayException exception, string correlationId)
        => exception.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new(ProjectCommandSubmissionOutcome.ValidationFailed, correlationId),
            StatusCodes.Status409Conflict => new(ProjectCommandSubmissionOutcome.IdempotencyConflict, correlationId),
            // Authentication / authorization / not-found all collapse to safe-denial (404 at the edge),
            // so cross-tenant existence cannot be inferred (AR-16).
            StatusCodes.Status401Unauthorized
                or StatusCodes.Status403Forbidden
                or StatusCodes.Status404NotFound => ProjectCommandSubmissionResult.Denied(correlationId),
            // Any 5xx is an upstream failure, not an authorization decision — surface as retryable.
            >= 500 and < 600 => new(ProjectCommandSubmissionOutcome.Unavailable, correlationId),
            _ => ProjectCommandSubmissionResult.Denied(correlationId),
        };

    private static bool IsIdempotentReplay(JsonElement? resultPayload)
    {
        if (resultPayload is null || resultPayload.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement root = resultPayload.Value;
        return root.TryGetProperty("idempotentReplay", out JsonElement replay)
            && replay.ValueKind is JsonValueKind.True or JsonValueKind.False
            && replay.GetBoolean();
    }
}
