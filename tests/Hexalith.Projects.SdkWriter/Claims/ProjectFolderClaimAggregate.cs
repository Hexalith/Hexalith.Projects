// <copyright file="ProjectFolderClaimAggregate.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using System;
using System.Globalization;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// EventStore SDK aggregate for AD-35 exclusive Active Project Folder claims
/// (<c>Tenant/project-folder-claims/FolderId</c>). Generation equals the claim stream version at
/// acquisition; retries after release/close are rejected; stale releases leave later holders intact.
/// </summary>
public sealed class ProjectFolderClaimAggregate : EventStoreAggregate<ProjectFolderClaimState>
{
    /// <summary>The canonical EventStore domain name for AD-35 Folder claims.</summary>
    public const string DomainName = "project-folder-claims";

    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="ProjectFolderClaimAggregate"/> class.</summary>
    public ProjectFolderClaimAggregate()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ProjectFolderClaimAggregate"/> class.</summary>
    /// <param name="timeProvider">The UTC clock used for event timestamps.</param>
    public ProjectFolderClaimAggregate(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Handles <see cref="AcquireProjectFolderClaim"/>.</summary>
    /// <param name="command">The acquisition command.</param>
    /// <param name="state">The current claim state.</param>
    /// <param name="envelope">The verified EventStore command envelope.</param>
    /// <returns>The domain result emitting <see cref="ProjectFolderClaimAcquired"/>, no-op, or rejection.</returns>
    public DomainResult Handle(AcquireProjectFolderClaim command, ProjectFolderClaimState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope))
        {
            return Reject(envelope, "invalid_envelope", command.TaskId, command.TaskStepKey);
        }

        string projectId = command.ProjectId?.Trim() ?? string.Empty;
        if (!TryValidateProjectId(projectId)
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskId)
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskStepKey))
        {
            return Reject(envelope, "invalid_claim_command", command.TaskId, command.TaskStepKey);
        }

        ProjectFolderClaimState current = state ?? new ProjectFolderClaimState();
        if ((!string.IsNullOrWhiteSpace(current.TenantId) && !string.Equals(current.TenantId, envelope.TenantId, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(current.FolderId) && !string.Equals(current.FolderId, envelope.AggregateId, StringComparison.Ordinal)))
        {
            return Reject(envelope, "tenant_or_folder_mismatch", command.TaskId, command.TaskStepKey);
        }

        if (current.IsClosed)
        {
            return Reject(envelope, "claim_closed", command.TaskId, command.TaskStepKey);
        }

        if (current.TryLookupByTaskStep(command.TaskId, command.TaskStepKey, out ProjectFolderClaimReceipt? prior) && prior is not null)
        {
            // Idempotent replay is valid ONLY while the exact acquired claim is still active and matches.
            // If the prior claim was released, superseded, or closed, refuse rather than returning NoOp.
            if (current.IsActive
                && current.ActiveClaim is { } active
                && active.Generation == prior.Generation
                && string.Equals(active.ProjectId, projectId, StringComparison.Ordinal)
                && string.Equals(active.TaskId, command.TaskId, StringComparison.Ordinal)
                && string.Equals(active.TaskStepKey, command.TaskStepKey, StringComparison.Ordinal))
            {
                return DomainResult.NoOp();
            }

            return Reject(envelope, "claim_no_longer_active", command.TaskId, command.TaskStepKey);
        }

        if (current.IsActive)
        {
            return Reject(envelope, "folder_claim_unavailable", command.TaskId, command.TaskStepKey);
        }

        long nextGeneration = current.Version + 1;
        string receiptId = $"claim:{envelope.TenantId}:{envelope.AggregateId}:g{nextGeneration.ToString(CultureInfo.InvariantCulture)}:{command.TaskStepKey}";
        ProjectFolderClaimAcquired acquired = new(
            envelope.TenantId,
            envelope.AggregateId,
            projectId,
            nextGeneration,
            command.TaskId,
            command.TaskStepKey,
            envelope.UserId,
            envelope.CorrelationId,
            receiptId,
            _timeProvider.GetUtcNow());

        return DomainResult.Success([acquired]);
    }

    /// <summary>Handles <see cref="ReleaseProjectFolderClaim"/>.</summary>
    /// <param name="command">The release command.</param>
    /// <param name="state">The current claim state.</param>
    /// <param name="envelope">The verified EventStore command envelope.</param>
    /// <returns>The domain result emitting <see cref="ProjectFolderClaimReleased"/>, no-op, or rejection.</returns>
    public DomainResult Handle(ReleaseProjectFolderClaim command, ProjectFolderClaimState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope))
        {
            return Reject(envelope, "invalid_envelope", command.TaskId, command.TaskStepKey);
        }

        string projectId = command.ProjectId?.Trim() ?? string.Empty;
        string reasonCode = command.ReasonCode?.Trim() ?? string.Empty;
        if (!TryValidateProjectId(projectId)
            || command.ExpectedGeneration <= 0
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskId)
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskStepKey)
            || string.IsNullOrWhiteSpace(reasonCode)
            || !ProjectCommandValidator.IsSafePersistedMetadata(reasonCode, ProjectCommandValidator.MaxNameLength))
        {
            return Reject(envelope, "invalid_release_command", command.TaskId, command.TaskStepKey);
        }

        ProjectFolderClaimState current = state ?? new ProjectFolderClaimState();
        if ((!string.IsNullOrWhiteSpace(current.TenantId) && !string.Equals(current.TenantId, envelope.TenantId, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(current.FolderId) && !string.Equals(current.FolderId, envelope.AggregateId, StringComparison.Ordinal)))
        {
            return Reject(envelope, "tenant_or_folder_mismatch", command.TaskId, command.TaskStepKey);
        }

        if (current.TryLookupRelease(command.TaskId, command.TaskStepKey, out ProjectFolderClaimReleaseRecord? priorRelease)
            && priorRelease is not null)
        {
            return string.Equals(priorRelease.ProjectId, projectId, StringComparison.Ordinal)
                && priorRelease.ReleasedGeneration == command.ExpectedGeneration
                ? DomainResult.NoOp()
                : Reject(envelope, "release_conflict", command.TaskId, command.TaskStepKey);
        }

        if (current.IsClosed)
        {
            return Reject(envelope, "claim_closed", command.TaskId, command.TaskStepKey);
        }

        if (!current.IsActive || current.ActiveClaim is not { } active)
        {
            return Reject(envelope, "claim_not_active", command.TaskId, command.TaskStepKey);
        }

        if (!string.Equals(active.ProjectId, projectId, StringComparison.Ordinal)
            || active.Generation != command.ExpectedGeneration)
        {
            return Reject(envelope, "stale_claim_generation", command.TaskId, command.TaskStepKey);
        }

        ProjectFolderClaimReleased released = new(
            envelope.TenantId,
            envelope.AggregateId,
            projectId,
            command.ExpectedGeneration,
            command.TaskId,
            command.TaskStepKey,
            reasonCode,
            envelope.UserId,
            envelope.CorrelationId,
            _timeProvider.GetUtcNow());

        return DomainResult.Success([released]);
    }

    /// <summary>Handles <see cref="CloseProjectFolderClaim"/>.</summary>
    /// <param name="command">The close/quarantine command.</param>
    /// <param name="state">The current claim state.</param>
    /// <param name="envelope">The verified EventStore command envelope.</param>
    /// <returns>The domain result emitting <see cref="ProjectFolderClaimClosed"/>, no-op, or rejection.</returns>
    public DomainResult Handle(CloseProjectFolderClaim command, ProjectFolderClaimState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope))
        {
            return Reject(envelope, "invalid_envelope", command.TaskId, command.TaskStepKey);
        }

        string reasonCode = command.ReasonCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reasonCode)
            || !ProjectCommandValidator.IsSafePersistedMetadata(reasonCode, ProjectCommandValidator.MaxNameLength))
        {
            return Reject(envelope, "invalid_close_command", command.TaskId, command.TaskStepKey);
        }

        if (command.TaskId is not null && !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskId))
        {
            return Reject(envelope, "invalid_close_command", null, null);
        }

        if (command.TaskStepKey is not null && !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskStepKey))
        {
            return Reject(envelope, "invalid_close_command", command.TaskId, null);
        }

        ProjectFolderClaimState current = state ?? new ProjectFolderClaimState();
        if ((!string.IsNullOrWhiteSpace(current.TenantId) && !string.Equals(current.TenantId, envelope.TenantId, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(current.FolderId) && !string.Equals(current.FolderId, envelope.AggregateId, StringComparison.Ordinal)))
        {
            return Reject(envelope, "tenant_or_folder_mismatch", command.TaskId, command.TaskStepKey);
        }

        if (current.IsClosed)
        {
            return DomainResult.NoOp();
        }

        ProjectFolderClaimClosed closed = new(
            envelope.TenantId,
            envelope.AggregateId,
            reasonCode,
            command.TaskId,
            command.TaskStepKey,
            envelope.UserId,
            envelope.CorrelationId,
            _timeProvider.GetUtcNow());

        return DomainResult.Success([closed]);
    }

    private static bool IsValidEnvelope(CommandEnvelope envelope)
        => string.Equals(envelope.Domain, DomainName, StringComparison.Ordinal)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.TenantId)
            && ProjectCommandValidator.IsSafePersistedReferenceIdentifier(envelope.AggregateId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.UserId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.CorrelationId);

    private static bool TryValidateProjectId(string projectId)
    {
        try
        {
            _ = new ProjectId(projectId);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            return false;
        }
    }

    private static DomainResult Reject(CommandEnvelope envelope, string reasonCode, string? taskId, string? taskStepKey)
        => DomainResult.Rejection(
        [
            new ProjectFolderClaimRejected(
                ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.TenantId) ? envelope.TenantId : string.Empty,
                ProjectCommandValidator.IsSafePersistedReferenceIdentifier(envelope.AggregateId) ? envelope.AggregateId : "unknown",
                reasonCode,
                ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(taskId) ? taskId : null,
                ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(taskStepKey) ? taskStepKey : null,
                ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.CorrelationId) ? envelope.CorrelationId : string.Empty),
        ]);
}
