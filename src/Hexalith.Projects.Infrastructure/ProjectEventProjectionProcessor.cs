// <copyright file="ProjectEventProjectionProcessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

using Hexalith.EventStore.Contracts.Events;

using Microsoft.Extensions.Logging;

/// <summary>
/// Processes Project EventStore envelopes into durable read-side projection state.
/// </summary>
public sealed class ProjectEventProjectionProcessor(
    IProjectProjectionStore projectionStore,
    ILogger<ProjectEventProjectionProcessor>? logger = null)
{
    private readonly ILogger<ProjectEventProjectionProcessor>? _logger = logger;
    private readonly IProjectProjectionStore _projectionStore = projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));

    /// <summary>Processes one EventStore event envelope.</summary>
    /// <param name="envelope">The EventStore event envelope.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The metadata-only append result.</returns>
    public async Task<ProjectProjectionAppendResult> ProcessAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ProjectProjectionAppendResult result = await _projectionStore
            .AppendAsync(envelope, cancellationToken)
            .ConfigureAwait(false);

        if (result.Status is ProjectProjectionAppendStatus.ReplayConflict or ProjectProjectionAppendStatus.InvalidPayload)
        {
            _logger?.LogWarning(
                "Project projection event processing failed closed for tenant {TenantId}, message {MessageId}, sequence {Sequence}, status {Status}.",
                result.TenantId,
                result.MessageId,
                result.Sequence,
                result.Status);
        }
        else
        {
            _logger?.LogDebug(
                "Project projection event processing completed for tenant {TenantId}, message {MessageId}, sequence {Sequence}, status {Status}.",
                result.TenantId,
                result.MessageId,
                result.Sequence,
                result.Status);
        }

        return result;
    }
}
