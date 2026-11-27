// <copyright file="FolderFirstProjectCreationWorkflow.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.SdkWriter.Workflow;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService.DurableTasks;
using Hexalith.Folders.Server.Provisioning;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Projections.ProjectReads;
using Hexalith.Projects.SdkWriter.Admission;
using Hexalith.Projects.SdkWriter.Aggregates;
using Hexalith.Projects.SdkWriter.Claims;
using Hexalith.Projects.SdkWriter.Commands;
using Hexalith.Projects.SdkWriter.Storage;

/// <summary>
/// Story 7.1 Folder-first Durable Task workflow composing platform <see cref="DurableTaskAdmission"/>
/// and <see cref="DurableTaskExecution"/> with <see cref="ProjectSdkAggregate"/>,
/// <see cref="ProjectFolderClaimAggregate"/>, <see cref="IProjectFolderProvisioningClient"/>,
/// <see cref="ReadModelBackedEventStore"/>, and supported <see cref="ProjectReadModelFolder"/> projection confirmation.
/// </summary>
public sealed class FolderFirstProjectCreationWorkflow
{
    /// <summary>The versioned EventStore Durable Task admission operation identifier.</summary>
    public const string AdmissionOperation = "projects.create.v1";

    private const string AdmissionKeyPrefix = "durable-task-admission-v1-";
    private const string IndexKeyPrefix = "durable-task-id-v1-";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadModelStore _store;
    private readonly IReadModelBatchStore _batches;
    private readonly string _storeName;
    private readonly ReadModelBackedEventStore _eventStore;
    private readonly ProjectSdkAggregate _projectAggregate;
    private readonly ProjectFolderClaimAggregate _claimAggregate;
    private readonly IProjectFolderProvisioningClient _folders;
    private readonly IProjectCreationAuthorityChecker _authorityChecker;
    private readonly TimeProvider _timeProvider;
    private readonly WorkflowAdmissionAuthorizer _admissionAuthorizer;
    private readonly DurableTaskAdmission _admission;
    private readonly DurableTaskExecution _execution;

    /// <summary>Initializes a new instance of the <see cref="FolderFirstProjectCreationWorkflow"/> class.</summary>
    /// <param name="store">The conditional state store.</param>
    /// <param name="batches">The atomic read-model batch store.</param>
    /// <param name="storeName">The state-store component name.</param>
    /// <param name="eventStore">The shared-store event store.</param>
    /// <param name="projectAggregate">The SDK Project aggregate.</param>
    /// <param name="claimAggregate">The AD-35 Folder claim aggregate.</param>
    /// <param name="folders">The Folders provisioning/validation adapter.</param>
    /// <param name="authorityChecker">The live actor authority checker.</param>
    /// <param name="timeProvider">The UTC clock.</param>
    /// <param name="leaseDuration">Optional worker lease duration (defaults to 30 seconds).</param>
    /// <param name="dependencyFailureBudget">Optional dependency failure budget per step (defaults to 3).</param>
    /// <param name="faultOptions">Optional fault/pause hooks for testing.</param>
    public FolderFirstProjectCreationWorkflow(
        IReadModelStore store,
        IReadModelBatchStore batches,
        string storeName,
        ReadModelBackedEventStore eventStore,
        ProjectSdkAggregate projectAggregate,
        ProjectFolderClaimAggregate claimAggregate,
        IProjectFolderProvisioningClient folders,
        IProjectCreationAuthorityChecker authorityChecker,
        TimeProvider timeProvider,
        TimeSpan? leaseDuration = null,
        int dependencyFailureBudget = 3,
        ProjectCreationFaultOptions? faultOptions = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _batches = batches ?? throw new ArgumentNullException(nameof(batches));
        _storeName = !string.IsNullOrWhiteSpace(storeName) ? storeName : throw new ArgumentException("Store name is required.", nameof(storeName));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _projectAggregate = projectAggregate ?? throw new ArgumentNullException(nameof(projectAggregate));
        _claimAggregate = claimAggregate ?? throw new ArgumentNullException(nameof(claimAggregate));
        _folders = folders ?? throw new ArgumentNullException(nameof(folders));
        _authorityChecker = authorityChecker ?? throw new ArgumentNullException(nameof(authorityChecker));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        FaultOptions = faultOptions ?? new ProjectCreationFaultOptions();

        _admissionAuthorizer = new WorkflowAdmissionAuthorizer(_authorityChecker);
        _admission = new DurableTaskAdmission(
            _store,
            _storeName,
            _admissionAuthorizer,
            _timeProvider);
        _execution = new DurableTaskExecution(
            _admission,
            _timeProvider,
            leaseDuration ?? TimeSpan.FromSeconds(30),
            dependencyFailureBudget);
    }

    /// <summary>Gets the configurable fault/pause hooks for this workflow instance.</summary>
    public ProjectCreationFaultOptions FaultOptions { get; }

    /// <summary>Evaluates, admits, and advances a Folder-first Project creation request.</summary>
    /// <param name="request">The incoming creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Safe Metadata workflow result.</returns>
    public async Task<ProjectCreationWorkflowResult> CreateProjectAsync(
        ProjectCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        // 1. Evaluate P1 syntax, persona allowlist, metadata safety, and Selection Evidence binding
        //    (deferring the 5-minute expiry check until we verify whether this is a retry of an
        //    already-admitted task — Selection Evidence is an admission-time guard, not a forward-recovery guard).
        ProjectCreationAdmissionDecision preCheck = ProjectCreationAdmissionEvaluator.Evaluate(
            request,
            now,
            skipSelectionEvidenceExpiryCheck: true);
        if (preCheck.Outcome != ProjectCreationAdmissionOutcome.Admitted)
        {
            return FromAdmissionDecision(preCheck);
        }

        DurableTaskScope scope = new(request.TenantId, request.ActorPrincipalId, AdmissionOperation, request.IdempotencyKey);

        // 2. Check persisted admitted task BEFORE live authority so post-IrreversibleStep retries
        //    never block on authority revocation, pre-commit revocations abandon cleanly, and crashes
        //    between AdmitAsync and EnsureTaskMetadataAsync recreate metadata from the verified request.
        PersistedTaskState? existing = await ReadPersistedAdmissionStateAsync(scope, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!string.Equals(existing.Task.RequestSha256, preCheck.RequestSha256, StringComparison.Ordinal))
            {
                return new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Conflict,
                    409,
                    TaskId: existing.Task.TaskId,
                    ReasonCode: "idempotency_conflict");
            }

            ProjectCreationTaskMetadata existingMeta = await EnsureTaskMetadataAsync(existing.Task, request, preCheck, now, cancellationToken).ConfigureAwait(false);
            if (existing.Progress.IsTerminal || existing.Progress.NeedsAttention)
            {
                return BuildResultFromProgress(existing.Task, existing.Progress, existingMeta);
            }

            if (existing.Progress.IrreversibleStep is null)
            {
                ActorAuthorityStatus retryAuthority = await _authorityChecker
                    .CheckAsync(request.TenantId, request.ActorPrincipalId, request.CallerClass, cancellationToken)
                    .ConfigureAwait(false);
                if (retryAuthority == ActorAuthorityStatus.Unavailable)
                {
                    return new ProjectCreationWorkflowResult(
                        ProjectCreationWorkflowStatus.Unavailable,
                        503,
                        TaskId: existing.Task.TaskId,
                        ReasonCode: "authority_unavailable");
                }

                if (retryAuthority != ActorAuthorityStatus.Allowed)
                {
                    ProjectCreationWorkflowResult abandoned = await AbandonReservationAsync(
                        existing.Task.TaskId,
                        "actor_authority_revoked",
                        cancellationToken).ConfigureAwait(false);
                    return abandoned.Status == ProjectCreationWorkflowStatus.NeedsAttention
                        ? abandoned
                        : new ProjectCreationWorkflowResult(
                            ProjectCreationWorkflowStatus.Denied,
                            404,
                            TaskId: existing.Task.TaskId,
                            ReasonCode: "actor_authority_revoked");
                }
            }

            // Already admitted: recover forward even if the 5-minute Selection Evidence window has since expired.
            return await AdvanceExistingTaskAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        // 3. New admission: check live actor authority (503 Unavailable distinct from 404 Denied).
        ActorAuthorityStatus authorityStatus = await _authorityChecker
            .CheckAsync(request.TenantId, request.ActorPrincipalId, request.CallerClass, cancellationToken)
            .ConfigureAwait(false);
        if (authorityStatus == ActorAuthorityStatus.Unavailable)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Unavailable,
                503,
                ReasonCode: "authority_unavailable");
        }

        if (authorityStatus != ActorAuthorityStatus.Allowed)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Denied,
                404,
                ReasonCode: "actor_not_authorized");
        }

        // 4. New admission: enforce full 5-minute Selection Evidence expiry check.
        ProjectCreationAdmissionDecision strictCheck = ProjectCreationAdmissionEvaluator.Evaluate(
            request,
            now,
            skipSelectionEvidenceExpiryCheck: false);
        if (strictCheck.Outcome != ProjectCreationAdmissionOutcome.Admitted)
        {
            return FromAdmissionDecision(strictCheck);
        }

        // 5. Admit via platform DurableTaskAdmission, propagating caller class and distinguishing 503 Unavailable.
        DurableTaskAdmissionResult admissionResult;
        bool unavailableDuringAdmission;
        using (WorkflowAdmissionAuthorizer.AdmissionCallScope authScope = _admissionAuthorizer.BeginScope(request.CallerClass))
        {
            admissionResult = await _admission
                .AdmitAsync(scope, strictCheck.RequestSha256!, null, cancellationToken)
                .ConfigureAwait(false);
            unavailableDuringAdmission = authScope.ObservedUnavailable;
        }

        if (admissionResult.Outcome == DurableTaskAdmissionOutcome.Conflict)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Conflict,
                409,
                ReasonCode: "idempotency_conflict");
        }

        if (admissionResult.Outcome != DurableTaskAdmissionOutcome.Admitted || admissionResult.Task is null)
        {
            return unavailableDuringAdmission
                ? new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Unavailable,
                    503,
                    ReasonCode: "authority_unavailable")
                : new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Denied,
                    404,
                    ReasonCode: "admission_denied");
        }

        CommittedDurableTaskAdmission task = admissionResult.Task;
        ProjectCreationTaskMetadata meta = await EnsureTaskMetadataAsync(task, request, strictCheck, now, cancellationToken).ConfigureAwait(false);

        if (FaultOptions.PauseBeforeProvisioning)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.InProgress,
                202,
                TaskId: task.TaskId,
                ReasonCode: "paused_before_provisioning");
        }

        DurableTaskLease? lease = await _execution.TryAcquireAsync(task, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            PersistedTaskState? current = await ReadTrustedTaskByIdAsync(task.TaskId, cancellationToken).ConfigureAwait(false);
            return current is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.InProgress, 202, TaskId: task.TaskId)
                : BuildResultFromProgress(current.Task, current.Progress, meta);
        }

        return await ExecuteAdmittedTaskAsync(lease, meta, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resumes an already-admitted Durable Task on any replica, enforcing live pre-commit authority and digest equivalence.</summary>
    /// <param name="taskId">The platform Durable Task identifier.</param>
    /// <param name="retryRequest">Optional caller retry request whose normalized digest must match the committed admission.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Safe Metadata workflow result.</returns>
    public async Task<ProjectCreationWorkflowResult> ResumeTaskAsync(
        string taskId,
        ProjectCreationRequest? retryRequest = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        PersistedTaskState? persisted = await ReadTrustedTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (persisted is null)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Denied,
                404,
                ReasonCode: "task_not_found");
        }

        ProjectCreationTaskMetadata? meta = await ReadTaskMetadataAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (meta is null)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.NeedsAttention,
                409,
                TaskId: taskId,
                ReasonCode: "task_metadata_missing");
        }

        if (retryRequest is not null)
        {
            // Forward recovery skips 5-minute Selection Evidence expiry while enforcing syntax, auth, and RequestSha256 equivalence.
            ProjectCreationAdmissionDecision eval = ProjectCreationAdmissionEvaluator.Evaluate(
                retryRequest,
                _timeProvider.GetUtcNow(),
                skipSelectionEvidenceExpiryCheck: true);
            if (eval.Outcome != ProjectCreationAdmissionOutcome.Admitted)
            {
                return FromAdmissionDecision(eval);
            }

            if (!string.Equals(eval.RequestSha256, persisted.Task.RequestSha256, StringComparison.Ordinal))
            {
                return new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Conflict,
                    409,
                    TaskId: taskId,
                    ReasonCode: "idempotency_conflict");
            }
        }

        if (persisted.Progress.IsTerminal || persisted.Progress.NeedsAttention)
        {
            return BuildResultFromProgress(persisted.Task, persisted.Progress, meta);
        }

        // Enforce live original-actor authority before pre-commit steps (before IrreversibleStep is set).
        if (persisted.Progress.IrreversibleStep is null)
        {
            ActorAuthorityStatus authority = await _authorityChecker
                .CheckAsync(persisted.Task.Scope.TenantId, persisted.Task.Scope.ActorId, meta.CallerClass, cancellationToken)
                .ConfigureAwait(false);
            if (authority == ActorAuthorityStatus.Unavailable)
            {
                return new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Unavailable,
                    503,
                    TaskId: taskId,
                    ReasonCode: "authority_unavailable");
            }

            if (authority != ActorAuthorityStatus.Allowed)
            {
                ProjectCreationWorkflowResult abandoned = await AbandonReservationAsync(
                    taskId,
                    "actor_authority_revoked",
                    cancellationToken).ConfigureAwait(false);
                return abandoned.Status == ProjectCreationWorkflowStatus.NeedsAttention
                    ? abandoned
                    : new ProjectCreationWorkflowResult(
                        ProjectCreationWorkflowStatus.Denied,
                        404,
                        TaskId: taskId,
                        ReasonCode: "actor_authority_revoked");
            }
        }

        return await AdvanceExistingTaskAsync(persisted, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads trusted persisted Durable Task progress directly without masking on revoked/unavailable caller authority.</summary>
    /// <param name="taskId">The platform Durable Task identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Safe Metadata status snapshot, or null when absent.</returns>
    public async Task<ProjectCreationWorkflowResult?> GetTrustedStatusAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        PersistedTaskState? persisted = await ReadTrustedTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (persisted is null)
        {
            return null;
        }

        ProjectCreationTaskMetadata? meta = await ReadTaskMetadataAsync(taskId, cancellationToken).ConfigureAwait(false);
        return BuildResultFromProgress(persisted.Task, persisted.Progress, meta);
    }

    /// <summary>Reads the raw committed <see cref="DurableTaskProgress"/> for test assertions.</summary>
    /// <param name="taskId">The platform Durable Task identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted progress, or null when absent.</returns>
    public async Task<DurableTaskProgress?> GetTrustedProgressAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        PersistedTaskState? persisted = await ReadTrustedTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        return persisted?.Progress;
    }

    /// <summary>
    /// Abandons a pre-commit Project creation reservation: refuses after <c>IrreversibleStep</c>,
    /// recovers any uncommitted Step 2 AD-35 claim via stream lookup, commits the terminal
    /// <see cref="ProjectCreationAbandoned"/> fence BEFORE exact-generation claim release, projects
    /// abandonment to the supported read model, transitions the Durable Task to terminal
    /// <see cref="DurableTaskStatus.Rejected"/> (or <see cref="DurableTaskStatus.NeedsAttention"/> if
    /// release fails or budget is exhausted), and returns the actual persisted status.
    /// </summary>
    /// <param name="taskId">The platform Durable Task identifier.</param>
    /// <param name="reasonCode">The Safe Metadata abandonment reason code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Safe Metadata workflow result reflecting the actual persisted state.</returns>
    public async Task<ProjectCreationWorkflowResult> AbandonReservationAsync(
        string taskId,
        string reasonCode = "creation_abandoned",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        PersistedTaskState? persisted = await ReadTrustedTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (persisted is null)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Denied,
                404,
                ReasonCode: "task_not_found");
        }

        CommittedDurableTaskAdmission originalTask = persisted.Task;
        DurableTaskProgress currentProgress = persisted.Progress;
        ProjectCreationTaskMetadata? meta = await ReadTaskMetadataAsync(taskId, cancellationToken).ConfigureAwait(false);

        if (currentProgress.IsTerminal)
        {
            return currentProgress.Status == DurableTaskStatus.Succeeded
                ? new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Conflict,
                    409,
                    TaskId: originalTask.TaskId,
                    ProjectId: meta?.ProjectId,
                    ReasonCode: "task_already_succeeded")
                : BuildResultFromProgress(originalTask, currentProgress, meta);
        }

        if (currentProgress.NeedsAttention)
        {
            return BuildResultFromProgress(originalTask, currentProgress, meta);
        }

        if (currentProgress.IrreversibleStep is not null)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Conflict,
                409,
                TaskId: originalTask.TaskId,
                ReasonCode: "irreversible_step_reached");
        }

        if (meta is null)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.NeedsAttention,
                409,
                TaskId: originalTask.TaskId,
                ReasonCode: "task_metadata_missing");
        }

        DurableTaskLease? lease = await _execution.TryAcquireAsync(originalTask, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            PersistedTaskState? refreshed = await ReadTrustedTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
            return refreshed is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.Conflict, 409, TaskId: taskId, ReasonCode: "lease_unavailable")
                : BuildResultFromProgress(refreshed.Task, refreshed.Progress, meta);
        }

        // Re-read after lease acquisition to ensure IrreversibleStep was not reached concurrently.
        PersistedTaskState? leasedState = await ReadTrustedTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (leasedState is null)
        {
            return new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.NeedsAttention, 409, TaskId: taskId);
        }

        if (leasedState.Progress.IrreversibleStep is not null)
        {
            // Advance under the active lease so it finishes cleanly instead of dangling Running,
            // and never expose ProjectId unless supported read-model confirmation succeeded.
            ProjectCreationWorkflowResult advanced = await ExecuteAdmittedTaskAsync(lease, meta, cancellationToken).ConfigureAwait(false);
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Conflict,
                409,
                TaskId: originalTask.TaskId,
                ProjectId: advanced.Status == ProjectCreationWorkflowStatus.Succeeded ? meta.ProjectId : null,
                ReasonCode: "irreversible_step_reached");
        }

        (string? folderId, long? claimGeneration) = await ResolveHeldClaimAsync(
            originalTask,
            leasedState.Progress,
            meta,
            cancellationToken).ConfigureAwait(false);

        return await AbandonWithActiveLeaseAsync(lease, meta, folderId, claimGeneration, reasonCode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads and rehydrates a Project aggregate stream from the shared store.</summary>
    /// <param name="tenantId">The owning Tenant identifier.</param>
    /// <param name="projectId">The Project identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rehydrated <see cref="ProjectSdkState"/> and raw stream.</returns>
    public async Task<(ProjectSdkState State, StoredEventStream Stream)> RehydrateProjectStateAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        StoredEventStream stream = await _eventStore
            .ReadStreamAsync(tenantId, ProjectSdkAggregate.DomainName, projectId, cancellationToken)
            .ConfigureAwait(false);
        ProjectSdkState state = new();
        foreach (StoredStreamEvent stored in stream.Events)
        {
            IEventPayload ev = ReadModelBackedEventStore.DeserializeEvent(stored);
            ApplyProjectSdkEvent(state, ev);
        }

        return (state, stream);
    }

    /// <summary>Reads and rehydrates an AD-35 Folder claim stream from the shared store.</summary>
    /// <param name="tenantId">The owning Tenant identifier.</param>
    /// <param name="folderId">The Folder identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rehydrated <see cref="ProjectFolderClaimState"/> and raw stream.</returns>
    public async Task<(ProjectFolderClaimState State, StoredEventStream Stream)> RehydrateClaimStateAsync(
        string tenantId,
        string folderId,
        CancellationToken cancellationToken = default)
    {
        StoredEventStream stream = await _eventStore
            .ReadStreamAsync(tenantId, ProjectFolderClaimAggregate.DomainName, folderId, cancellationToken)
            .ConfigureAwait(false);
        ProjectFolderClaimState state = new();
        foreach (StoredStreamEvent stored in stream.Events)
        {
            IEventPayload ev = ReadModelBackedEventStore.DeserializeEvent(stored);
            switch (ev)
            {
                case ProjectFolderClaimAcquired acquired:
                    state.Apply(acquired);
                    break;
                case ProjectFolderClaimReleased released:
                    state.Apply(released);
                    break;
                case ProjectFolderClaimClosed closed:
                    state.Apply(closed);
                    break;
                case ProjectFolderClaimRejected rejected:
                    state.Apply(rejected);
                    break;
            }
        }

        return (state, stream);
    }

    /// <summary>
    /// Projects the persisted <c>projects</c> stream into the supported <see cref="ProjectReadModel"/>
    /// and <see cref="ProjectReadIndex"/> atomically with pending-batch recovery and recovery-safe manifests.
    /// </summary>
    /// <param name="tenantId">The owning Tenant identifier.</param>
    /// <param name="projectId">The Project identifier.</param>
    /// <param name="dispatchId">The deterministic dispatch identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when supported detail and tenant index confirm the current state; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ProjectToSupportedReadModelAsync(
        string tenantId,
        string projectId,
        string dispatchId,
        CancellationToken cancellationToken = default)
    {
        if (FaultOptions.ConsumeFailProjectionOnce())
        {
            return false;
        }

        StoredEventStream stream = await _eventStore
            .ReadStreamAsync(tenantId, ProjectSdkAggregate.DomainName, projectId, cancellationToken)
            .ConfigureAwait(false);

        // Guard empty streams so we never write a GlobalPosition == 0 / LastVerifiedAt == default ProjectReadIndex!
        if (stream.Events.Count == 0)
        {
            return false;
        }

        ProjectionEventDto[] dtos = stream.Events.Select(stored => new ProjectionEventDto(
            stored.EventTypeName,
            stored.Payload,
            "json",
            stored.SequenceNumber,
            stored.Timestamp,
            "projects",
            stored.CorrelationId,
            stored.ActorId,
            stored.GlobalPosition)).ToArray();

        ProjectionRequest request = new(tenantId, ProjectSdkAggregate.DomainName, projectId, dtos);
        string detailKey = ProjectReadModelKeys.Detail(tenantId, projectId);
        string indexKey = ProjectReadModelKeys.Index(tenantId);

        for (int attempt = 0; attempt < ReadModelWritePolicy.DefaultMaxAttempts; attempt++)
        {
            if (_batches is IReadModelPendingBatchRecoveryStore recovery)
            {
                ReadModelBatchScope recoveryScope = new(
                    _storeName,
                    tenantId,
                    ProjectSdkAggregate.DomainName,
                    projectId,
                    "project-reads",
                    dispatchId);
                foreach (string key in new[] { detailKey, indexKey })
                {
                    ReadModelPendingBatchRecoveryResult pending = await recovery
                        .RecoverPendingAsync(recoveryScope, key, cancellationToken)
                        .ConfigureAwait(false);
                    if (!pending.IsResolved)
                    {
                        return false;
                    }
                }
            }

            ReadModelEntry<ProjectReadModel> detailEntry = await _store
                .GetAsync<ProjectReadModel>(_storeName, detailKey, cancellationToken)
                .ConfigureAwait(false);
            ReadModelEntry<ProjectReadIndex> indexEntry = await _store
                .GetAsync<ProjectReadIndex>(_storeName, indexKey, cancellationToken)
                .ConfigureAwait(false);

            ProjectReadModel next = ProjectReadModelFolder.Fold(request, detailEntry.Value);

            if (next == detailEntry.Value)
            {
                if (next.Freshness != EvidenceFreshnessState.Current)
                {
                    return false;
                }

                // Short-circuit ONLY when ProjectReadIndex ALSO exists, matches TenantId, has consistent membership,
                // and does not mark this Project unavailable.
                if (indexEntry.Value is { } existingIndex
                    && string.Equals(existingIndex.TenantId, tenantId, StringComparison.Ordinal)
                    && !existingIndex.UnavailableProjectIds.Contains(projectId, StringComparer.Ordinal)
                    && (next.Detail is null || next.IsCreationAbandoned
                        ? !existingIndex.ProjectIds.Contains(projectId, StringComparer.Ordinal)
                        : existingIndex.ProjectIds.Contains(projectId, StringComparer.Ordinal)))
                {
                    return true;
                }
            }

            ProjectReadIndex? currentIndex = indexEntry.Value;
            if (currentIndex is not null && !string.Equals(currentIndex.TenantId, tenantId, StringComparison.Ordinal))
            {
                return false;
            }

            bool isVisibleProject = next.Detail is not null && !next.IsCreationAbandoned;
            string[] ids = (currentIndex?.ProjectIds ?? [])
                .Where(id => isVisibleProject || !string.Equals(id, projectId, StringComparison.Ordinal))
                .Concat(isVisibleProject ? [projectId] : [])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            bool advancesIndex = next.GlobalPosition > (currentIndex?.GlobalPosition ?? 0);
            ProjectReadIndex index = new(
                tenantId,
                ids,
                Math.Max(next.GlobalPosition, currentIndex?.GlobalPosition ?? 0),
                advancesIndex ? next.LastVerifiedAt : (currentIndex?.LastVerifiedAt ?? next.LastVerifiedAt))
            {
                UnavailableProjectIds = (currentIndex?.UnavailableProjectIds ?? [])
                    .Where(id => !string.Equals(id, projectId, StringComparison.Ordinal))
                    .Concat(next.Freshness == EvidenceFreshnessState.Current ? [] : [projectId])
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
            };

            string batchId = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new[] { dispatchId, detailEntry.ETag, indexEntry.ETag }, JsonOptions))));
            ReadModelBatch batch = new(
                new ReadModelBatchScope(_storeName, tenantId, ProjectSdkAggregate.DomainName, projectId, "project-reads", batchId),
                [
                    ReadModelBatchOperation.Write(detailKey, next, Concurrency(detailEntry.ETag)),
                    ReadModelBatchOperation.Write(indexKey, index, Concurrency(indexEntry.ETag)),
                ])
            {
                RetainManifestForRecovery = true,
            };

            ReadModelBatchResult result = await _batches.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return next.Freshness == EvidenceFreshnessState.Current;
            }

            if (result.ConflictKind != ReadModelBatchConflictKind.Optimistic)
            {
                return false;
            }
        }

        return false;
    }

    private async Task<ProjectCreationWorkflowResult> AdvanceExistingTaskAsync(
        PersistedTaskState persisted,
        CancellationToken cancellationToken)
    {
        ProjectCreationTaskMetadata? meta = await ReadTaskMetadataAsync(persisted.Task.TaskId, cancellationToken).ConfigureAwait(false);
        if (persisted.Progress.IsTerminal || persisted.Progress.NeedsAttention)
        {
            return BuildResultFromProgress(persisted.Task, persisted.Progress, meta);
        }

        if (meta is null)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.NeedsAttention,
                409,
                TaskId: persisted.Task.TaskId,
                ReasonCode: "task_metadata_missing");
        }

        DurableTaskLease? lease = await _execution.TryAcquireAsync(persisted.Task, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            PersistedTaskState? current = await ReadTrustedTaskByIdAsync(persisted.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return current is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.InProgress, 202, TaskId: persisted.Task.TaskId)
                : BuildResultFromProgress(current.Task, current.Progress, meta);
        }

        return await ExecuteAdmittedTaskAsync(lease, meta, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectCreationWorkflowResult> ExecuteAdmittedTaskAsync(
        DurableTaskLease lease,
        ProjectCreationTaskMetadata meta,
        CancellationToken cancellationToken)
    {
        // STEP 1: Provision new Folder (no-Folder path) OR validate supplied Folder.
        ProvisionOrValidateFolderStepEffect step1Effect = new(this, meta);
        string? step1ReceiptJson;
        try
        {
            step1ReceiptJson = await _execution.RunStepAsync(lease, 1, step1Effect, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            PersistedTaskState? afterStep1Failure = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterStep1Failure is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.WaitingForDependency, 503, TaskId: lease.Task.TaskId, ReasonCode: "folder_dependency_unavailable")
                : BuildResultFromProgress(afterStep1Failure.Task, afterStep1Failure.Progress, meta);
        }

        if (step1ReceiptJson is null)
        {
            PersistedTaskState? afterStep1Refusal = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterStep1Refusal is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.Rejected, 404, TaskId: lease.Task.TaskId)
                : BuildResultFromProgress(afterStep1Refusal.Task, afterStep1Refusal.Progress, meta);
        }

        FolderStepReceiptPayload folderReceipt = JsonSerializer.Deserialize<FolderStepReceiptPayload>(step1ReceiptJson, JsonOptions)!;

        if (FaultOptions.PauseAfterFolderReceipt)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.InProgress,
                202,
                TaskId: lease.Task.TaskId,
                FolderId: folderReceipt.FolderId,
                FolderProvisioningReceiptId: folderReceipt.ProvisioningReceiptId,
                ReasonCode: "paused_after_folder_receipt");
        }

        // STEP 2: Acquire exclusive AD-35 claim on Tenant/project-folder-claims/FolderId.
        AcquireFolderClaimStepEffect step2Effect = new(this, meta, folderReceipt);
        string? step2ReceiptJson;
        try
        {
            step2ReceiptJson = await _execution.RunStepAsync(lease, 2, step2Effect, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            PersistedTaskState? afterStep2Failure = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterStep2Failure is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.WaitingForDependency, 503, TaskId: lease.Task.TaskId, ReasonCode: "claim_dependency_unavailable")
                : BuildResultFromProgress(afterStep2Failure.Task, afterStep2Failure.Progress, meta);
        }

        if (step2ReceiptJson is null)
        {
            PersistedTaskState? afterStep2Refusal = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterStep2Refusal is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.Rejected, 404, TaskId: lease.Task.TaskId)
                : BuildResultFromProgress(afterStep2Refusal.Task, afterStep2Refusal.Progress, meta);
        }

        ClaimStepReceiptPayload claimReceipt = JsonSerializer.Deserialize<ClaimStepReceiptPayload>(step2ReceiptJson, JsonOptions)!;

        if (FaultOptions.PauseAfterClaim)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.InProgress,
                202,
                TaskId: lease.Task.TaskId,
                FolderId: folderReceipt.FolderId,
                FolderClaimGeneration: claimReceipt.Generation,
                FolderProvisioningReceiptId: folderReceipt.ProvisioningReceiptId,
                ReasonCode: "paused_after_claim");
        }

        // PRE-COMMIT LIVE AUTHORITY & SUPPLIED-FOLDER CHECK (ONLY before IrreversibleStep 3 is reached!).
        PersistedTaskState? preStep3State = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
        if (preStep3State is null || (preStep3State.Progress.Checkpoint < 3 && preStep3State.Progress.IrreversibleStep is null))
        {
            if (FaultOptions.BeforePreCommitCheckHook is not null)
            {
                await FaultOptions.BeforePreCommitCheckHook(cancellationToken).ConfigureAwait(false);
            }

            ActorAuthorityStatus preCommitAuthority = await _authorityChecker
                .CheckAsync(meta.TenantId, meta.ActorPrincipalId, meta.CallerClass, cancellationToken)
                .ConfigureAwait(false);
            if (preCommitAuthority == ActorAuthorityStatus.Unavailable)
            {
                await RecordPreCommitDependencyDelayAsync(lease, "Actor authority unavailable.", cancellationToken).ConfigureAwait(false);
                return new ProjectCreationWorkflowResult(
                    ProjectCreationWorkflowStatus.Unavailable,
                    503,
                    TaskId: lease.Task.TaskId,
                    FolderId: folderReceipt.FolderId,
                    FolderClaimGeneration: claimReceipt.Generation,
                    FolderProvisioningReceiptId: folderReceipt.ProvisioningReceiptId,
                    ReasonCode: "authority_unavailable");
            }

            if (preCommitAuthority != ActorAuthorityStatus.Allowed)
            {
                // Commit abandonment fence BEFORE releasing exact-generation claim (retains any provisioned Folder with 0 delete attempts!).
                return await AbandonWithActiveLeaseAsync(
                    lease,
                    meta,
                    folderReceipt.FolderId,
                    claimReceipt.Generation,
                    "precommit_authority_denied",
                    cancellationToken).ConfigureAwait(false);
            }

            if (folderReceipt.IsSupplied)
            {
                FolderProvisioningResult preCommitFolder;
                try
                {
                    preCommitFolder = await _folders
                        .ValidateSuppliedFolderAsync(meta.TenantId, meta.ActorPrincipalId, folderReceipt.FolderId, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    await RecordPreCommitDependencyDelayAsync(lease, "Supplied Folder pre-commit check unavailable.", cancellationToken).ConfigureAwait(false);
                    PersistedTaskState? afterFolderOutage = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
                    return afterFolderOutage is null
                        ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.WaitingForDependency, 503, TaskId: lease.Task.TaskId, FolderId: folderReceipt.FolderId, FolderClaimGeneration: claimReceipt.Generation, ReasonCode: "folder_dependency_unavailable")
                        : BuildResultFromProgress(afterFolderOutage.Task, afterFolderOutage.Progress, meta);
                }

                if (preCommitFolder.Outcome == FolderProvisioningOutcome.OrphanedOrReserved)
                {
                    _ = await _execution.RunStepAsync(lease, 3, new StaticOutcomeEffect(DurableTaskEffectLookup.Unresolved), cancellationToken).ConfigureAwait(false);
                    PersistedTaskState? afterOrphan = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
                    return afterOrphan is null
                        ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.NeedsAttention, 409, TaskId: lease.Task.TaskId)
                        : BuildResultFromProgress(afterOrphan.Task, afterOrphan.Progress, meta);
                }

                if (preCommitFolder.Outcome != FolderProvisioningOutcome.Provisioned
                    || !VerifyFolderSecurity(preCommitFolder, meta.ActorPrincipalId))
                {
                    return await AbandonWithActiveLeaseAsync(
                        lease,
                        meta,
                        folderReceipt.FolderId,
                        claimReceipt.Generation,
                        "precommit_supplied_folder_denied",
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // STEP 3 (Irreversible): Commit bound ProjectCreated fenced by winning claim generation.
        CommitBoundProjectStepEffect step3Effect = new(this, meta, folderReceipt, claimReceipt);
        string? step3Receipt;
        try
        {
            step3Receipt = await _execution.RunIrreversibleStepAsync(lease, 3, step3Effect, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            PersistedTaskState? afterStep3Failure = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterStep3Failure is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.WaitingForDependency, 503, TaskId: lease.Task.TaskId, ReasonCode: "project_commit_unavailable")
                : BuildResultFromProgress(afterStep3Failure.Task, afterStep3Failure.Progress, meta);
        }

        if (step3Receipt is null)
        {
            PersistedTaskState? afterStep3Refusal = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterStep3Refusal is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.Rejected, 409, TaskId: lease.Task.TaskId)
                : BuildResultFromProgress(afterStep3Refusal.Task, afterStep3Refusal.Progress, meta);
        }

        if (FaultOptions.PauseBeforeProjection)
        {
            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.InProgress,
                202,
                TaskId: lease.Task.TaskId,
                FolderId: folderReceipt.FolderId,
                FolderClaimGeneration: claimReceipt.Generation,
                FolderProvisioningReceiptId: folderReceipt.ProvisioningReceiptId,
                ReasonCode: "awaiting_read_model_confirmation");
        }

        // STEP 4: Confirm supported read model through DurableTaskExecution Step 4 so retries clear
        //         EffectPending cleanly before CompleteAsync and Unavailable escalates to NeedsAttention.
        ConfirmReadModelStepEffect step4Effect = new(this, meta);
        string? step4Receipt;
        try
        {
            step4Receipt = await _execution.RunStepAsync(lease, 4, step4Effect, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            PersistedTaskState? afterProjectionFailure = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return afterProjectionFailure is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.WaitingForDependency, 202, TaskId: lease.Task.TaskId, ReasonCode: "projection_lag")
                : BuildResultFromProgress(afterProjectionFailure.Task, afterProjectionFailure.Progress, meta);
        }

        if (step4Receipt is null)
        {
            PersistedTaskState? escalated = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
            return escalated is null
                ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.NeedsAttention, 409, TaskId: lease.Task.TaskId, ReasonCode: "projection_unavailable")
                : BuildResultFromProgress(escalated.Task, escalated.Progress, meta);
        }

        DurableTaskProgress completed = await _execution
            .CompleteAsync(lease, DurableTaskStatus.Succeeded, cancellationToken)
            .ConfigureAwait(false);

        return BuildResultFromProgress(lease.Task, completed, meta);
    }

    private async Task<ProjectCreationWorkflowResult> AbandonWithActiveLeaseAsync(
        DurableTaskLease lease,
        ProjectCreationTaskMetadata meta,
        string? folderId,
        long? claimGeneration,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        // 1. Commit terminal ProjectCreationAbandoned fence to reserved ProjectId stream BEFORE releasing claim!
        AbandonmentFenceOutcome fenceOutcome = await CommitAbandonmentFenceAsync(
            meta,
            folderId,
            claimGeneration,
            reasonCode,
            cancellationToken).ConfigureAwait(false);

        if (fenceOutcome == AbandonmentFenceOutcome.AlreadyCreated)
        {
            // Project was already created: settle pending Step 3 + Step 4 cleanly under the active lease
            // before calling CompleteAsync so no lease dangles Running and no unprojected ProjectId leaks!
            bool projected = await SettleAlreadyCreatedUnderLeaseAsync(lease, meta, folderId, claimGeneration, cancellationToken)
                .ConfigureAwait(false);

            return new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Conflict,
                409,
                TaskId: lease.Task.TaskId,
                ProjectId: projected ? meta.ProjectId : null,
                FolderId: folderId,
                FolderClaimGeneration: claimGeneration,
                ReasonCode: "project_already_created");
        }

        // 2. Project ProjectCreationAbandoned to supported read model (best-effort; stays invisible in ProjectReadIndex).
        try
        {
            _ = await ProjectToSupportedReadModelAsync(
                meta.TenantId,
                meta.ProjectId,
                $"{lease.Task.TaskId}/project-reads-abandoned",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Best-effort supported read-model projection for abandonment; fence is already committed.
        }

        // 3. Release exact-generation AD-35 claim BEFORE transitioning Durable Task to terminal Rejected.
        if (!string.IsNullOrWhiteSpace(folderId) && claimGeneration is { } generation)
        {
            bool released = await TryReleaseFolderClaimAsync(meta, folderId, generation, reasonCode, cancellationToken).ConfigureAwait(false);
            if (!released)
            {
                // Failed to release claim -> task MUST stay NeedsAttention (not Rejected!).
                await TransitionTaskToNeedsAttentionAsync(lease, cancellationToken).ConfigureAwait(false);
                PersistedTaskState? afterNeedsAttention = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
                return afterNeedsAttention is null
                    ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.NeedsAttention, 409, TaskId: lease.Task.TaskId, FolderId: folderId, FolderClaimGeneration: generation, ReasonCode: "claim_release_failed")
                    : BuildResultFromProgress(afterNeedsAttention.Task, afterNeedsAttention.Progress, meta);
            }
        }

        // 4. Transition Durable Task to terminal Rejected and ALWAYS re-read actual persisted status
        //    so a budget-exhausted NeedsAttention transition is never misreported as Rejected!
        await TransitionTaskToRejectedAsync(lease, cancellationToken).ConfigureAwait(false);
        PersistedTaskState? finalState = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
        return finalState is null
            ? new ProjectCreationWorkflowResult(ProjectCreationWorkflowStatus.Rejected, 404, TaskId: lease.Task.TaskId, ReasonCode: reasonCode)
            : BuildResultFromProgress(finalState.Task, finalState.Progress, meta);
    }

    private async Task<(string? FolderId, long? ClaimGeneration)> ResolveHeldClaimAsync(
        CommittedDurableTaskAdmission task,
        DurableTaskProgress progress,
        ProjectCreationTaskMetadata meta,
        CancellationToken cancellationToken)
    {
        if (progress.Receipts.Length >= 2)
        {
            ClaimStepReceiptPayload? claimReceipt = JsonSerializer.Deserialize<ClaimStepReceiptPayload>(progress.Receipts[1], JsonOptions);
            if (claimReceipt is not null)
            {
                return (claimReceipt.FolderId, claimReceipt.Generation);
            }
        }

        // Step 2 may have committed an AD-35 claim and crashed BEFORE persisting Receipts[1].
        // Recover the candidate FolderId from Receipts[0] or meta.SuppliedFolderId and inspect the claim stream directly!
        string? candidateFolderId = null;
        if (progress.Receipts.Length >= 1)
        {
            FolderStepReceiptPayload? folderReceipt = JsonSerializer.Deserialize<FolderStepReceiptPayload>(progress.Receipts[0], JsonOptions);
            candidateFolderId = folderReceipt?.FolderId;
        }

        candidateFolderId ??= meta.SuppliedFolderId;
        if (string.IsNullOrWhiteSpace(candidateFolderId))
        {
            return (null, null);
        }

        (ProjectFolderClaimState claimState, _) = await RehydrateClaimStateAsync(task.Scope.TenantId, candidateFolderId, cancellationToken).ConfigureAwait(false);
        string step2Key = $"{task.TaskId}/step/2";
        if (claimState.TryLookupByTaskStep(task.TaskId, step2Key, out ProjectFolderClaimReceipt? recovered)
            && recovered is not null
            && claimState.IsActive
            && !claimState.IsClosed
            && claimState.ActiveClaim is { } active
            && active.Generation == recovered.Generation
            && string.Equals(active.ProjectId, meta.ProjectId, StringComparison.Ordinal))
        {
            return (candidateFolderId, recovered.Generation);
        }

        return (candidateFolderId, null);
    }

    private async Task<AbandonmentFenceOutcome> CommitAbandonmentFenceAsync(
        ProjectCreationTaskMetadata meta,
        string? folderId,
        long? claimGeneration,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        string abandonStepKey = $"{meta.TaskId}/abandon";
        for (int attempt = 0; attempt < 8; attempt++)
        {
            (ProjectSdkState state, StoredEventStream stream) = await RehydrateProjectStateAsync(
                meta.TenantId,
                meta.ProjectId,
                cancellationToken).ConfigureAwait(false);

            if (state.DomainState.IsCreated)
            {
                return AbandonmentFenceOutcome.AlreadyCreated;
            }

            if (state.DomainState.IsCreationAbandoned)
            {
                return AbandonmentFenceOutcome.Abandoned;
            }

            AbandonProjectCreation command = new(folderId, claimGeneration, reasonCode, abandonStepKey);
            CommandEnvelope envelope = BuildEnvelope(
                meta.TenantId,
                ProjectSdkAggregate.DomainName,
                meta.ProjectId,
                nameof(AbandonProjectCreation),
                command,
                meta.ActorPrincipalId,
                meta.CorrelationId,
                meta.TaskId,
                abandonStepKey);
            DomainResult result = await ((IDomainProcessor)_projectAggregate).ProcessAsync(envelope, state).ConfigureAwait(false);
            if (result.IsNoOp)
            {
                return AbandonmentFenceOutcome.Abandoned;
            }

            if (!result.IsSuccess || result.Events.Count == 0)
            {
                throw new InvalidOperationException("Failed to produce ProjectCreationAbandoned fence event.");
            }

            StoredEventStreamAppendResult append = await _eventStore.TryAppendAsync(
                meta.TenantId,
                ProjectSdkAggregate.DomainName,
                meta.ProjectId,
                stream.Version,
                result.Events,
                meta.ActorPrincipalId,
                meta.CorrelationId,
                abandonStepKey,
                cancellationToken).ConfigureAwait(false);
            if (append.Succeeded)
            {
                return AbandonmentFenceOutcome.Abandoned;
            }
        }

        throw new InvalidOperationException("Abandonment fence append exceeded optimistic concurrency retries.");
    }

    private async Task<bool> TryReleaseFolderClaimAsync(
        ProjectCreationTaskMetadata meta,
        string folderId,
        long claimGeneration,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        if (FaultOptions.ConsumeFailReleaseClaimOnce())
        {
            return false;
        }

        string releaseStepKey = $"{meta.TaskId}/release-claim";
        for (int attempt = 0; attempt < 8; attempt++)
        {
            (ProjectFolderClaimState claimState, StoredEventStream stream) = await RehydrateClaimStateAsync(
                meta.TenantId,
                folderId,
                cancellationToken).ConfigureAwait(false);

            if (!claimState.IsActive
                || claimState.ActiveClaim is not { } active
                || !string.Equals(active.ProjectId, meta.ProjectId, StringComparison.Ordinal)
                || active.Generation != claimGeneration)
            {
                // Already released or superseded by a later generation (leave later holder intact).
                return true;
            }

            ReleaseProjectFolderClaim command = new(meta.ProjectId, claimGeneration, meta.TaskId, releaseStepKey, reasonCode);
            CommandEnvelope envelope = BuildEnvelope(
                meta.TenantId,
                ProjectFolderClaimAggregate.DomainName,
                folderId,
                nameof(ReleaseProjectFolderClaim),
                command,
                meta.ActorPrincipalId,
                meta.CorrelationId,
                meta.TaskId,
                releaseStepKey);
            DomainResult result = await ((IDomainProcessor)_claimAggregate).ProcessAsync(envelope, claimState).ConfigureAwait(false);
            if (result.IsNoOp)
            {
                return true;
            }

            if (!result.IsSuccess || result.Events.Count == 0)
            {
                return false;
            }

            StoredEventStreamAppendResult append = await _eventStore.TryAppendAsync(
                meta.TenantId,
                ProjectFolderClaimAggregate.DomainName,
                folderId,
                stream.Version,
                result.Events,
                meta.ActorPrincipalId,
                meta.CorrelationId,
                releaseStepKey,
                cancellationToken).ConfigureAwait(false);
            if (append.Succeeded)
            {
                return true;
            }
        }

        return false;
    }

    private async Task TransitionTaskToRejectedAsync(DurableTaskLease lease, CancellationToken cancellationToken)
    {
        PersistedTaskState? current = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
        if (current is null || current.Progress.IsTerminal || current.Progress.NeedsAttention)
        {
            return;
        }

        if (!current.Progress.EffectPending)
        {
            _ = await _execution.CompleteAsync(lease, DurableTaskStatus.Rejected, cancellationToken).ConfigureAwait(false);
            return;
        }

        // EffectPending is true (e.g. step crashed mid-flight): complete the pending step with Rejected.
        int nextStep = current.Progress.Checkpoint + 1;
        _ = await _execution
            .RunStepAsync(lease, nextStep, new StaticOutcomeEffect(DurableTaskEffectLookup.Rejected), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task TransitionTaskToNeedsAttentionAsync(DurableTaskLease lease, CancellationToken cancellationToken)
    {
        PersistedTaskState? current = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
        if (current is null || current.Progress.IsTerminal || current.Progress.NeedsAttention)
        {
            return;
        }

        int nextStep = current.Progress.Checkpoint + 1;
        _ = await _execution
            .RunStepAsync(lease, nextStep, new StaticOutcomeEffect(DurableTaskEffectLookup.Unresolved), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ProjectCreationTaskMetadata> EnsureTaskMetadataAsync(
        CommittedDurableTaskAdmission task,
        ProjectCreationRequest request,
        ProjectCreationAdmissionDecision decision,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ProjectCreationTaskMetadata? existing = await ReadTaskMetadataAsync(task.TaskId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        string reservedProjectId = UniqueIdHelper.GenerateSortableUniqueStringId();
        string correlationId = !string.IsNullOrWhiteSpace(request.CorrelationId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(request.CorrelationId)
            ? request.CorrelationId
            : task.TaskId;
        ProjectCreationTaskMetadata created = new(
            task.TaskId,
            task.Scope.TenantId,
            task.Scope.ActorId,
            request.CallerClass,
            reservedProjectId,
            decision.CanonicalName!,
            decision.CanonicalDescription,
            decision.CanonicalSetupMetadata,
            decision.SuppliedFolderId,
            task.RequestSha256,
            correlationId,
            now);

        bool saved = await _store
            .TrySaveAsync(_storeName, TaskMetadataKey(task.TaskId), created, string.Empty, cancellationToken)
            .ConfigureAwait(false);
        if (saved)
        {
            return created;
        }

        return await ReadTaskMetadataAsync(task.TaskId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Task metadata reservation could not be confirmed.");
    }

    private async Task<ProjectCreationTaskMetadata?> ReadTaskMetadataAsync(string taskId, CancellationToken cancellationToken)
    {
        ReadModelEntry<ProjectCreationTaskMetadata> entry = await _store
            .GetAsync<ProjectCreationTaskMetadata>(_storeName, TaskMetadataKey(taskId), cancellationToken)
            .ConfigureAwait(false);
        return entry.Value;
    }

    private async Task<PersistedTaskState?> ReadPersistedAdmissionStateAsync(DurableTaskScope scope, CancellationToken cancellationToken)
    {
        string key = AdmissionKey(scope);
        return await ReadPersistedAdmissionByKeyAsync(key, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PersistedTaskState?> ReadTrustedTaskByIdAsync(string taskId, CancellationToken cancellationToken)
    {
        string indexKey = IndexKey(taskId);
        ReadModelEntry<JsonObject> indexEntry = await _store
            .GetAsync<JsonObject>(_storeName, indexKey, cancellationToken)
            .ConfigureAwait(false);
        string? admissionKey = indexEntry.Value?["admissionKey"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(admissionKey))
        {
            return null;
        }

        return await ReadPersistedAdmissionByKeyAsync(admissionKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PersistedTaskState?> ReadPersistedAdmissionByKeyAsync(string admissionKey, CancellationToken cancellationToken)
    {
        ReadModelEntry<JsonObject> entry = await _store
            .GetAsync<JsonObject>(_storeName, admissionKey, cancellationToken)
            .ConfigureAwait(false);
        if (entry.Value is null || entry.Value["task"] is not JsonObject taskNode)
        {
            return null;
        }

        CommittedDurableTaskAdmission? task = taskNode.Deserialize<CommittedDurableTaskAdmission>(JsonOptions);
        if (task is null)
        {
            return null;
        }

        DurableTaskProgress progress = new([], false, DurableTaskStatus.Pending);
        if (entry.Value["execution"] is JsonObject executionNode
            && executionNode["progress"] is JsonObject progressNode)
        {
            progress = progressNode.Deserialize<DurableTaskProgress>(JsonOptions)
                ?? new DurableTaskProgress([], false, DurableTaskStatus.Unknown);
        }

        return new PersistedTaskState(task, progress);
    }

    private static ProjectCreationWorkflowResult BuildResultFromProgress(
        CommittedDurableTaskAdmission task,
        DurableTaskProgress progress,
        ProjectCreationTaskMetadata? meta)
    {
        FolderStepReceiptPayload? folderReceipt = progress.Receipts.Length >= 1
            ? TryDeserialize<FolderStepReceiptPayload>(progress.Receipts[0])
            : null;
        ClaimStepReceiptPayload? claimReceipt = progress.Receipts.Length >= 2
            ? TryDeserialize<ClaimStepReceiptPayload>(progress.Receipts[1])
            : null;

        return progress.Status switch
        {
            DurableTaskStatus.Succeeded => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Succeeded,
                200,
                TaskId: task.TaskId,
                ProjectId: meta?.ProjectId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId),

            DurableTaskStatus.Rejected => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Rejected,
                404,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "task_rejected"),

            DurableTaskStatus.Failed => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Rejected,
                500,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "task_failed"),

            DurableTaskStatus.Cancelled => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Rejected,
                409,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "task_cancelled"),

            DurableTaskStatus.NeedsAttention => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.NeedsAttention,
                409,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "needs_attention"),

            DurableTaskStatus.WaitingForDependency => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.WaitingForDependency,
                202,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "waiting_for_dependency"),

            DurableTaskStatus.Pending or DurableTaskStatus.Running => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.InProgress,
                202,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "in_progress"),

            // Unknown or unmapped status MUST fail closed as NeedsAttention (never fall through to InProgress!).
            _ => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.NeedsAttention,
                409,
                TaskId: task.TaskId,
                FolderId: folderReceipt?.FolderId,
                FolderClaimGeneration: claimReceipt?.Generation,
                FolderProvisioningReceiptId: folderReceipt?.ProvisioningReceiptId,
                ReasonCode: "unknown_task_status"),
        };
    }

    private static bool VerifyFolderSecurity(FolderProvisioningResult result, string originalActorId)
        => string.Equals(result.SoleManagerActorId, originalActorId, StringComparison.Ordinal)
            && !result.InheritOrganizationPermissions
            && !result.WorkloadHasFolderAuthority
            && !string.IsNullOrWhiteSpace(result.FolderId);

    private static ProjectCreationWorkflowResult FromAdmissionDecision(ProjectCreationAdmissionDecision decision)
        => decision.Outcome switch
        {
            ProjectCreationAdmissionOutcome.ValidationFailure => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.ValidationFailure,
                decision.HttpStatusCode,
                RejectedField: decision.RejectedField,
                ReasonCode: decision.ReasonCode),
            ProjectCreationAdmissionOutcome.RequestPreview => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.RequestPreview,
                decision.HttpStatusCode,
                ReasonCode: decision.ReasonCode),
            _ => new ProjectCreationWorkflowResult(
                ProjectCreationWorkflowStatus.Denied,
                decision.HttpStatusCode,
                ReasonCode: decision.ReasonCode),
        };

    private static CommandEnvelope BuildEnvelope<TCommand>(
        string tenantId,
        string domain,
        string aggregateId,
        string commandType,
        TCommand payload,
        string actorId,
        string correlationId,
        string taskId,
        string messageId)
        => new(
            MessageId: messageId,
            TenantId: tenantId,
            Domain: domain,
            AggregateId: aggregateId,
            CommandType: commandType,
            Payload: JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions),
            CorrelationId: correlationId,
            CausationId: null,
            UserId: actorId,
            Extensions: new Dictionary<string, string>(StringComparer.Ordinal) { ["taskId"] = taskId });

    private static void ApplyProjectSdkEvent(ProjectSdkState state, IEventPayload ev)
    {
        switch (ev)
        {
            case ProjectCreated e: state.Apply(e); break;
            case ProjectCreationAbandoned e: state.Apply(e); break;
            case ProjectSetupUpdated e: state.Apply(e); break;
            case ProjectFolderSet e: state.Apply(e); break;
            case ProjectFolderCreationPending e: state.Apply(e); break;
            case FileReferenceLinked e: state.Apply(e); break;
            case FileReferenceUnlinked e: state.Apply(e); break;
            case MemoryLinked e: state.Apply(e); break;
            case MemoryUnlinked e: state.Apply(e); break;
            case ProjectArchived e: state.Apply(e); break;
            case ProjectRestored e: state.Apply(e); break;
            case ProjectResolutionConfirmed e: state.Apply(e); break;
            case ProjectChatBotAuthorityGranted e: state.Apply(e); break;
            case ProjectChatBotAuthorityRevoked e: state.Apply(e); break;
            case ProjectCreationRejected e: state.Apply(e); break;
            case ProjectArchiveRejected e: state.Apply(e); break;
            case ProjectRestoreRejected e: state.Apply(e); break;
            case ProjectSetupUpdateRejected e: state.Apply(e); break;
            case ProjectReferenceLinkRejected e: state.Apply(e); break;
            case ProjectReferenceUnlinkRejected e: state.Apply(e); break;
            case ProjectResolutionConfirmationRejected e: state.Apply(e); break;
            case ProjectChatBotAuthorityRejected e: state.Apply(e); break;
            default:
                throw new InvalidOperationException($"Unsupported Project stream event '{ev.GetType().FullName}'.");
        }
    }

    private static string AdmissionKey(DurableTaskScope scope)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new[] { scope.TenantId, scope.ActorId, scope.Operation, scope.IdempotencyKey });
        return AdmissionKeyPrefix + Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static string IndexKey(string taskId)
        => IndexKeyPrefix + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(taskId)));

    private static string TaskMetadataKey(string taskId) => "story71:task-meta:v1:" + taskId;

    private static ReadModelBatchConcurrency Concurrency(string? etag)
        => string.IsNullOrEmpty(etag) ? ReadModelBatchConcurrency.CreateOnly : ReadModelBatchConcurrency.Match(etag);

    private static T? TryDeserialize<T>(string json)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private enum AbandonmentFenceOutcome
    {
        Abandoned,
        AlreadyCreated,
    }

    private sealed record PersistedTaskState(CommittedDurableTaskAdmission Task, DurableTaskProgress Progress);

    private async Task RecordPreCommitDependencyDelayAsync(
        DurableTaskLease lease,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await _execution
                .RunStepAsync(lease, 3, new StaticThrowingEffect(message), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Recorded cleanly as WaitingForDependency (or NeedsAttention if budget exhausted) without setting IrreversibleStep.
        }
    }

    private async Task<bool> SettleAlreadyCreatedUnderLeaseAsync(
        DurableTaskLease lease,
        ProjectCreationTaskMetadata meta,
        string? folderId,
        long? claimGeneration,
        CancellationToken cancellationToken)
    {
        PersistedTaskState? current = await ReadTrustedTaskByIdAsync(lease.Task.TaskId, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return false;
        }

        if (current.Progress.Status == DurableTaskStatus.Succeeded)
        {
            return true;
        }

        if (current.Progress.IsTerminal || current.Progress.NeedsAttention)
        {
            return false;
        }

        // Settle any pending Step 3 (or prior uncommitted steps) before running Step 4 confirmation.
        if (current.Progress.Checkpoint < 3)
        {
            (ProjectSdkState state, _) = await RehydrateProjectStateAsync(meta.TenantId, meta.ProjectId, cancellationToken).ConfigureAwait(false);
            string resolvedFolderId = folderId ?? state.DomainState.ProjectFolder?.FolderId ?? meta.SuppliedFolderId ?? string.Empty;
            long resolvedGeneration = claimGeneration ?? state.DomainState.FolderClaimGeneration ?? 1;

            FolderStepReceiptPayload folderReceipt = current.Progress.Receipts.Length >= 1
                ? TryDeserialize<FolderStepReceiptPayload>(current.Progress.Receipts[0])
                    ?? new FolderStepReceiptPayload(resolvedFolderId, state.DomainState.ProjectFolder?.DisplayName, state.DomainState.FolderProvisioningReceiptId, !string.IsNullOrWhiteSpace(meta.SuppliedFolderId))
                : new FolderStepReceiptPayload(resolvedFolderId, state.DomainState.ProjectFolder?.DisplayName, state.DomainState.FolderProvisioningReceiptId, !string.IsNullOrWhiteSpace(meta.SuppliedFolderId));

            ClaimStepReceiptPayload claimReceipt = current.Progress.Receipts.Length >= 2
                ? TryDeserialize<ClaimStepReceiptPayload>(current.Progress.Receipts[1])
                    ?? new ClaimStepReceiptPayload(resolvedFolderId, meta.ProjectId, resolvedGeneration, $"recovered:{meta.TaskId}")
                : new ClaimStepReceiptPayload(resolvedFolderId, meta.ProjectId, resolvedGeneration, $"recovered:{meta.TaskId}");

            if (current.Progress.Checkpoint == 0)
            {
                _ = await _execution.RunStepAsync(lease, 1, new StaticCommittedEffect(JsonSerializer.Serialize(folderReceipt, JsonOptions)), cancellationToken).ConfigureAwait(false);
            }

            if (current.Progress.Checkpoint <= 1)
            {
                _ = await _execution.RunStepAsync(lease, 2, new StaticCommittedEffect(JsonSerializer.Serialize(claimReceipt, JsonOptions)), cancellationToken).ConfigureAwait(false);
            }

            string? step3Receipt;
            try
            {
                step3Receipt = await _execution.RunIrreversibleStepAsync(
                    lease,
                    3,
                    new CommitBoundProjectStepEffect(this, meta, folderReceipt, claimReceipt),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (step3Receipt is null)
            {
                return false;
            }
        }

        string? step4Receipt;
        try
        {
            step4Receipt = await _execution
                .RunStepAsync(lease, 4, new ConfirmReadModelStepEffect(this, meta), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (step4Receipt is null)
        {
            return false;
        }

        _ = await _execution.CompleteAsync(lease, DurableTaskStatus.Succeeded, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private sealed class WorkflowAdmissionAuthorizer(IProjectCreationAuthorityChecker checker) : IDurableTaskAdmissionAuthorizer
    {
        private readonly AsyncLocal<AdmissionCallScope?> _currentScope = new();

        public AdmissionCallScope BeginScope(ProjectCreationCallerClass callerClass)
        {
            AdmissionCallScope scope = new(this, _currentScope.Value, callerClass);
            _currentScope.Value = scope;
            return scope;
        }

        public async ValueTask<DurableTaskAdmissionAuthority> AuthorizeAsync(
            DurableTaskScope scope,
            string requestSha256,
            CancellationToken cancellationToken)
        {
            AdmissionCallScope? callScope = _currentScope.Value;
            ProjectCreationCallerClass callerClass = callScope?.CallerClass ?? ProjectCreationCallerClass.ProjectUser;
            ActorAuthorityStatus status = await checker
                .CheckAsync(scope.TenantId, scope.ActorId, callerClass, cancellationToken)
                .ConfigureAwait(false);
            if (status == ActorAuthorityStatus.Unavailable && callScope is not null)
            {
                callScope.ObservedUnavailable = true;
            }

            return status == ActorAuthorityStatus.Allowed
                ? new DurableTaskAdmissionAuthority(Allowed: true, RequiresConfirmation: false, ConfirmationBindingSha256: null)
                : new DurableTaskAdmissionAuthority(Allowed: false, RequiresConfirmation: false, ConfirmationBindingSha256: null);
        }

        internal sealed class AdmissionCallScope(
            WorkflowAdmissionAuthorizer owner,
            AdmissionCallScope? prior,
            ProjectCreationCallerClass callerClass) : IDisposable
        {
            public ProjectCreationCallerClass CallerClass { get; } = callerClass;

            public bool ObservedUnavailable { get; set; }

            public void Dispose() => owner._currentScope.Value = prior;
        }
    }

    private sealed class ProvisionOrValidateFolderStepEffect(
        FolderFirstProjectCreationWorkflow workflow,
        ProjectCreationTaskMetadata meta) : IDurableTaskEffect
    {
        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(meta.SuppliedFolderId))
            {
                FolderProvisioningResult supplied = await workflow._folders
                    .ValidateSuppliedFolderAsync(meta.TenantId, meta.ActorPrincipalId, meta.SuppliedFolderId, cancellationToken)
                    .ConfigureAwait(false);
                return MapSupplied(supplied);
            }

            FolderProvisioningRequest request = BuildProvisioningRequest(step);
            FolderProvisioningResult lookup = await workflow._folders
                .LookupProvisioningAsync(meta.TenantId, meta.ActorPrincipalId, request, cancellationToken)
                .ConfigureAwait(false);
            return MapLookup(lookup);
        }

        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(meta.SuppliedFolderId))
            {
                FolderProvisioningResult supplied = await workflow._folders
                    .ValidateSuppliedFolderAsync(meta.TenantId, meta.ActorPrincipalId, meta.SuppliedFolderId, cancellationToken)
                    .ConfigureAwait(false);
                return MapSupplied(supplied);
            }

            FolderProvisioningRequest request = BuildProvisioningRequest(step);
            FolderProvisioningResult created = await workflow._folders
                .ProvisionFolderAsync(meta.TenantId, meta.ActorPrincipalId, request, cancellationToken)
                .ConfigureAwait(false);
            return MapProvision(created);
        }

        private FolderProvisioningRequest BuildProvisioningRequest(DurableTaskEffectStep step)
        {
            string digest = FoldersAggregateProvisioningClient.ComputeRequestSha256(meta.CanonicalName);
            return new FolderProvisioningRequest(
                step.Task.TaskId,
                step.Key,
                AdmissionOperation,
                digest,
                step.Fence,
                ExpectedVersion: 0,
                meta.CanonicalName);
        }

        private (DurableTaskEffectLookup Outcome, string? Receipt) MapLookup(FolderProvisioningResult result)
            => result.Outcome switch
            {
                FolderProvisioningOutcome.Absent => (DurableTaskEffectLookup.Absent, null),
                FolderProvisioningOutcome.Provisioned => VerifyFolderSecurity(result, meta.ActorPrincipalId)
                    ? (DurableTaskEffectLookup.Committed, SerializeReceipt(result, isSupplied: false))
                    : (DurableTaskEffectLookup.Unresolved, null),
                FolderProvisioningOutcome.OrphanedOrReserved => (DurableTaskEffectLookup.Unresolved, null),
                _ => (DurableTaskEffectLookup.Rejected, null),
            };

        private (DurableTaskEffectLookup Outcome, string? Receipt) MapProvision(FolderProvisioningResult result)
            => result.Outcome switch
            {
                FolderProvisioningOutcome.Provisioned => VerifyFolderSecurity(result, meta.ActorPrincipalId)
                    ? (DurableTaskEffectLookup.Committed, SerializeReceipt(result, isSupplied: false))
                    : (DurableTaskEffectLookup.Unresolved, null),
                FolderProvisioningOutcome.OrphanedOrReserved => (DurableTaskEffectLookup.Unresolved, null),
                _ => (DurableTaskEffectLookup.Rejected, null),
            };

        private (DurableTaskEffectLookup Outcome, string? Receipt) MapSupplied(FolderProvisioningResult result)
            => result.Outcome switch
            {
                // Preserve the supplied Folder's own display name; enforce security invariants on supplied path too.
                FolderProvisioningOutcome.Provisioned => VerifyFolderSecurity(result, meta.ActorPrincipalId)
                    ? (DurableTaskEffectLookup.Committed, SerializeReceipt(result, isSupplied: true))
                    : (DurableTaskEffectLookup.Rejected, null),
                // Orphaned or reserved Folders map to NeedsAttention (Unresolved), NEVER Rejected!
                FolderProvisioningOutcome.OrphanedOrReserved => (DurableTaskEffectLookup.Unresolved, null),
                _ => (DurableTaskEffectLookup.Rejected, null),
            };

        private static string SerializeReceipt(FolderProvisioningResult result, bool isSupplied)
            => JsonSerializer.Serialize(
                new FolderStepReceiptPayload(result.FolderId!, result.DisplayName, result.ReceiptId, isSupplied),
                JsonOptions);
    }

    private sealed class AcquireFolderClaimStepEffect(
        FolderFirstProjectCreationWorkflow workflow,
        ProjectCreationTaskMetadata meta,
        FolderStepReceiptPayload folderReceipt) : IDurableTaskEffect
    {
        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            (ProjectFolderClaimState claimState, _) = await workflow
                .RehydrateClaimStateAsync(meta.TenantId, folderReceipt.FolderId, cancellationToken)
                .ConfigureAwait(false);

            if (claimState.TryLookupByTaskStep(step.Task.TaskId, step.Key, out ProjectFolderClaimReceipt? receipt)
                && receipt is not null)
            {
                // Verify the claim is STILL active, not closed, and matches exact generation & ProjectId!
                if (claimState.IsActive
                    && !claimState.IsClosed
                    && claimState.ActiveClaim is { } active
                    && active.Generation == receipt.Generation
                    && string.Equals(active.ProjectId, meta.ProjectId, StringComparison.Ordinal))
                {
                    return (DurableTaskEffectLookup.Committed, SerializeClaimReceipt(receipt));
                }

                return (DurableTaskEffectLookup.Rejected, null);
            }

            return (DurableTaskEffectLookup.Absent, null);
        }

        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            if (workflow.FaultOptions.BeforeClaimAppendBarrier is not null)
            {
                await workflow.FaultOptions.BeforeClaimAppendBarrier(cancellationToken).ConfigureAwait(false);
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                (ProjectFolderClaimState claimState, StoredEventStream stream) = await workflow
                    .RehydrateClaimStateAsync(meta.TenantId, folderReceipt.FolderId, cancellationToken)
                    .ConfigureAwait(false);

                if (claimState.TryLookupByTaskStep(step.Task.TaskId, step.Key, out ProjectFolderClaimReceipt? existing)
                    && existing is not null)
                {
                    if (claimState.IsActive
                        && !claimState.IsClosed
                        && claimState.ActiveClaim is { } active
                        && active.Generation == existing.Generation
                        && string.Equals(active.ProjectId, meta.ProjectId, StringComparison.Ordinal))
                    {
                        return (DurableTaskEffectLookup.Committed, SerializeClaimReceipt(existing));
                    }

                    return (DurableTaskEffectLookup.Rejected, null);
                }

                AcquireProjectFolderClaim command = new(meta.ProjectId, step.Task.TaskId, step.Key);
                CommandEnvelope envelope = BuildEnvelope(
                    meta.TenantId,
                    ProjectFolderClaimAggregate.DomainName,
                    folderReceipt.FolderId,
                    nameof(AcquireProjectFolderClaim),
                    command,
                    meta.ActorPrincipalId,
                    meta.CorrelationId,
                    step.Task.TaskId,
                    step.Key);
                DomainResult result = await ((IDomainProcessor)workflow._claimAggregate)
                    .ProcessAsync(envelope, claimState)
                    .ConfigureAwait(false);

                if (!result.IsSuccess || result.Events is not [ProjectFolderClaimAcquired acquired])
                {
                    // If a new Folder was already provisioned for a no-Folder request, retain it in NeedsAttention.
                    return folderReceipt.IsSupplied
                        ? (DurableTaskEffectLookup.Rejected, null)
                        : (DurableTaskEffectLookup.Unresolved, null);
                }

                StoredEventStreamAppendResult append = await workflow._eventStore.TryAppendAsync(
                    meta.TenantId,
                    ProjectFolderClaimAggregate.DomainName,
                    folderReceipt.FolderId,
                    stream.Version,
                    result.Events,
                    meta.ActorPrincipalId,
                    meta.CorrelationId,
                    step.Key,
                    cancellationToken).ConfigureAwait(false);
                if (append.Succeeded)
                {
                    if (workflow.FaultOptions.ConsumeDropNextClaimReplyOnce())
                    {
                        throw new TimeoutException("Simulated lost reply after durable AD-35 claim acquisition.");
                    }

                    ProjectFolderClaimReceipt receipt = new(
                        acquired.TenantId,
                        acquired.FolderId,
                        acquired.ProjectId,
                        acquired.Generation,
                        acquired.TaskId,
                        acquired.TaskStepKey,
                        acquired.ReceiptId,
                        acquired.OccurredAt);
                    return (DurableTaskEffectLookup.Committed, SerializeClaimReceipt(receipt));
                }
            }

            throw new IOException("AD-35 claim acquisition exceeded optimistic concurrency attempts.");
        }

        private static string SerializeClaimReceipt(ProjectFolderClaimReceipt receipt)
            => JsonSerializer.Serialize(
                new ClaimStepReceiptPayload(receipt.FolderId, receipt.ProjectId, receipt.Generation, receipt.ReceiptId),
                JsonOptions);
    }

    private sealed class CommitBoundProjectStepEffect(
        FolderFirstProjectCreationWorkflow workflow,
        ProjectCreationTaskMetadata meta,
        FolderStepReceiptPayload folderReceipt,
        ClaimStepReceiptPayload claimReceipt) : IDurableTaskEffect
    {
        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            (ProjectSdkState state, StoredEventStream stream) = await workflow
                .RehydrateProjectStateAsync(meta.TenantId, meta.ProjectId, cancellationToken)
                .ConfigureAwait(false);

            if (state.DomainState.IsCreated)
            {
                bool matchesBinding = string.Equals(state.DomainState.ProjectFolder?.FolderId, folderReceipt.FolderId, StringComparison.Ordinal)
                    && state.DomainState.FolderClaimGeneration == claimReceipt.Generation;
                return matchesBinding
                    ? (DurableTaskEffectLookup.Committed, $"project-created:{meta.ProjectId}:v{stream.Version.ToString(CultureInfo.InvariantCulture)}")
                    : (DurableTaskEffectLookup.Unresolved, null);
            }

            if (state.DomainState.IsCreationAbandoned)
            {
                return (DurableTaskEffectLookup.Rejected, null);
            }

            return (DurableTaskEffectLookup.Absent, null);
        }

        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            if (workflow.FaultOptions.ConsumeFailCommitOnce())
            {
                throw new IOException("Simulated transient Project commit failure.");
            }

            // Re-verify winning AD-35 claim generation is still active before committing.
            (ProjectFolderClaimState claimState, _) = await workflow
                .RehydrateClaimStateAsync(meta.TenantId, folderReceipt.FolderId, cancellationToken)
                .ConfigureAwait(false);
            if (!claimState.IsActive
                || claimState.IsClosed
                || claimState.ActiveClaim is not { } active
                || !string.Equals(active.ProjectId, meta.ProjectId, StringComparison.Ordinal)
                || active.Generation != claimReceipt.Generation)
            {
                return (DurableTaskEffectLookup.Rejected, null);
            }

            (ProjectSdkState state, StoredEventStream stream) = await workflow
                .RehydrateProjectStateAsync(meta.TenantId, meta.ProjectId, cancellationToken)
                .ConfigureAwait(false);

            if (state.DomainState.IsCreated)
            {
                return string.Equals(state.DomainState.ProjectFolder?.FolderId, folderReceipt.FolderId, StringComparison.Ordinal)
                    && state.DomainState.FolderClaimGeneration == claimReceipt.Generation
                    ? (DurableTaskEffectLookup.Committed, $"project-created:{meta.ProjectId}:v{stream.Version.ToString(CultureInfo.InvariantCulture)}")
                    : (DurableTaskEffectLookup.Unresolved, null);
            }

            if (state.DomainState.IsCreationAbandoned)
            {
                return (DurableTaskEffectLookup.Rejected, null);
            }

            CreateBoundProject command = new(
                meta.CanonicalName,
                meta.CanonicalDescription,
                meta.CanonicalSetupMetadata,
                folderReceipt.FolderId,
                folderReceipt.FolderDisplayName,
                folderReceipt.ProvisioningReceiptId,
                claimReceipt.Generation,
                step.Key);

            CommandEnvelope envelope = BuildEnvelope(
                meta.TenantId,
                ProjectSdkAggregate.DomainName,
                meta.ProjectId,
                nameof(CreateBoundProject),
                command,
                meta.ActorPrincipalId,
                meta.CorrelationId,
                step.Task.TaskId,
                step.Key);

            DomainResult result = await ((IDomainProcessor)workflow._projectAggregate)
                .ProcessAsync(envelope, state)
                .ConfigureAwait(false);
            if (!result.IsSuccess || result.Events.Count == 0)
            {
                return (DurableTaskEffectLookup.Unresolved, null);
            }

            // Append with expectedVersion: stream.Version (never hardcoded 0, so prior rejection events on the stream never break CAS!).
            StoredEventStreamAppendResult append = await workflow._eventStore.TryAppendAsync(
                meta.TenantId,
                ProjectSdkAggregate.DomainName,
                meta.ProjectId,
                stream.Version,
                result.Events,
                meta.ActorPrincipalId,
                meta.CorrelationId,
                step.Key,
                cancellationToken).ConfigureAwait(false);

            if (!append.Succeeded)
            {
                throw new IOException("Project bound-creation CAS conflict; retry via authoritative lookup.");
            }

            if (workflow.FaultOptions.ConsumeDropNextCommitReplyOnce())
            {
                throw new TimeoutException("Simulated lost reply after durable ProjectCreated commit.");
            }

            return (DurableTaskEffectLookup.Committed, $"project-created:{meta.ProjectId}:v{append.Stream.Version.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private sealed class ConfirmReadModelStepEffect(
        FolderFirstProjectCreationWorkflow workflow,
        ProjectCreationTaskMetadata meta) : IDurableTaskEffect
    {
        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            ReadModelEntry<ProjectReadModel> detailEntry = await workflow._store
                .GetAsync<ProjectReadModel>(workflow._storeName, ProjectReadModelKeys.Detail(meta.TenantId, meta.ProjectId), cancellationToken)
                .ConfigureAwait(false);
            if (detailEntry.Value is { Freshness: EvidenceFreshnessState.Unavailable })
            {
                return (DurableTaskEffectLookup.Unresolved, null);
            }

            if (detailEntry.Value is { Freshness: EvidenceFreshnessState.Current, Detail: not null, IsCreationAbandoned: false } detail)
            {
                ReadModelEntry<ProjectReadIndex> indexEntry = await workflow._store
                    .GetAsync<ProjectReadIndex>(workflow._storeName, ProjectReadModelKeys.Index(meta.TenantId), cancellationToken)
                    .ConfigureAwait(false);
                if (indexEntry.Value is { } index
                    && string.Equals(index.TenantId, meta.TenantId, StringComparison.Ordinal)
                    && index.ProjectIds.Contains(meta.ProjectId, StringComparer.Ordinal)
                    && !index.UnavailableProjectIds.Contains(meta.ProjectId, StringComparer.Ordinal))
                {
                    return (DurableTaskEffectLookup.Committed, $"read-model-confirmed:{meta.ProjectId}:v{detail.Sequence.ToString(CultureInfo.InvariantCulture)}");
                }
            }

            return (DurableTaskEffectLookup.Absent, null);
        }

        public async Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(
            DurableTaskEffectStep step,
            CancellationToken cancellationToken)
        {
            bool projected = await workflow.ProjectToSupportedReadModelAsync(
                meta.TenantId,
                meta.ProjectId,
                $"{step.Task.TaskId}/project-reads",
                cancellationToken).ConfigureAwait(false);
            if (projected)
            {
                return (DurableTaskEffectLookup.Committed, $"read-model-confirmed:{meta.ProjectId}");
            }

            ReadModelEntry<ProjectReadModel> detailEntry = await workflow._store
                .GetAsync<ProjectReadModel>(workflow._storeName, ProjectReadModelKeys.Detail(meta.TenantId, meta.ProjectId), cancellationToken)
                .ConfigureAwait(false);
            if (detailEntry.Value is { Freshness: EvidenceFreshnessState.Unavailable })
            {
                return (DurableTaskEffectLookup.Unresolved, null);
            }

            throw new IOException("Read model confirmation unavailable.");
        }
    }

    private sealed class StaticCommittedEffect(string receipt) : IDurableTaskEffect
    {
        public Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(DurableTaskEffectStep step, CancellationToken cancellationToken)
            => Task.FromResult<(DurableTaskEffectLookup, string?)>((DurableTaskEffectLookup.Committed, receipt));

        public Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(DurableTaskEffectStep step, CancellationToken cancellationToken)
            => Task.FromResult<(DurableTaskEffectLookup, string?)>((DurableTaskEffectLookup.Committed, receipt));
    }

    private sealed class StaticOutcomeEffect(DurableTaskEffectLookup outcome) : IDurableTaskEffect
    {
        public Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(DurableTaskEffectStep step, CancellationToken cancellationToken)
            => Task.FromResult<(DurableTaskEffectLookup, string?)>((outcome, null));

        public Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(DurableTaskEffectStep step, CancellationToken cancellationToken)
            => Task.FromResult<(DurableTaskEffectLookup, string?)>((outcome, null));
    }

    private sealed class StaticThrowingEffect(string message) : IDurableTaskEffect
    {
        public Task<(DurableTaskEffectLookup Outcome, string? Receipt)> LookupAsync(DurableTaskEffectStep step, CancellationToken cancellationToken)
            => Task.FromException<(DurableTaskEffectLookup, string?)>(new IOException(message));

        public Task<(DurableTaskEffectLookup Outcome, string? Receipt)> ExecuteAsync(DurableTaskEffectStep step, CancellationToken cancellationToken)
            => Task.FromException<(DurableTaskEffectLookup, string?)>(new IOException(message));
    }
}
#endif
