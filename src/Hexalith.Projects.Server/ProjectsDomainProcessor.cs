// <copyright file="ProjectsDomainProcessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// The Projects <c>/process</c> aggregate-callback domain processor (Story 1.4). Mirrors the Folders
/// <c>FolderDomainProcessor</c> stripped to the create slice: it deserializes the create payload,
/// derives the command from the verified envelope (tenant comes from the envelope, never the payload),
/// invokes the pure <see cref="ProjectAggregate"/> <c>Handle</c>, and maps the result to a
/// <see cref="DomainResult"/> (success events, a domain rejection event, or a no-op for an idempotent
/// replay). Domain rejections are events, never exceptions; only a malformed payload / unexpected
/// failure fails closed to a metadata-only rejection.
/// </summary>
public sealed class ProjectsDomainProcessor(TimeProvider timeProvider) : IDomainProcessor
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc/>
    public Task<DomainResult> ProcessAsync(CommandEnvelope command, object? currentState)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!string.Equals(command.Domain, ProjectsServerModule.DomainName, StringComparison.Ordinal)
            || !string.Equals(command.CommandType, ProjectsServerModule.CreateProjectCommandType, StringComparison.Ordinal))
        {
            return Task.FromResult(Rejection(command, ProjectResultCode.ValidationFailed, null));
        }

        return Task.FromResult(ProcessCreate(command, currentState));
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
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Name))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null);
        }

        ProjectId projectId;
        try
        {
            projectId = new ProjectId(envelope.AggregateId);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null);
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
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null);
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
            ProjectResultCode.Created => DomainResult.Success(result.Events),

            // A logical replay produced no new event — acknowledge as a no-op (the prior event landed).
            ProjectResultCode.IdempotentReplay => DomainResult.NoOp(),

            // Every other code is a domain rejection (an event, never an exception).
            _ => DomainResult.Rejection([result.ToRejectionEvent()]),
        };

    private static DomainResult Rejection(CommandEnvelope envelope, ProjectResultCode code, string? rejectedField)
    {
        // Build the metadata-only rejection directly from the envelope when no aggregate result is
        // available (malformed payload). Reason maps through the shared vocabulary.
        ProjectResult synthetic = ProjectResult.Rejected(
            new CreateProject(
                envelope.TenantId,
                SafeProjectId(envelope.AggregateId),
                "x",
                null,
                null,
                envelope.UserId,
                envelope.CorrelationId,
                envelope.CorrelationId,
                envelope.MessageId),
            code,
            rejectedField);

        return DomainResult.Rejection([synthetic.ToRejectionEvent()]);
    }

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
}
