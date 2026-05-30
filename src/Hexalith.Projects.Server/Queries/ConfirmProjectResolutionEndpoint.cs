// <copyright file="ConfirmProjectResolutionEndpoint.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System.Text.Json;

using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Conversations;

using Microsoft.AspNetCore.Http;

/// <summary>Confirm ambiguous Project resolution mutation endpoint.</summary>
public static partial class ProjectsDomainServiceEndpoints
{
    private static async Task<IResult> ConfirmProjectResolutionAsync(
        string projectId,
        string conversationId,
        HttpContext httpContext,
        IProjectCommandSubmitter submitter,
        IProjectTenantContextAccessor tenantContext,
        ProjectAuthorizationGate authorizationGate,
        IProjectConversationAssignmentDirectory assignmentDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryReadMutationEnvelope(httpContext, out string? correlationId, out string? taskId, out string? idempotencyKey, out IResult? envelopeRejection))
        {
            return envelopeRejection!;
        }

        if (!IsCanonicalIdentifier(projectId) || !IsCanonicalIdentifier(conversationId))
        {
            return SafeDenial(correlationId, taskId);
        }

        ConfirmProjectResolutionHttpRequest? body;
        try
        {
            body = await httpContext.Request
                .ReadFromJsonAsync<ConfirmProjectResolutionHttpRequest>(RequestJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return ValidationProblem(correlationId, taskId, "body");
        }

        if (body is null
            || !string.Equals(body.RequestSchemaVersion, "v1", StringComparison.Ordinal)
            || !string.Equals(body.Operation, "confirm", StringComparison.Ordinal)
            || body.Confirmed != true
            || !string.Equals(body.ResolutionResult, "MultipleCandidates", StringComparison.Ordinal)
            || !string.Equals(body.ProjectId, projectId, StringComparison.Ordinal)
            || !string.Equals(body.ConversationId, conversationId, StringComparison.Ordinal)
            || !IsValidCandidateEvidence(body.CandidateProjectIds, projectId)
            || (!string.IsNullOrWhiteSpace(body.SourceProjectId) && !IsCanonicalIdentifier(body.SourceProjectId))
            || string.Equals(body.SourceProjectId, projectId, StringComparison.Ordinal))
        {
            return ValidationProblem(correlationId, taskId, "confirmation");
        }

        ProjectAuthorizationResult targetAuthorization = await authorizationGate
            .AuthorizeConfirmProjectResolutionAsync(projectId, tenantContext, httpContext, correlationId, taskId, cancellationToken)
            .ConfigureAwait(false);
        if (!targetAuthorization.IsAllowed || !IsActive(targetAuthorization.ProjectDetail))
        {
            return targetAuthorization.Retryable && targetAuthorization.Reason == ReferenceState.Unavailable
                ? ReadModelUnavailable(correlationId, taskId)
                : SafeDenial(correlationId, taskId, targetAuthorization);
        }

        ProjectId? sourceProjectId = null;
        if (!string.IsNullOrWhiteSpace(body.SourceProjectId))
        {
            ProjectAuthorizationResult sourceAuthorization = await authorizationGate
                .AuthorizeConfirmProjectResolutionAsync(body.SourceProjectId!, tenantContext, httpContext, correlationId, taskId, cancellationToken)
                .ConfigureAwait(false);
            if (!sourceAuthorization.IsAllowed || !IsActive(sourceAuthorization.ProjectDetail))
            {
                return sourceAuthorization.Retryable && sourceAuthorization.Reason == ReferenceState.Unavailable
                    ? ReadModelUnavailable(correlationId, taskId)
                    : SafeDenial(correlationId, taskId, sourceAuthorization);
            }

            sourceProjectId = new ProjectId(body.SourceProjectId!);
        }

        ProjectConversationAssignmentResult assignment = await assignmentDirectory
            .ConfirmResolutionAssignmentAsync(
                new ProjectId(projectId),
                new ConversationId(conversationId),
                sourceProjectId,
                new ConversationTenantId(tenantContext.AuthoritativeTenantId!),
                new CallerPrincipalId(tenantContext.PrincipalId!),
                new ProjectConversationCommandMetadata(correlationId!, taskId!, idempotencyKey!),
                cancellationToken)
            .ConfigureAwait(false);

        if (assignment.Outcome != ProjectConversationAssignmentOutcome.Accepted)
        {
            return AssignmentResult(httpContext, timeProvider, assignment, correlationId!, taskId!);
        }

        ConfirmProjectResolution command = new(
            tenantContext.AuthoritativeTenantId!,
            new ProjectId(projectId),
            conversationId,
            sourceProjectId,
            tenantContext.PrincipalId!,
            correlationId!,
            taskId!,
            idempotencyKey!);

        ProjectCommandSubmissionResult result = await submitter
            .SubmitConfirmProjectResolutionAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return MutationResult(httpContext, timeProvider, result, correlationId!, taskId!);
    }

    private static bool IsValidCandidateEvidence(IReadOnlyList<string>? candidateProjectIds, string routeProjectId)
    {
        if (candidateProjectIds is null || candidateProjectIds.Count < 2)
        {
            return false;
        }

        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (string? candidate in candidateProjectIds)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !IsCanonicalIdentifier(candidate) || !unique.Add(candidate))
            {
                return false;
            }
        }

        return unique.Count >= 2 && unique.Contains(routeProjectId);
    }

    private sealed record ConfirmProjectResolutionHttpRequest(
        string? RequestSchemaVersion,
        string? Operation,
        string? ProjectId,
        string? ConversationId,
        string? ResolutionResult,
        bool? Confirmed,
        IReadOnlyList<string>? CandidateProjectIds,
        string? SourceProjectId);
}
