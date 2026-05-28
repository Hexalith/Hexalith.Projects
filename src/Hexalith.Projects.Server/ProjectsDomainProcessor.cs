// <copyright file="ProjectsDomainProcessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// The Projects <c>/process</c> aggregate-callback domain processor (Story 1.4). Mirrors the Folders
/// <c>FolderDomainProcessor</c> stripped to the create slice: it deserializes the create payload,
/// derives the command from the verified envelope (tenant comes from the envelope, never the payload),
/// invokes the pure <see cref="ProjectAggregate"/> <c>Handle</c>, and maps the result to a
/// <see cref="DomainResult"/> (success events, a domain rejection event, or a no-op for an idempotent
/// replay). Domain rejections are events, never exceptions; only a malformed payload / unexpected
/// failure fails closed to a metadata-only rejection.
/// </summary>
public sealed class ProjectsDomainProcessor(
    TimeProvider timeProvider,
    IProjectEventStoreAuthorizationValidator authorizationValidator) : IDomainProcessor
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly IProjectEventStoreAuthorizationValidator _authorizationValidator =
        authorizationValidator ?? throw new ArgumentNullException(nameof(authorizationValidator));

    /// <inheritdoc/>
    public async Task<DomainResult> ProcessAsync(CommandEnvelope command, object? currentState)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!string.Equals(command.Domain, ProjectsServerModule.DomainName, StringComparison.Ordinal))
        {
            return Rejection(command, ProjectResultCode.ValidationFailed, null, ProjectsServerModule.CreateProjectCommandType);
        }

        string? actionToken = command.CommandType switch
        {
            ProjectsServerModule.CreateProjectCommandType => ProjectAuthorizationGate.CreateProjectAction,
            ProjectsServerModule.UpdateProjectSetupCommandType => ProjectAuthorizationGate.UpdateProjectSetupAction,
            ProjectsServerModule.ArchiveProjectCommandType => ProjectAuthorizationGate.ArchiveProjectAction,
            ProjectsServerModule.SetProjectFolderCommandType => ProjectAuthorizationGate.SetProjectFolderAction,
            ProjectsServerModule.LinkFileReferenceCommandType => ProjectAuthorizationGate.LinkFileReferenceAction,
            ProjectsServerModule.UnlinkFileReferenceCommandType => ProjectAuthorizationGate.UnlinkFileReferenceAction,
            ProjectsServerModule.LinkMemoryCommandType => ProjectAuthorizationGate.LinkMemoryAction,
            ProjectsServerModule.UnlinkMemoryCommandType => ProjectAuthorizationGate.UnlinkMemoryAction,
            _ => null,
        };

        if (actionToken is null)
        {
            return Rejection(command, ProjectResultCode.ValidationFailed, null, command.CommandType);
        }

        EventStoreAuthorizationValidationResult validation;
        try
        {
            validation = await _authorizationValidator.ValidateAsync(
                new EventStoreAuthorizationValidationRequest(
                    command.TenantId,
                    command.UserId,
                    actionToken,
                    command.AggregateId,
                    command.CorrelationId,
                    ReadTaskId(command),
                    [AuthorizationLayer.EventStoreValidator]),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            validation = EventStoreAuthorizationValidationResult.Unavailable();
        }

        if (validation.Status != EventStoreAuthorizationValidationStatus.Allowed)
        {
            return Rejection(command, ProjectResultCode.Unauthorized, null, command.CommandType);
        }

        return command.CommandType switch
        {
            ProjectsServerModule.CreateProjectCommandType => ProcessCreate(command, currentState),
            ProjectsServerModule.UpdateProjectSetupCommandType => ProcessUpdateProjectSetup(command, currentState),
            ProjectsServerModule.ArchiveProjectCommandType => ProcessArchiveProject(command, currentState),
            ProjectsServerModule.SetProjectFolderCommandType => ProcessSetProjectFolder(command, currentState),
            ProjectsServerModule.LinkFileReferenceCommandType => ProcessLinkFileReference(command, currentState),
            ProjectsServerModule.UnlinkFileReferenceCommandType => ProcessUnlinkFileReference(command, currentState),
            ProjectsServerModule.LinkMemoryCommandType => ProcessLinkMemory(command, currentState),
            ProjectsServerModule.UnlinkMemoryCommandType => ProcessUnlinkMemory(command, currentState),
            _ => Rejection(command, ProjectResultCode.ValidationFailed, null, command.CommandType),
        };
    }

    private DomainResult ProcessCreate(CommandEnvelope envelope, object? currentState)
    {
        CreateProjectPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CreateProjectPayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Malformed payload / type mismatch fails closed to a metadata-only rejection (no echo).
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Name))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        // Tenant comes from the verified EventStore envelope (authoritative authority), never the
        // payload. The command pipeline already validated the envelope identity components.
        CreateProject command = new(
            envelope.TenantId,
            projectId,
            payload.Name,
            payload.Description,
            payload.SetupMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Any unexpected exception from the pure aggregate must fail closed without leaking
            // type/stack metadata through the gateway response.
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private DomainResult ProcessUpdateProjectSetup(CommandEnvelope envelope, object? currentState)
    {
        UpdateProjectSetupPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<UpdateProjectSetupPayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        if (payload is null || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "requestSchemaVersion", envelope.CommandType);
        }

        if (payload.Setup is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "setup", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        UpdateProjectSetup command = new(
            envelope.TenantId,
            projectId,
            payload.Setup,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private DomainResult ProcessArchiveProject(CommandEnvelope envelope, object? currentState)
    {
        try
        {
            ArchiveProjectPayload? payload = JsonSerializer.Deserialize<ArchiveProjectPayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
            if (payload is null || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal))
            {
                return Rejection(envelope, ProjectResultCode.ValidationFailed, "requestSchemaVersion", envelope.CommandType);
            }

            if (!string.Equals(payload.ArchiveIntent, "archive", StringComparison.Ordinal))
            {
                return Rejection(envelope, ProjectResultCode.ValidationFailed, "archiveIntent", envelope.CommandType);
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        ArchiveProject command = new(
            envelope.TenantId,
            projectId,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private DomainResult ProcessSetProjectFolder(CommandEnvelope envelope, object? currentState)
    {
        SetProjectFolderPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SetProjectFolderPayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        if (payload is null
            || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(payload.Operation, "set", StringComparison.Ordinal)
            || !string.Equals(payload.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.FolderId)
            || payload.FolderMetadata is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        SetProjectFolder command = new(
            envelope.TenantId,
            projectId,
            payload.FolderId!,
            payload.FolderMetadata,
            payload.ReplacementConfirmed,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }


    private DomainResult ProcessLinkFileReference(CommandEnvelope envelope, object? currentState)
    {
        LinkFileReferencePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LinkFileReferencePayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        if (payload is null
            || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(payload.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(payload.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.FileReferenceId)
            || string.IsNullOrWhiteSpace(payload.FolderId)
            || payload.FileMetadata is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        LinkFileReference command = new(
            envelope.TenantId,
            projectId,
            payload.FileReferenceId!,
            payload.FolderId!,
            payload.FileMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private DomainResult ProcessUnlinkFileReference(CommandEnvelope envelope, object? currentState)
    {
        UnlinkFileReferencePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<UnlinkFileReferencePayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        if (payload is null
            || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(payload.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(payload.UnlinkIntent, "removeReference", StringComparison.Ordinal)
            || !string.Equals(payload.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.FileReferenceId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        UnlinkFileReference command = new(
            envelope.TenantId,
            projectId,
            payload.FileReferenceId!,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private DomainResult ProcessLinkMemory(CommandEnvelope envelope, object? currentState)
    {
        LinkMemoryPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LinkMemoryPayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        if (payload is null
            || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(payload.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(payload.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.MemoryReferenceId)
            || payload.MemoryMetadata is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        LinkMemory command = new(
            envelope.TenantId,
            projectId,
            payload.MemoryReferenceId!,
            payload.MemoryMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private DomainResult ProcessUnlinkMemory(CommandEnvelope envelope, object? currentState)
    {
        UnlinkMemoryPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<UnlinkMemoryPayload>(Encoding.UTF8.GetString(envelope.Payload), PayloadJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "body", envelope.CommandType);
        }

        if (payload is null
            || !string.Equals(payload.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(payload.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(payload.UnlinkIntent, "removeReference", StringComparison.Ordinal)
            || !string.Equals(payload.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.MemoryReferenceId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", envelope.CommandType);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        UnlinkMemory command = new(
            envelope.TenantId,
            projectId,
            payload.MemoryReferenceId!,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId);

        ProjectState state = currentState as ProjectState ?? ProjectState.Empty;

        ProjectResult result;
        try
        {
            result = ProjectAggregate.Handle(state, command, _timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, envelope.CommandType);
        }

        return ToDomainResult(result);
    }

    private static string ReadTaskId(CommandEnvelope envelope)
        => envelope.Extensions is not null
            && envelope.Extensions.TryGetValue("taskId", out string? taskId)
            && !string.IsNullOrWhiteSpace(taskId)
            ? taskId
            : envelope.CorrelationId;

    private static DomainResult ToDomainResult(ProjectResult result)
        => result.Code switch
        {
            // Persist-then-publish: the success event is persisted then published by the pipeline.
            ProjectResultCode.Created
                or ProjectResultCode.SetupUpdated
                or ProjectResultCode.Archived
                or ProjectResultCode.FolderSet
                or ProjectResultCode.FileReferenceLinked
                or ProjectResultCode.FileReferenceUnlinked
                or ProjectResultCode.MemoryLinked
                or ProjectResultCode.MemoryUnlinked => DomainResult.Success(result.Events),

            // A logical replay produced no new event — acknowledge as a no-op (the prior event landed).
            ProjectResultCode.IdempotentReplay => DomainResult.NoOp(),

            // Every other code is a domain rejection (an event, never an exception).
            _ => DomainResult.Rejection([result.ToRejectionEvent()]),
        };

    private static DomainResult Rejection(CommandEnvelope envelope, ProjectResultCode code, string? rejectedField, string commandType)
    {
        // Build the metadata-only rejection directly from the envelope when no aggregate result is
        // available (malformed payload). Reason maps through the shared vocabulary.
        ProjectResult synthetic = ProjectResult.Rejected(
            ToContractCommandType(commandType),
            envelope.TenantId,
            SafeProjectId(envelope.AggregateId).Value,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId,
            code,
            rejectedField);

        return DomainResult.Rejection([synthetic.ToRejectionEvent()]);
    }

    private static string ToContractCommandType(string commandType)
        => commandType switch
        {
            ProjectsServerModule.UpdateProjectSetupCommandType => nameof(UpdateProjectSetup),
            ProjectsServerModule.ArchiveProjectCommandType => nameof(ArchiveProject),
            ProjectsServerModule.SetProjectFolderCommandType => nameof(SetProjectFolder),
            ProjectsServerModule.LinkFileReferenceCommandType => nameof(LinkFileReference),
            ProjectsServerModule.UnlinkFileReferenceCommandType => nameof(UnlinkFileReference),
            ProjectsServerModule.LinkMemoryCommandType => nameof(LinkMemory),
            ProjectsServerModule.UnlinkMemoryCommandType => nameof(UnlinkMemory),
            _ => nameof(CreateProject),
        };

    // Produces a placeholder ProjectId so the synthetic rejection result can be built even when the
    // envelope aggregate id is malformed; the rejection event never echoes it (SafePassthrough +
    // ToRejectionEvent only surface safe metadata).
    private static ProjectId SafeProjectId(string aggregateId)
    {
        try
        {
            return new ProjectId(aggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return new ProjectId("unknown");
        }
    }

    private sealed record CreateProjectPayload(
        [property: JsonRequired] string? Name,
        string? Description,
        string? SetupMetadata);

    private sealed record UpdateProjectSetupPayload(
        string? RequestSchemaVersion,
        ProjectSetup? Setup);

    private sealed record ArchiveProjectPayload(
        [property: JsonRequired] string? ArchiveIntent,
        [property: JsonRequired] string? RequestSchemaVersion);

    private sealed record SetProjectFolderPayload(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? FolderId,
        ProjectFolderMetadata? FolderMetadata,
        bool ReplacementConfirmed);

    private sealed record LinkFileReferencePayload(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? FileReferenceId,
        string? FolderId,
        ProjectFileReferenceMetadata? FileMetadata);

    private sealed record UnlinkFileReferencePayload(
        string? RequestSchemaVersion,
        string? Operation,
        string? UnlinkIntent,
        string? ProjectId,
        string? FileReferenceId);

    private sealed record LinkMemoryPayload(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? MemoryReferenceId,
        ProjectMemoryReferenceMetadata? MemoryMetadata);

    private sealed record UnlinkMemoryPayload(
        string? RequestSchemaVersion,
        string? Operation,
        string? UnlinkIntent,
        string? ProjectId,
        string? MemoryReferenceId);
}
