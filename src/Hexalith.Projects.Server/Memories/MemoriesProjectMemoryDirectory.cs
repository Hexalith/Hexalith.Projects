// <copyright file="MemoriesProjectMemoryDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Memories;

using System;
using System.Net;
using System.Net.Http;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects ACL adapter over the Hexalith.Memories generated metadata-only Case client (Story 2.7). It
/// validates a Memory Reference through the Memories <c>GetCaseAsync</c> route (the only stable read
/// route per the Story 2.6 ADR), which returns metadata-only case summary (id/tenant/name/optional
/// description/status/timestamps/memory-unit count) only after Memories runs tenant access and case
/// authorization. It never requests MemoryUnit content, embeddings, search results, or traversal
/// payloads (the content-bearing routes are deliberately not used) and translates the typed Case into a
/// safe Projects outcome, failing closed on any denial, archived, unavailable, or untrusted evidence.
/// </summary>
public sealed class MemoriesProjectMemoryDirectory(MemoriesClient memoriesClient) : IProjectMemoryDirectory
{
    private readonly MemoriesClient _memoriesClient = memoriesClient ?? throw new ArgumentNullException(nameof(memoriesClient));

    /// <inheritdoc />
    public Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string tenantId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
        => CheckMemoryEvidenceAsync(projectId, memoryReferenceId, tenantId, correlationId, cancellationToken);

    /// <inheritdoc />
    public Task<ProjectMemoryValidationResult> RefreshMemoryReferenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string tenantId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
        => CheckMemoryEvidenceAsync(projectId, memoryReferenceId, tenantId, correlationId, cancellationToken);

    private async Task<ProjectMemoryValidationResult> CheckMemoryEvidenceAsync(
        ProjectId projectId,
        string memoryReferenceId,
        string tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectId);

        if (string.IsNullOrWhiteSpace(memoryReferenceId)
            || string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            return new(ProjectMemoryValidationOutcome.ValidationFailed, correlationId);
        }

        try
        {
            Case caseResult = await _memoriesClient
                .GetCaseAsync(tenantId, memoryReferenceId, cancellationToken)
                .ConfigureAwait(false);

            return Evaluate(caseResult, tenantId, correlationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MemoriesRemoteException ex)
        {
            return new(MapMemoriesStatus(ex.StatusCode), correlationId);
        }
        catch (HttpRequestException)
        {
            return new(ProjectMemoryValidationOutcome.Unavailable, correlationId);
        }
        catch (Exception)
        {
            return new(ProjectMemoryValidationOutcome.Unavailable, correlationId);
        }
    }

    private static ProjectMemoryValidationResult Evaluate(Case? result, string tenantId, string correlationId)
    {
        // No evidence at all is untrusted: fail closed as unavailable (retryable) rather than accept.
        if (result is null)
        {
            return new(ProjectMemoryValidationOutcome.Unavailable, correlationId);
        }

        // The case must belong to the envelope tenant; a cross-tenant mismatch is never acceptable.
        if (!string.Equals(result.TenantId, tenantId, StringComparison.Ordinal))
        {
            return new(ProjectMemoryValidationOutcome.TenantMismatch, correlationId);
        }

        return result.Status switch
        {
            CaseStatus.Active => ProjectMemoryValidationResult.Accepted(correlationId),
            CaseStatus.Closed or CaseStatus.Deleting => new(ProjectMemoryValidationOutcome.Archived, correlationId),
            _ => new(ProjectMemoryValidationOutcome.Denied, correlationId),
        };
    }

    private static ProjectMemoryValidationOutcome MapMemoriesStatus(HttpStatusCode statusCode)
        => statusCode switch
        {
            HttpStatusCode.BadRequest
                or HttpStatusCode.UnprocessableEntity
                or HttpStatusCode.Conflict => ProjectMemoryValidationOutcome.ValidationFailed,
            HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound => ProjectMemoryValidationOutcome.Denied,
            HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable => ProjectMemoryValidationOutcome.Unavailable,
            >= (HttpStatusCode)500 and < (HttpStatusCode)600 => ProjectMemoryValidationOutcome.Unavailable,
            _ => ProjectMemoryValidationOutcome.Denied,
        };
}
