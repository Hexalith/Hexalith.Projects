// <copyright file="ProjectProjectionHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Rebuilds the canonical Project aggregate state for EventStore's stateless full-replay projection callback.
/// </summary>
internal static class ProjectProjectionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, Type> ProjectEventTypes = typeof(IProjectEvent).Assembly
        .GetTypes()
        .Where(static type => !type.IsAbstract && !type.IsInterface && typeof(IProjectEvent).IsAssignableFrom(type))
        .ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

    /// <summary>Rebuilds a Project aggregate from the complete ordered event sequence.</summary>
    /// <param name="request">The authoritative aggregate identity and full event history.</param>
    /// <returns>The stable projection type and rebuilt aggregate state.</returns>
    public static ProjectionResponse Project(ProjectionRequest request)
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
            if (eventEnvelope.SequenceNumber != expectedSequence)
            {
                throw new InvalidOperationException("Project projection history is not a complete ordered sequence.");
            }

            if (!string.Equals(eventEnvelope.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase)
                || !ProjectEventTypes.TryGetValue(eventEnvelope.EventTypeName, out Type? eventType))
            {
                throw new InvalidOperationException("Project projection history contains an unsupported event.");
            }

            IProjectEvent projectEvent = JsonSerializer.Deserialize(eventEnvelope.Payload, eventType, JsonOptions) as IProjectEvent
                ?? throw new InvalidOperationException("Project projection history contains an invalid event payload.");
            state = ProjectStateApply.Apply(state, projectEvent, identity);
            expectedSequence++;
        }

        return new ProjectionResponse(
            ProjectsServerModule.ProjectionType,
            JsonSerializer.SerializeToElement(state, JsonOptions));
    }
}
