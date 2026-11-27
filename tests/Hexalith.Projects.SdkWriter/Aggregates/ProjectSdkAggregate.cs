// <copyright file="ProjectSdkAggregate.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Aggregates;

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.SdkWriter.Commands;

using ContractArchiveProject = Hexalith.Projects.Contracts.Commands.ArchiveProject;
using ContractConfirmProjectResolution = Hexalith.Projects.Contracts.Commands.ConfirmProjectResolution;
using ContractCreateProject = Hexalith.Projects.Contracts.Commands.CreateProject;
using ContractLinkFileReference = Hexalith.Projects.Contracts.Commands.LinkFileReference;
using ContractLinkMemory = Hexalith.Projects.Contracts.Commands.LinkMemory;
using ContractRestoreProject = Hexalith.Projects.Contracts.Commands.RestoreProject;
using ContractSetProjectFolder = Hexalith.Projects.Contracts.Commands.SetProjectFolder;
using ContractUnlinkFileReference = Hexalith.Projects.Contracts.Commands.UnlinkFileReference;
using ContractUnlinkMemory = Hexalith.Projects.Contracts.Commands.UnlinkMemory;
using ContractUpdateProjectSetup = Hexalith.Projects.Contracts.Commands.UpdateProjectSetup;
using WireArchiveProject = Hexalith.Projects.SdkWriter.Commands.ArchiveProject;
using WireConfirmProjectResolution = Hexalith.Projects.SdkWriter.Commands.ConfirmProjectResolution;
using WireCreateProject = Hexalith.Projects.SdkWriter.Commands.CreateProject;
using WireLinkFileReference = Hexalith.Projects.SdkWriter.Commands.LinkFileReference;
using WireLinkMemory = Hexalith.Projects.SdkWriter.Commands.LinkMemory;
using WireRestoreProject = Hexalith.Projects.SdkWriter.Commands.RestoreProject;
using WireSetProjectFolder = Hexalith.Projects.SdkWriter.Commands.SetProjectFolder;
using WireUnlinkFileReference = Hexalith.Projects.SdkWriter.Commands.UnlinkFileReference;
using WireUnlinkMemory = Hexalith.Projects.SdkWriter.Commands.UnlinkMemory;
using WireUpdateProjectSetup = Hexalith.Projects.SdkWriter.Commands.UpdateProjectSetup;

/// <summary>
/// EventStore SDK aggregate for the <c>projects</c> domain in Story 7.1 test composition.
/// Exposes public <c>Handle</c> methods matching all 12 routed v1 command short names plus
/// <see cref="CreateBoundProject"/> and <see cref="AbandonProjectCreation"/> for discovery by
/// <see cref="EventStoreAggregate{TState}"/>.
/// </summary>
public sealed class ProjectSdkAggregate : EventStoreAggregate<ProjectSdkState>
{
    /// <summary>The canonical domain name handled by this aggregate.</summary>
    public const string DomainName = "projects";

    private readonly TimeProvider _timeProvider;
    private readonly Func<string, string, string, bool>? _isCurrentOwner;

    /// <summary>Initializes a new instance of the <see cref="ProjectSdkAggregate"/> class.</summary>
    public ProjectSdkAggregate()
        : this(TimeProvider.System, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ProjectSdkAggregate"/> class.</summary>
    /// <param name="timeProvider">The UTC clock used for event timestamps.</param>
    /// <param name="isCurrentOwner">
    /// Optional synchronous current-owner verifier <c>(tenantId, userId, operation) =&gt; bool</c>
    /// for <see cref="GrantProjectChatBotAuthority"/> and <see cref="RevokeProjectChatBotAuthority"/>.
    /// Fails closed (<c>owner_unavailable</c>) when null or false.
    /// </param>
    public ProjectSdkAggregate(
        TimeProvider timeProvider,
        Func<string, string, string, bool>? isCurrentOwner = null)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _isCurrentOwner = isCurrentOwner;
    }

    /// <summary>Handles Story 7.1 Folder-first bound Project creation.</summary>
    /// <param name="command">The bound creation command.</param>
    /// <param name="state">The current SDK state, or null for a fresh stream.</param>
    /// <param name="envelope">The verified EventStore command envelope.</param>
    /// <returns>The domain result emitting one bound <see cref="ProjectCreated"/> event, no-op, or rejection.</returns>
    public DomainResult Handle(CreateBoundProject command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractCreateProject));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "projectId", nameof(ContractCreateProject));
        }

        if (!ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskStepKey))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.TaskStepKey), nameof(ContractCreateProject));
        }

        string trimmedFolderId = command.FolderId?.Trim() ?? string.Empty;
        if (!ProjectCommandValidator.IsSafePersistedReferenceIdentifier(trimmedFolderId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.FolderId), nameof(ContractCreateProject));
        }

        if (command.FolderClaimGeneration <= 0)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.FolderClaimGeneration), nameof(ContractCreateProject));
        }

        string? canonicalFolderDisplayName = string.IsNullOrWhiteSpace(command.FolderDisplayName)
            ? null
            : command.FolderDisplayName.Trim();
        if (!ProjectCommandValidator.IsSafePersistedMetadata(canonicalFolderDisplayName, ProjectCommandValidator.MaxNameLength))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.FolderDisplayName), nameof(ContractCreateProject));
        }

        string? canonicalProvisioningReceiptId = string.IsNullOrWhiteSpace(command.FolderProvisioningReceiptId)
            ? null
            : command.FolderProvisioningReceiptId.Trim();
        if (canonicalProvisioningReceiptId is not null
            && !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(canonicalProvisioningReceiptId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.FolderProvisioningReceiptId), nameof(ContractCreateProject));
        }

        ContractCreateProject createCheck = new(
            envelope.TenantId,
            projectId!,
            command.Name,
            command.Description,
            command.SetupMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);
        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(createCheck);
        if (!validation.IsAccepted)
        {
            return Rejection(envelope, validation.Code, validation.RejectedField, nameof(ContractCreateProject));
        }

        string fingerprint = ComputeBoundCreationFingerprint(
            validation.CanonicalName!,
            validation.CanonicalDescription,
            validation.CanonicalSetupMetadata,
            trimmedFolderId,
            canonicalFolderDisplayName,
            canonicalProvisioningReceiptId,
            command.FolderClaimGeneration,
            command.TaskStepKey);

        ProjectState domainState = state?.DomainState ?? ProjectState.Empty;
        if ((!string.IsNullOrWhiteSpace(domainState.TenantId) && !string.Equals(domainState.TenantId, envelope.TenantId, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(domainState.ProjectId) && !string.Equals(domainState.ProjectId, projectId!.Value, StringComparison.Ordinal)))
        {
            return Rejection(envelope, ProjectResultCode.TenantMismatch, null, nameof(ContractCreateProject));
        }

        if (domainState.IdempotencyFingerprints.TryGetValue(envelope.MessageId, out string? priorFingerprint))
        {
            return string.Equals(priorFingerprint, fingerprint, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : Rejection(envelope, ProjectResultCode.IdempotencyConflict, null, nameof(ContractCreateProject));
        }

        if (state is not null && state.AppliedTaskSteps.TryGetValue(command.TaskStepKey, out string? priorStepFingerprint))
        {
            return string.Equals(priorStepFingerprint, fingerprint, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : Rejection(envelope, ProjectResultCode.IdempotencyConflict, nameof(command.TaskStepKey), nameof(ContractCreateProject));
        }

        if (domainState.IsCreationAbandoned)
        {
            return Rejection(envelope, ProjectResultCode.CreationAbandoned, null, nameof(ContractCreateProject));
        }

        if (domainState.IsCreated)
        {
            return Rejection(envelope, ProjectResultCode.DuplicateProject, null, nameof(ContractCreateProject));
        }

        ProjectCreated created = new(
            envelope.TenantId,
            projectId!.Value,
            validation.CanonicalName!,
            validation.CanonicalDescription,
            validation.CanonicalSetupMetadata,
            ProjectLifecycle.Active,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId,
            fingerprint,
            _timeProvider.GetUtcNow())
        {
            FolderId = trimmedFolderId,
            FolderDisplayName = canonicalFolderDisplayName,
            FolderProvisioningReceiptId = canonicalProvisioningReceiptId,
            FolderClaimGeneration = command.FolderClaimGeneration,
            TaskStepKey = command.TaskStepKey,
        };

        return DomainResult.Success([created]);
    }

    /// <summary>Handles Story 7.1 terminal abandonment fencing on a reserved <c>ProjectId</c> stream.</summary>
    /// <param name="command">The abandonment command.</param>
    /// <param name="state">The current SDK state, or null for a fresh stream.</param>
    /// <param name="envelope">The verified EventStore command envelope.</param>
    /// <returns>The domain result emitting <see cref="ProjectCreationAbandoned"/>, no-op, or rejection.</returns>
    public DomainResult Handle(AbandonProjectCreation command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractCreateProject));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "projectId", nameof(ContractCreateProject));
        }

        if (!ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(command.TaskStepKey))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.TaskStepKey), nameof(ContractCreateProject));
        }

        string canonicalReason = command.ReasonCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(canonicalReason)
            || !ProjectCommandValidator.IsSafePersistedMetadata(canonicalReason, ProjectCommandValidator.MaxNameLength))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.ReasonCode), nameof(ContractCreateProject));
        }

        string? canonicalFolderId = string.IsNullOrWhiteSpace(command.FolderId) ? null : command.FolderId.Trim();
        if (canonicalFolderId is not null && !ProjectCommandValidator.IsSafePersistedReferenceIdentifier(canonicalFolderId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.FolderId), nameof(ContractCreateProject));
        }

        if (command.FolderClaimGeneration is { } generation && (canonicalFolderId is null || generation <= 0))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, nameof(command.FolderClaimGeneration), nameof(ContractCreateProject));
        }

        string fingerprint = ComputeAbandonmentFingerprint(
            canonicalFolderId,
            command.FolderClaimGeneration,
            canonicalReason,
            command.TaskStepKey);

        ProjectState domainState = state?.DomainState ?? ProjectState.Empty;
        if ((!string.IsNullOrWhiteSpace(domainState.TenantId) && !string.Equals(domainState.TenantId, envelope.TenantId, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(domainState.ProjectId) && !string.Equals(domainState.ProjectId, projectId!.Value, StringComparison.Ordinal)))
        {
            return Rejection(envelope, ProjectResultCode.TenantMismatch, null, nameof(ContractCreateProject));
        }

        if (domainState.IdempotencyFingerprints.TryGetValue(envelope.MessageId, out string? priorFingerprint))
        {
            return string.Equals(priorFingerprint, fingerprint, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : Rejection(envelope, ProjectResultCode.IdempotencyConflict, null, nameof(ContractCreateProject));
        }

        if (state is not null && state.AppliedTaskSteps.TryGetValue(command.TaskStepKey, out string? priorStepFingerprint))
        {
            return string.Equals(priorStepFingerprint, fingerprint, StringComparison.Ordinal)
                ? DomainResult.NoOp()
                : Rejection(envelope, ProjectResultCode.IdempotencyConflict, nameof(command.TaskStepKey), nameof(ContractCreateProject));
        }

        if (domainState.IsCreated)
        {
            return Rejection(envelope, ProjectResultCode.DuplicateProject, null, nameof(ContractCreateProject));
        }

        if (domainState.IsCreationAbandoned)
        {
            return Rejection(envelope, ProjectResultCode.CreationAbandoned, null, nameof(ContractCreateProject));
        }

        ProjectCreationAbandoned abandoned = new(
            envelope.TenantId,
            projectId!.Value,
            canonicalFolderId,
            command.FolderClaimGeneration,
            canonicalReason,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId,
            fingerprint,
            _timeProvider.GetUtcNow())
        {
            TaskStepKey = command.TaskStepKey,
        };

        return DomainResult.Success([abandoned]);
    }

    /// <summary>Handles retained v1 <c>CreateProject</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireCreateProject command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId)
            || string.IsNullOrWhiteSpace(command.Name)
            || !TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractCreateProject));
        }

        ContractCreateProject domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.Name,
            command.Description,
            command.SetupMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>UpdateProjectSetup</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireUpdateProjectSetup command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractUpdateProjectSetup));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "requestSchemaVersion", nameof(ContractUpdateProjectSetup));
        }

        if (command.Setup is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "setup", nameof(ContractUpdateProjectSetup));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractUpdateProjectSetup));
        }

        ContractUpdateProjectSetup domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.Setup,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>ArchiveProject</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireArchiveProject command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractArchiveProject));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "requestSchemaVersion", nameof(ContractArchiveProject));
        }

        if (!string.Equals(command.ArchiveIntent, "archive", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "archiveIntent", nameof(ContractArchiveProject));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractArchiveProject));
        }

        ContractArchiveProject domainCommand = new(
            envelope.TenantId,
            projectId!,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>RestoreProject</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireRestoreProject command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractRestoreProject));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "requestSchemaVersion", nameof(ContractRestoreProject));
        }

        if (!string.Equals(command.RestoreIntent, "restore", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "restoreIntent", nameof(ContractRestoreProject));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractRestoreProject));
        }

        ContractRestoreProject domainCommand = new(
            envelope.TenantId,
            projectId!,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>SetProjectFolder</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireSetProjectFolder command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractSetProjectFolder));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(command.Operation, "set", StringComparison.Ordinal)
            || !string.Equals(command.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.FolderId)
            || command.FolderMetadata is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", nameof(ContractSetProjectFolder));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractSetProjectFolder));
        }

        ContractSetProjectFolder domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.FolderId,
            command.FolderMetadata,
            command.ReplacementConfirmed,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>LinkFileReference</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireLinkFileReference command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractLinkFileReference));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(command.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(command.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.FileReferenceId)
            || string.IsNullOrWhiteSpace(command.FolderId)
            || command.FileMetadata is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", nameof(ContractLinkFileReference));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractLinkFileReference));
        }

        ContractLinkFileReference domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.FileReferenceId,
            command.FolderId,
            command.FileMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>UnlinkFileReference</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireUnlinkFileReference command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractUnlinkFileReference));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(command.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(command.UnlinkIntent, "removeReference", StringComparison.Ordinal)
            || !string.Equals(command.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.FileReferenceId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", nameof(ContractUnlinkFileReference));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractUnlinkFileReference));
        }

        ContractUnlinkFileReference domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.FileReferenceId,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>LinkMemory</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireLinkMemory command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractLinkMemory));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(command.Operation, "link", StringComparison.Ordinal)
            || !string.Equals(command.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.MemoryReferenceId)
            || command.MemoryMetadata is null)
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", nameof(ContractLinkMemory));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractLinkMemory));
        }

        ContractLinkMemory domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.MemoryReferenceId,
            command.MemoryMetadata,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>UnlinkMemory</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireUnlinkMemory command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractUnlinkMemory));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(command.Operation, "unlink", StringComparison.Ordinal)
            || !string.Equals(command.UnlinkIntent, "removeReference", StringComparison.Ordinal)
            || !string.Equals(command.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.MemoryReferenceId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", nameof(ContractUnlinkMemory));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractUnlinkMemory));
        }

        ContractUnlinkMemory domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.MemoryReferenceId,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <c>ConfirmProjectResolution</c>.</summary>
    /// <param name="command">The wire command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(WireConfirmProjectResolution command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsValidEnvelope(envelope, out string taskId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "envelope", nameof(ContractConfirmProjectResolution));
        }

        if (!string.Equals(command.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(command.Operation, "confirm", StringComparison.Ordinal)
            || !string.Equals(command.ProjectId, envelope.AggregateId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.ConversationId)
            || !string.Equals(command.ResolutionResult, "MultipleCandidates", StringComparison.Ordinal))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, "identity", nameof(ContractConfirmProjectResolution));
        }

        if (!TryParseProjectId(envelope.AggregateId, out ProjectId? projectId))
        {
            return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractConfirmProjectResolution));
        }

        ProjectId? sourceProjectId = null;
        if (!string.IsNullOrWhiteSpace(command.SourceProjectId))
        {
            if (!TryParseProjectId(command.SourceProjectId, out sourceProjectId))
            {
                return Rejection(envelope, ProjectResultCode.ValidationFailed, null, nameof(ContractConfirmProjectResolution));
            }
        }

        ContractConfirmProjectResolution domainCommand = new(
            envelope.TenantId,
            projectId!,
            command.ConversationId,
            sourceProjectId,
            envelope.UserId,
            envelope.CorrelationId,
            taskId,
            envelope.MessageId);

        return ToDomainResult(ProjectAggregate.Handle(state?.DomainState ?? ProjectState.Empty, domainCommand, _timeProvider.GetUtcNow()));
    }

    /// <summary>Handles retained v1 <see cref="GrantProjectChatBotAuthority"/>.</summary>
    /// <param name="command">The grant command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(GrantProjectChatBotAuthority command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!string.Equals(envelope.Domain, DomainName, StringComparison.Ordinal))
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("invalid_domain")]);
        }

        if (_isCurrentOwner is null
            || !IsSafeGrantToken(envelope.TenantId)
            || !IsSafeGrantToken(envelope.AggregateId)
            || !IsSafeGrantToken(envelope.UserId)
            || !_isCurrentOwner(envelope.TenantId, envelope.UserId, nameof(GrantProjectChatBotAuthority)))
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("owner_unavailable")]);
        }

        ProjectState domainState = state?.DomainState ?? ProjectState.Empty;
        if (!domainState.IsCreated
            || domainState.IsCreationAbandoned
            || domainState.TenantId != envelope.TenantId
            || domainState.ProjectId != envelope.AggregateId
            || domainState.Lifecycle != ProjectLifecycle.Active)
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("project_unavailable")]);
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (!ValidGrantScope(command.PrincipalId, command.Operation, command.ActorClass, command.Origin)
            || !IsSafeGrantToken(command.GrantId)
            || command.ExpiresAt.Offset != TimeSpan.Zero
            || command.ExpiresAt <= now
            || command.ExpiresAt > now.AddDays(30)
            || domainState.ChatBotGrantIssuanceIds.Contains(command.GrantId))
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("invalid_grant")]);
        }

        string scope = ChatBotGrantScope.For(command.PrincipalId, command.Operation, command.ActorClass, command.Origin);
        if (domainState.ChatBotGrants.TryGetValue(scope, out ProjectChatBotGrant? prior)
            && !prior.Revoked
            && prior.ExpiresAt > now)
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("grant_active")]);
        }

        return DomainResult.Success(
        [
            new ProjectChatBotAuthorityGranted(
                envelope.TenantId,
                envelope.AggregateId,
                command.PrincipalId,
                command.Operation,
                command.ActorClass,
                command.Origin,
                command.GrantId,
                command.ExpiresAt,
                envelope.UserId,
                envelope.CorrelationId,
                ReadTaskId(envelope),
                envelope.MessageId,
                Convert.ToHexString(SHA256.HashData(envelope.Payload)),
                now),
        ]);
    }

    /// <summary>Handles retained v1 <see cref="RevokeProjectChatBotAuthority"/>.</summary>
    /// <param name="command">The revoke command.</param>
    /// <param name="state">The current SDK state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult Handle(RevokeProjectChatBotAuthority command, ProjectSdkState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!string.Equals(envelope.Domain, DomainName, StringComparison.Ordinal))
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("invalid_domain")]);
        }

        if (_isCurrentOwner is null
            || !IsSafeGrantToken(envelope.TenantId)
            || !IsSafeGrantToken(envelope.AggregateId)
            || !IsSafeGrantToken(envelope.UserId)
            || !_isCurrentOwner(envelope.TenantId, envelope.UserId, nameof(RevokeProjectChatBotAuthority)))
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("owner_unavailable")]);
        }

        ProjectState domainState = state?.DomainState ?? ProjectState.Empty;
        if (!domainState.IsCreated
            || domainState.IsCreationAbandoned
            || domainState.TenantId != envelope.TenantId
            || domainState.ProjectId != envelope.AggregateId
            || domainState.Lifecycle != ProjectLifecycle.Active)
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("project_unavailable")]);
        }

        if (!ValidGrantScope(command.PrincipalId, command.Operation, command.ActorClass, command.Origin)
            || !IsSafeGrantToken(command.GrantId))
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("invalid_revoke")]);
        }

        string revokeScope = ChatBotGrantScope.For(command.PrincipalId, command.Operation, command.ActorClass, command.Origin);
        if (!domainState.ChatBotGrants.TryGetValue(revokeScope, out ProjectChatBotGrant? current)
            || current.GrantId != command.GrantId
            || current.Revoked)
        {
            return DomainResult.Rejection([new ProjectChatBotAuthorityRejected("grant_mismatch")]);
        }

        return DomainResult.Success(
        [
            new ProjectChatBotAuthorityRevoked(
                envelope.TenantId,
                envelope.AggregateId,
                command.PrincipalId,
                command.Operation,
                command.ActorClass,
                command.Origin,
                command.GrantId,
                envelope.UserId,
                envelope.CorrelationId,
                ReadTaskId(envelope),
                envelope.MessageId,
                Convert.ToHexString(SHA256.HashData(envelope.Payload)),
                _timeProvider.GetUtcNow()),
        ]);
    }

    /// <summary>Computes the canonical fingerprint for a bound Project creation command.</summary>
    /// <param name="canonicalName">The trimmed Project name.</param>
    /// <param name="canonicalDescription">The trimmed optional description.</param>
    /// <param name="canonicalSetupMetadata">The trimmed optional setup metadata.</param>
    /// <param name="folderId">The canonical Folder identifier.</param>
    /// <param name="canonicalFolderDisplayName">The trimmed optional Folder display name.</param>
    /// <param name="provisioningReceiptId">The optional provisioning receipt identifier.</param>
    /// <param name="folderClaimGeneration">The winning claim generation.</param>
    /// <param name="taskStepKey">The deterministic task-step key.</param>
    /// <returns>A <c>sha256:</c>-prefixed lowercase digest.</returns>
    public static string ComputeBoundCreationFingerprint(
        string canonicalName,
        string? canonicalDescription,
        string? canonicalSetupMetadata,
        string folderId,
        string? canonicalFolderDisplayName,
        string? provisioningReceiptId,
        long folderClaimGeneration,
        string taskStepKey)
    {
        ArgumentNullException.ThrowIfNull(canonicalName);
        ArgumentNullException.ThrowIfNull(folderId);
        ArgumentNullException.ThrowIfNull(taskStepKey);

        string[] lines =
        [
            "operation=CreateBoundProject",
            "field=description;" + FormatOptional(canonicalDescription),
            "field=folder_claim_generation;present=true;value=i:" + folderClaimGeneration.ToString(CultureInfo.InvariantCulture),
            "field=folder_display_name;" + FormatOptional(canonicalFolderDisplayName),
            "field=folder_id;present=true;value=s:" + Escape(folderId),
            "field=folder_provisioning_receipt_id;" + FormatOptional(provisioningReceiptId),
            "field=name;present=true;value=s:" + Escape(canonicalName),
            "field=setup_metadata;" + FormatOptional(canonicalSetupMetadata),
            "field=task_step_key;present=true;value=s:" + Escape(taskStepKey),
        ];
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        return "sha256:" + Convert.ToHexStringLower(digest);
    }

    /// <summary>Computes the canonical fingerprint for a terminal abandonment command.</summary>
    /// <param name="folderId">The optional Folder identifier.</param>
    /// <param name="folderClaimGeneration">The optional claim generation.</param>
    /// <param name="reasonCode">The canonical reason code.</param>
    /// <param name="taskStepKey">The deterministic task-step key.</param>
    /// <returns>A <c>sha256:</c>-prefixed lowercase digest.</returns>
    public static string ComputeAbandonmentFingerprint(
        string? folderId,
        long? folderClaimGeneration,
        string reasonCode,
        string taskStepKey)
    {
        ArgumentNullException.ThrowIfNull(reasonCode);
        ArgumentNullException.ThrowIfNull(taskStepKey);

        string[] lines =
        [
            "operation=AbandonProjectCreation",
            "field=folder_claim_generation;" + (folderClaimGeneration is { } gen
                ? "present=true;value=i:" + gen.ToString(CultureInfo.InvariantCulture)
                : "present=false;value=omitted"),
            "field=folder_id;" + FormatOptional(folderId),
            "field=reason_code;present=true;value=s:" + Escape(reasonCode),
            "field=task_step_key;present=true;value=s:" + Escape(taskStepKey),
        ];
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        return "sha256:" + Convert.ToHexStringLower(digest);
    }

    private static bool IsValidEnvelope(CommandEnvelope envelope, out string taskId)
    {
        taskId = ReadTaskId(envelope);
        return string.Equals(envelope.Domain, DomainName, StringComparison.Ordinal)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.TenantId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.UserId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.CorrelationId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(envelope.MessageId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(taskId);
    }

    private static string ReadTaskId(CommandEnvelope envelope)
        => envelope.Extensions is not null
            && envelope.Extensions.TryGetValue("taskId", out string? taskId)
            && !string.IsNullOrWhiteSpace(taskId)
            ? taskId
            : envelope.CorrelationId;

    private static bool TryParseProjectId(string aggregateId, out ProjectId? projectId)
    {
        try
        {
            projectId = new ProjectId(aggregateId);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            projectId = null;
            return false;
        }
    }

    private static bool IsSafeGrantToken(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= 128
            && value != "*"
            && value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');

    private static bool ValidGrantScope(string principal, string operation, string actorClass, string origin)
        => IsSafeGrantToken(principal)
            && IsSafeGrantToken(operation)
            && actorClass is "human" or "service" or "ai"
            && origin is "Api" or "Ui" or "Cli" or "Mcp" or "Worker" or "Mailbox" or "Ai";

    private static DomainResult ToDomainResult(ProjectResult result)
        => result.Code switch
        {
            ProjectResultCode.Created
                or ProjectResultCode.SetupUpdated
                or ProjectResultCode.Archived
                or ProjectResultCode.Restored
                or ProjectResultCode.FolderSet
                or ProjectResultCode.FileReferenceLinked
                or ProjectResultCode.FileReferenceUnlinked
                or ProjectResultCode.MemoryLinked
                or ProjectResultCode.MemoryUnlinked
                or ProjectResultCode.ProjectResolutionConfirmed => DomainResult.Success(result.Events),
            ProjectResultCode.IdempotentReplay => DomainResult.NoOp(),
            _ => DomainResult.Rejection([result.ToRejectionEvent()]),
        };

    private static DomainResult Rejection(
        CommandEnvelope envelope,
        ProjectResultCode code,
        string? rejectedField,
        string contractCommandType)
    {
        string safeProjectId = TryParseProjectId(envelope.AggregateId, out ProjectId? parsed)
            ? parsed!.Value
            : "unknown";
        ProjectResult synthetic = ProjectResult.Rejected(
            contractCommandType,
            envelope.TenantId,
            safeProjectId,
            envelope.UserId,
            envelope.CorrelationId,
            ReadTaskId(envelope),
            envelope.MessageId,
            code,
            rejectedField);
        return DomainResult.Rejection([synthetic.ToRejectionEvent()]);
    }

    private static string FormatOptional(string? value)
        => value is null ? "present=false;value=omitted" : "present=true;value=s:" + Escape(value);

    private static string Escape(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            builder.Append(c switch
            {
                '\\' => "\\\\",
                '\t' => "\\t",
                '\r' => "\\r",
                '\n' => "\\n",
                '\u2028' => "\\u2028",
                '\u2029' => "\\u2029",
                ';' => "\\;",
                '=' => "\\=",
                _ when char.IsControl(c) => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture),
                _ => c.ToString(),
            });
        }

        return builder.ToString();
    }
}
