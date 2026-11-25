// <copyright file="ProjectProjectionHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Text.Json;

using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Rebuilds the canonical Project aggregate state for EventStore's stateless full-replay projection callback.
/// </summary>
[EventStoreDomain(ProjectsServerModule.DomainName)]
internal sealed class ProjectProjectionHandler : IDomainProjectionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    ProjectionResponse IDomainProjectionHandler.Project(ProjectionRequest request) => Project(request);

    ProjectionResponse IDomainProjectionHandler.Project(ProjectionRequest request, CancellationToken cancellationToken)
        => Project(request, cancellationToken);

    /// <summary>Rebuilds a Project aggregate from the complete ordered event sequence.</summary>
    /// <param name="request">The authoritative aggregate identity and full event history.</param>
    /// <returns>The stable projection type and rebuilt aggregate state.</returns>
    public static ProjectionResponse Project(ProjectionRequest request) => Project(request, CancellationToken.None);

    private static ProjectionResponse Project(ProjectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Domain, ProjectsServerModule.DomainName, StringComparison.Ordinal))
        {
            throw new ArgumentException("The projection request targets an unsupported domain.", nameof(request));
        }

        ArgumentNullException.ThrowIfNull(request.Events);
        ProjectIdentity identity = new(request.TenantId, new ProjectId(request.AggregateId));
        ProjectState state = ProjectState.Empty;
        long expectedSequence = 1;

        foreach (ProjectionEventDto eventEnvelope in request.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (eventEnvelope.SequenceNumber != expectedSequence)
            {
                throw new InvalidOperationException("Project projection history is not a complete ordered sequence.");
            }

            if (!string.Equals(eventEnvelope.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Project projection history contains an unsupported event.");
            }

            state = ProjectsDomainProcessor.ApplyStoredEvent(state, eventEnvelope.EventTypeName, eventEnvelope.Payload, identity);
            expectedSequence++;
        }

        return new ProjectionResponse(
            ProjectsServerModule.ProjectionType,
            JsonSerializer.SerializeToElement(state, JsonOptions));
    }
}
