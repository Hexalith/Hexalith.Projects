// <copyright file="ProposeNewProjectEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Proposals;

using Microsoft.AspNetCore.Http;

/// <summary>Story 4.5 endpoints for NoMatch new-Project proposal preview and confirmation.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> ProposeNewProjectAsync(
        HttpContext httpContext,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationResolutionDirectory conversationResolutionDirectory,
        IProjectListReadModel listReadModel,
        IProjectReferenceIndexReadModel referenceIndexReadModel,
        ProjectResolutionEngine resolutionEngine,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string? correlationId = ReadHeader(httpContext, "X-Correlation-Id");
        correlationId = IsCanonicalIdentifier(correlationId) ? correlationId : null;
        string? taskId = ReadHeader(httpContext, "X-Hexalith-Task-Id");
        taskId = IsCanonicalIdentifier(taskId) ? taskId : null;

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeListAsync(tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        if (HasHeader(httpContext, "Idempotency-Key"))
        {
            return ValidationProblem(correlationId, taskId, "idempotency_key");
        }

        string? requestedFreshness = ReadHeader(httpContext, FreshnessHeaderName);
        if (requestedFreshness is not null && !string.Equals(requestedFreshness, EventuallyConsistent, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "freshness");
        }

        ProjectCreationProposalHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<ProjectCreationProposalHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (!TryValidateProposalPreviewRequest(body, out string? rejectedField, out IReadOnlyList<string> fileReferenceIds))
        {
            return ValidationProblem(correlationId, taskId, rejectedField ?? "body");
        }

        string authoritativeTenantId = tenantContext.AuthoritativeTenantId!;
        string principalId = tenantContext.PrincipalId!;
        string safeCorrelationId = correlationId ?? string.Empty;

        ProjectResolution resolution;
        try
        {
            resolution = await ResolveCombinedEvidenceAsync(
                body!.ConversationId!,
                body.FolderId,
                fileReferenceIds,
                authoritativeTenantId,
                principalId,
                safeCorrelationId,
                correlationId,
                taskId,
                conversationResolutionDirectory,
                listReadModel,
                referenceIndexReadModel,
                resolutionEngine,
                timeProvider,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, taskId);
        }

        if (resolution.Result != ResolutionResult.NoMatch)
        {
            return ValidationProblem(correlationId, taskId, "resolutionResult");
        }

        ConversationResolutionMetadata conversation;
        try
        {
            conversation = await conversationResolutionDirectory
                .ReadConversationMetadataAsync(
                    new ConversationId(body.ConversationId!),
                    new ConversationTenantId(authoritativeTenantId),
                    new CallerPrincipalId(principalId),
                    safeCorrelationId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, taskId);
        }

        IResult? conversationProblem = ConversationPreflightProblem(conversation.ReferenceState, correlationId, taskId);
        if (conversationProblem is not null)
        {
            return conversationProblem;
        }

        ProjectCreationProposal? proposal = ProjectCreationProposalBuilder.TryBuild(
            resolution.Result,
            body.ConversationId!,
            body.SuggestedName,
            conversation.SafeLabel,

            // The attachment-label tier is intentionally unused here: the read-style preview carries
            // only ids, and a NoMatch reference-index lookup yields no included candidate rows from
            // which a safe folder/file display label could be read. Caller suggestion, conversation
            // label, then the deterministic fallback cover the reachable cases. The builder keeps the
            // attachment-label parameter for callers (e.g. confirm-side flows) that can supply one.
            attachmentLabel: null,
            body.Description,
            body.SetupMetadata,
            body.FolderId,
            fileReferenceIds,
            resolution.ObservedAt,
            EventuallyConsistent);

        if (proposal is null)
        {
            return ValidationProblem(correlationId, taskId, "metadata");
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        }

        httpContext.Response.Headers[FreshnessHeaderName] = EventuallyConsistent;
        return Results.Json(proposal, ResponseJsonOptions);
    }

    private static async Task<IResult> ConfirmNewProjectProposalAsync(
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationResolutionDirectory conversationResolutionDirectory,
        IProjectListReadModel listReadModel,
        IProjectReferenceIndexReadModel referenceIndexReadModel,
        IProjectConversationAssignmentDirectory assignmentDirectory,
        IProjectFolderDirectory folderDirectory,
        IProjectFileReferenceDirectory fileReferenceDirectory,
        ProjectResolutionEngine resolutionEngine,
        IProjectProposalConfirmationIdempotencyLedger idempotencyLedger,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        ConfirmNewProjectProposalHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<ConfirmNewProjectProposalHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (!TryValidateConfirmRequest(body, out string? rejectedField, out IReadOnlyList<ConfirmProposalFileReferenceHttpRequest> fileReferences))
        {
            return ValidationProblem(correlationId, taskId, rejectedField ?? "confirmation");
        }

        ProjectAuthorizationResult authorization = await authorizationGate
            .AuthorizeCreateAsync(tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!authorization.IsAllowed)
        {
            return authorization.Retryable && authorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, authorization);
        }

        string authoritativeTenantId = tenantContext.AuthoritativeTenantId!;
        string principalId = tenantContext.PrincipalId!;
        ProjectId projectId = new(body!.ProjectId!);

        // Preflight the conversation, folder, and file ACLs before any write. These boundaries are
        // designed to return safe result outcomes, but an unexpected throw from a degraded read model
        // must still fail closed as a retryable 503 (AC6) rather than surface a 500 - mirroring the
        // preview endpoint and the resolution re-check below.
        try
        {
            ConversationResolutionMetadata conversation = await conversationResolutionDirectory
                .ReadConversationMetadataAsync(
                    new ConversationId(body.ConversationId!),
                    new ConversationTenantId(authoritativeTenantId),
                    new CallerPrincipalId(principalId),
                    correlationId!,
                    cancellationToken)
                .ConfigureAwait(false);

            IResult? conversationProblem = ConversationPreflightProblem(conversation.ReferenceState, correlationId, taskId);
            if (conversationProblem is not null)
            {
                return conversationProblem;
            }

            if (body.Folder is not null)
            {
                ProjectFolderValidationResult folderValidation = await folderDirectory
                    .ValidateSetProjectFolderAsync(projectId, body.Folder.FolderId!, correlationId!, cancellationToken)
                    .ConfigureAwait(false);
                IResult? folderProblem = FolderValidationProblem(folderValidation, correlationId, taskId);
                if (folderProblem is not null)
                {
                    return folderProblem;
                }
            }

            foreach (ConfirmProposalFileReferenceHttpRequest file in fileReferences)
            {
                ProjectFileReferenceValidationResult fileValidation = await fileReferenceDirectory
                    .ValidateLinkFileReferenceAsync(
                        projectId,
                        file.FolderId!,
                        file.WorkspaceId!,
                        file.FilePath!,
                        correlationId!,
                        taskId!,
                        cancellationToken)
                    .ConfigureAwait(false);
                IResult? fileProblem = FileReferenceValidationProblem(fileValidation, correlationId, taskId);
                if (fileProblem is not null)
                {
                    return fileProblem;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, taskId);
        }

        ProjectResolution resolution;
        try
        {
            resolution = await ResolveCombinedEvidenceAsync(
                body.ConversationId!,
                body.Folder?.FolderId,
                fileReferences.Select(static file => file.FileReferenceId!).ToArray(),
                authoritativeTenantId,
                principalId,
                correlationId!,
                correlationId,
                taskId,
                conversationResolutionDirectory,
                listReadModel,
                referenceIndexReadModel,
                resolutionEngine,
                timeProvider,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReadModelUnavailable(correlationId, taskId);
        }

        if (resolution.Result != ResolutionResult.NoMatch)
        {
            return ValidationProblem(correlationId, taskId, "resolutionResult");
        }

        // Record the root idempotency fingerprint only after authorization and every ACL preflight
        // succeed, so an unauthorized or denied attempt never poisons the key (AC6). Same root key +
        // different body returns 409 here, before the first CreateProject write (AC7).
        string rootFingerprint = ComputeConfirmProposalFingerprint(body!);
        if (!idempotencyLedger.TryRecord(idempotencyKey!, rootFingerprint))
        {
            return IdempotencyConflict(correlationId, taskId);
        }

        CreateProject create = new(
            authoritativeTenantId,
            projectId,
            body.ProjectMetadata!.DisplayName!,
            body.Description,
            body.SetupMetadata,
            principalId,
            correlationId!,
            taskId!,
            DeriveChildIdempotencyKey(idempotencyKey!, "create"));
        ProjectCommandValidationResult createValidation = ProjectCommandValidator.Validate(create);
        if (!createValidation.IsAccepted)
        {
            return ValidationProblem(correlationId, taskId, createValidation.RejectedField ?? "command");
        }

        ProjectCommandSubmissionResult createResult = await submitter
            .SubmitCreateProjectAsync(create, cancellationToken)
            .ConfigureAwait(false);
        if (createResult.Outcome is not (ProjectCommandSubmissionOutcome.Accepted or ProjectCommandSubmissionOutcome.IdempotentReplay))
        {
            return MutationResult(httpContext, timeProvider, createResult, correlationId!, taskId!);
        }

        ProjectConversationAssignmentResult assignment = await assignmentDirectory
            .ConfirmResolutionAssignmentAsync(
                projectId,
                new ConversationId(body.ConversationId!),
                expectedSourceProjectId: null,
                new ConversationTenantId(authoritativeTenantId),
                new CallerPrincipalId(principalId),
                new ProjectConversationCommandMetadata(correlationId!, taskId!, DeriveChildIdempotencyKey(idempotencyKey!, "conversation")),
                cancellationToken)
            .ConfigureAwait(false);
        if (assignment.Outcome != ProjectConversationAssignmentOutcome.Accepted)
        {
            return AssignmentResult(httpContext, timeProvider, assignment, correlationId!, taskId!);
        }

        if (body.Folder is not null)
        {
            SetProjectFolder folder = new(
                authoritativeTenantId,
                projectId,
                body.Folder.FolderId!,
                body.Folder.FolderMetadata!,
                ReplacementConfirmed: false,
                principalId,
                correlationId!,
                taskId!,
                DeriveChildIdempotencyKey(idempotencyKey!, "folder"));
            ProjectCommandValidationResult folderValidation = ProjectCommandValidator.Validate(folder);
            if (!folderValidation.IsAccepted)
            {
                return ValidationProblem(correlationId, taskId, folderValidation.RejectedField ?? "command");
            }

            ProjectCommandSubmissionResult folderResult = await submitter
                .SubmitSetProjectFolderAsync(folder, cancellationToken)
                .ConfigureAwait(false);
            if (folderResult.Outcome is not (ProjectCommandSubmissionOutcome.Accepted or ProjectCommandSubmissionOutcome.IdempotentReplay))
            {
                return MutationResult(httpContext, timeProvider, folderResult, correlationId!, taskId!);
            }
        }

        foreach (ConfirmProposalFileReferenceHttpRequest file in fileReferences)
        {
            LinkFileReference link = new(
                authoritativeTenantId,
                projectId,
                file.FileReferenceId!,
                file.FolderId!,
                file.FileMetadata!,
                principalId,
                correlationId!,
                taskId!,
                DeriveChildIdempotencyKey(idempotencyKey!, "file:" + file.FileReferenceId!));
            ProjectCommandValidationResult linkValidation = ProjectCommandValidator.Validate(link);
            if (!linkValidation.IsAccepted)
            {
                return ValidationProblem(correlationId, taskId, linkValidation.RejectedField ?? "command");
            }

            ProjectCommandSubmissionResult linkResult = await submitter
                .SubmitLinkFileReferenceAsync(link, cancellationToken)
                .ConfigureAwait(false);
            if (linkResult.Outcome is not (ProjectCommandSubmissionOutcome.Accepted or ProjectCommandSubmissionOutcome.IdempotentReplay))
            {
                return MutationResult(httpContext, timeProvider, linkResult, correlationId!, taskId!);
            }
        }

        return Accepted(httpContext, timeProvider, correlationId!, taskId!, idempotentReplay: false);
    }

    private static async Task<ProjectResolution> ResolveCombinedEvidenceAsync(
        string conversationId,
        string? folderId,
        IReadOnlyList<string> fileReferenceIds,
        string authoritativeTenantId,
        string principalId,
        string safeCorrelationId,
        string? correlationId,
        string? taskId,
        IProjectConversationResolutionDirectory conversationResolutionDirectory,
        IProjectListReadModel listReadModel,
        IProjectReferenceIndexReadModel referenceIndexReadModel,
        ProjectResolutionEngine resolutionEngine,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ConversationResolutionMetadata conversationMetadata = await conversationResolutionDirectory
            .ReadConversationMetadataAsync(
                new ConversationId(conversationId),
                new ConversationTenantId(authoritativeTenantId),
                new CallerPrincipalId(principalId),
                safeCorrelationId,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProjectListItem> projectedRows = await listReadModel
            .ListAsync(authoritativeTenantId, lifecycleFilter: null, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<ProjectListItem> rows = ProjectQueryTenantFilter.FilterList(authoritativeTenantId, projectedRows);
        ConversationResolutionProjectCandidate[] conversationCandidates = rows
            .Select(static row => new ConversationResolutionProjectCandidate(row.ProjectId, row.Name, row.Lifecycle))
            .ToArray();

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<ProjectResolutionCandidateEvidence> evidence =
        [
            .. ConversationResolutionEvidenceMapper.Map(conversationMetadata, conversationCandidates, now),
        ];

        string[] folderIds = string.IsNullOrWhiteSpace(folderId) ? [] : [folderId!];
        if (folderIds.Length > 0 || fileReferenceIds.Count > 0)
        {
            IReadOnlyList<ProjectReferenceIndexCandidateRow> indexedRows = await referenceIndexReadModel
                .ListByReferenceAsync(authoritativeTenantId, folderIds, fileReferenceIds, cancellationToken)
                .ConfigureAwait(false);

            List<AttachmentResolutionProjectCandidate> attachmentCandidates = new(indexedRows.Count);
            foreach (ProjectReferenceIndexCandidateRow row in indexedRows)
            {
                if (!string.Equals(row.TenantId, authoritativeTenantId, StringComparison.Ordinal))
                {
                    continue;
                }

                List<AttachmentResolutionReference> folders = [];
                List<AttachmentResolutionReference> files = [];
                foreach (ProjectReferenceIndexItem reference in row.MatchedReferences)
                {
                    string? referenceId = reference.ReferenceId;
                    if (string.IsNullOrWhiteSpace(referenceId))
                    {
                        continue;
                    }

                    if (string.Equals(reference.ReferenceKind, AttachmentResolutionEvidenceMapper.FolderReferenceKind, StringComparison.Ordinal))
                    {
                        folders.Add(new AttachmentResolutionReference(reference.ReferenceKind, referenceId, reference.ReferenceState));
                    }
                    else if (string.Equals(reference.ReferenceKind, AttachmentResolutionEvidenceMapper.FileReferenceKind, StringComparison.Ordinal))
                    {
                        files.Add(new AttachmentResolutionReference(reference.ReferenceKind, referenceId, reference.ReferenceState));
                    }
                }

                attachmentCandidates.Add(new AttachmentResolutionProjectCandidate(row.ProjectId, row.DisplayName, row.Lifecycle, folders, files));
            }

            AttachmentResolutionMetadata attachments = new(
                folderIds.Select(static id => new AttachmentResolutionReference(AttachmentResolutionEvidenceMapper.FolderReferenceKind, id, ReferenceState.Included)).ToArray(),
                fileReferenceIds.Select(static id => new AttachmentResolutionReference(AttachmentResolutionEvidenceMapper.FileReferenceKind, id, ReferenceState.Included)).ToArray());
            evidence.AddRange(AttachmentResolutionEvidenceMapper.Map(attachments, attachmentCandidates, now));
        }

        ProjectResolutionContext context = new(
            AuthoritativeTenantId: authoritativeTenantId,
            RequestedTenantId: authoritativeTenantId,
            IncludeArchived: false,
            Now: now,
            CorrelationId: correlationId,
            TaskId: taskId,
            PresentedInputIds: [conversationId, .. folderIds, .. fileReferenceIds]);

        return resolutionEngine.Resolve(context, MergeEvidence(evidence));
    }

    private static IReadOnlyList<ProjectResolutionCandidateEvidence> MergeEvidence(IReadOnlyList<ProjectResolutionCandidateEvidence> evidence)
        => evidence
            .GroupBy(static item => item.ProjectId, StringComparer.Ordinal)
            .Select(static group =>
            {
                ProjectResolutionCandidateEvidence first = group.First();
                return new ProjectResolutionCandidateEvidence(
                    first.ProjectId,
                    first.DisplayName,
                    first.Lifecycle,
                    group.SelectMany(static item => item.Signals)
                        .OrderBy(static signal => signal.ReferenceKind, StringComparer.Ordinal)
                        .ThenBy(static signal => signal.ReferenceId, StringComparer.Ordinal)
                        .ThenBy(static signal => signal.ReasonCode)
                        .ToArray());
            })
            .OrderBy(static item => item.ProjectId, StringComparer.Ordinal)
            .ToArray();

    private static bool TryValidateProposalPreviewRequest(
        ProjectCreationProposalHttpRequest? body,
        out string? rejectedField,
        out IReadOnlyList<string> fileReferenceIds)
    {
        rejectedField = null;
        fileReferenceIds = [];
        if (body is null || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal))
        {
            rejectedField = "requestSchemaVersion";
            return false;
        }

        if (!IsCanonicalIdentifier(body.ConversationId))
        {
            rejectedField = "conversationId";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(body.FolderId) && !IsCanonicalIdentifier(body.FolderId))
        {
            rejectedField = "folderId";
            return false;
        }

        if (!TryNormalizeFileReferenceIds(body.FileReferenceIds, out fileReferenceIds))
        {
            rejectedField = "fileReferenceIds";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(body.SuggestedName)
            && !ProjectCreationProposalBuilder.IsSafeCreateMetadata(body.SuggestedName, null, null))
        {
            rejectedField = "suggestedName";
            return false;
        }

        if (!ProjectCreationProposalBuilder.IsSafeCreateMetadata(
            string.IsNullOrWhiteSpace(body.SuggestedName) ? "New project" : body.SuggestedName!,
            body.Description,
            body.SetupMetadata))
        {
            rejectedField = "metadata";
            return false;
        }

        return true;
    }

    private static bool TryValidateConfirmRequest(
        ConfirmNewProjectProposalHttpRequest? body,
        out string? rejectedField,
        out IReadOnlyList<ConfirmProposalFileReferenceHttpRequest> fileReferences)
    {
        rejectedField = null;
        fileReferences = [];
        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "confirmNewProjectProposal", StringComparison.Ordinal)
            || body.Confirmed != true
            || !string.Equals(body.ResolutionResult, "NoMatch", StringComparison.Ordinal))
        {
            rejectedField = "confirmation";
            return false;
        }

        if (!IsCanonicalIdentifier(body.ProjectId) || !IsCanonicalIdentifier(body.ConversationId))
        {
            rejectedField = "identity";
            return false;
        }

        if (body.ProjectMetadata is null
            || string.IsNullOrWhiteSpace(body.ProjectMetadata.DisplayName)
            || !SensitiveMetadataTierValidator.IsValid(body.ProjectMetadata.MetadataClass)
            || !ProjectCreationProposalBuilder.IsSafeCreateMetadata(body.ProjectMetadata.DisplayName!, body.Description, body.SetupMetadata))
        {
            rejectedField = "projectMetadata";
            return false;
        }

        if (body.Folder is not null
            && (!IsCanonicalIdentifier(body.Folder.FolderId)
                || body.Folder.FolderMetadata is null
                || !ProjectCommandValidator.Validate(new SetProjectFolder(
                    "tenant-proposal-validation",
                    new ProjectId(body.ProjectId!),
                    body.Folder.FolderId!,
                    body.Folder.FolderMetadata,
                    ReplacementConfirmed: false,
                    "principal-proposal-validation",
                    "correlation-proposal-validation",
                    "task-proposal-validation",
                    "idempotency-proposal-validation")).IsAccepted))
        {
            rejectedField = "folder";
            return false;
        }

        ConfirmProposalFileReferenceHttpRequest[] orderedFiles = (body.FileReferences ?? Array.Empty<ConfirmProposalFileReferenceHttpRequest>())
            .OrderBy(static file => file.FileReferenceId, StringComparer.Ordinal)
            .ToArray();
        if (orderedFiles.Length > MaxAttachmentReferenceCount)
        {
            rejectedField = "fileReferences";
            return false;
        }

        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (ConfirmProposalFileReferenceHttpRequest file in orderedFiles)
        {
            if (!IsCanonicalIdentifier(file.FileReferenceId)
                || !unique.Add(file.FileReferenceId!)
                || !IsCanonicalIdentifier(file.FolderId)
                || !IsCanonicalIdentifier(file.WorkspaceId)
                || !IsWorkspaceRelativePath(file.FilePath)
                || file.FileMetadata is null
                || !ProjectCommandValidator.Validate(new LinkFileReference(
                    "tenant-proposal-validation",
                    new ProjectId(body.ProjectId!),
                    file.FileReferenceId!,
                    file.FolderId!,
                    file.FileMetadata,
                    "principal-proposal-validation",
                    "correlation-proposal-validation",
                    "task-proposal-validation",
                    "idempotency-proposal-validation")).IsAccepted)
            {
                rejectedField = "fileReferences";
                return false;
            }
        }

        if (!TryNormalizeFileReferenceIds(body.FileReferenceIds, out IReadOnlyList<string> declaredFileIds)
            || !declaredFileIds.SequenceEqual(orderedFiles.Select(static file => file.FileReferenceId!), StringComparer.Ordinal))
        {
            rejectedField = "fileReferenceIds";
            return false;
        }

        fileReferences = orderedFiles;
        return true;
    }

    private static bool TryNormalizeFileReferenceIds(IReadOnlyList<string>? input, out IReadOnlyList<string> ids)
    {
        ids = [];
        if (input is null || input.Count == 0)
        {
            return true;
        }

        if (input.Count > MaxAttachmentReferenceCount)
        {
            return false;
        }

        SortedSet<string> unique = new(StringComparer.Ordinal);
        foreach (string? id in input)
        {
            if (!IsCanonicalIdentifier(id) || !unique.Add(id!))
            {
                return false;
            }
        }

        ids = unique.ToArray();
        return true;
    }

    private static IResult? ConversationPreflightProblem(ReferenceState state, string? correlationId, string? taskId)
        => state switch
        {
            ReferenceState.Included => null,
            ReferenceState.Stale or ReferenceState.Unavailable => ReadModelUnavailable(correlationId, taskId),
            _ => SafeDenial(correlationId, taskId),
        };

    private static string DeriveChildIdempotencyKey(string root, string child)
        => root + ":" + child;

    private static string ComputeConfirmProposalFingerprint(ConfirmNewProjectProposalHttpRequest body)
    {
        string[] fileIds = (body.FileReferences ?? Array.Empty<ConfirmProposalFileReferenceHttpRequest>())
            .Select(static file => file.FileReferenceId!)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        string? folderId = body.Folder?.FolderId;
        string? description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim();
        string? setupMetadata = string.IsNullOrWhiteSpace(body.SetupMetadata) ? null : body.SetupMetadata.Trim();

        string[] lines =
        [
            "operation=ConfirmNewProjectProposal",
            "field=conversation_id;present=true;value=s:" + EscapeFingerprint(body.ConversationId!),
            "field=description;present=true;value=" + (description is null ? "null" : "s:" + EscapeFingerprint(description)),
            "field=file_reference_ids;present=true;value=j:[" + string.Join(",", fileIds.Select(static id => "\"" + EscapeJson(id) + "\"")) + "]",
            "field=folder.folder_id;present=" + (string.IsNullOrWhiteSpace(folderId) ? "false;value=omitted" : "true;value=s:" + EscapeFingerprint(folderId!)),
            "field=operation;present=true;value=s:confirmNewProjectProposal",
            "field=project_id;present=true;value=s:" + EscapeFingerprint(body.ProjectId!),
            "field=project_metadata.display_name;present=true;value=s:" + EscapeFingerprint(body.ProjectMetadata!.DisplayName!.Trim()),
            "field=request_schema_version;present=true;value=s:v1",
            "field=resolution_result;present=true;value=s:NoMatch",
            "field=setup_metadata;present=true;value=" + (setupMetadata is null ? "null" : "s:" + EscapeFingerprint(setupMetadata)),
        ];

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static string EscapeFingerprint(string value)
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
                ';' => "\\;",
                '=' => "\\=",
                _ when char.IsControl(c) => "\\u" + ((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture),
                _ => c.ToString(),
            });
        }

        return builder.ToString();
    }

    private static string EscapeJson(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private sealed record ProjectCreationProposalHttpRequest(
        string? RequestSchemaVersion,
        string? ConversationId,
        string? FolderId,
        IReadOnlyList<string>? FileReferenceIds,
        string? SuggestedName,
        string? Description,
        string? SetupMetadata);

    private sealed record ConfirmNewProjectProposalHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ResolutionResult,
        bool? Confirmed,
        string? ProjectId,
        string? ConversationId,
        ProjectMetadataHttpRequest? ProjectMetadata,
        string? Description,
        string? SetupMetadata,
        ConfirmProposalFolderHttpRequest? Folder,
        IReadOnlyList<ConfirmProposalFileReferenceHttpRequest>? FileReferences,
        IReadOnlyList<string>? FileReferenceIds);

    private sealed record ConfirmProposalFolderHttpRequest(
        string? FolderId,
        ProjectFolderMetadata? FolderMetadata);

    private sealed record ConfirmProposalFileReferenceHttpRequest(
        string? FileReferenceId,
        string? FolderId,
        string? WorkspaceId,
        string? FilePath,
        ProjectFileReferenceMetadata? FileMetadata);
}
