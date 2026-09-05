// <copyright file="ConversationStartSetupProjectionHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Projections.ConversationStartSetup;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>Projects the bounded Conversation-start source from the durable Project event stream.</summary>
public sealed class ConversationStartSetupProjectionHandler(IReadModelStore readModelStore) : IAsyncDomainProjectionHandler
{
    private const string StoreName = "projects-conversation-start-setup";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, Type> EventTypes = typeof(IProjectEvent).Assembly
        .GetTypes()
        .Where(static type => !type.IsAbstract && !type.IsInterface && typeof(IProjectEvent).IsAssignableFrom(type))
        .ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

    private readonly IReadModelStore _readModelStore = readModelStore ?? throw new ArgumentNullException(nameof(readModelStore));

    /// <inheritdoc/>
    public string Domain => "projects";

    /// <inheritdoc/>
    public string ProjectionType => "conversation-start-setup";

    /// <inheritdoc/>
    public async Task<DomainProjectionHandlerResult> ProjectAsync(
        ProjectionRequest request,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);
        if (!string.Equals(request.Domain, Domain, StringComparison.Ordinal)
            || request.Events is null)
        {
            return DomainProjectionHandlerResult.Failed("invalid-request");
        }

        List<ProjectProjectionEnvelope> envelopes = new(request.Events.Length);
        foreach (ProjectionEventDto eventDto in request.Events)
        {
            if (!string.Equals(eventDto.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase)
                || !EventTypes.TryGetValue(eventDto.EventTypeName, out Type? eventType))
            {
                return DomainProjectionHandlerResult.Failed("unsupported-event");
            }

            IProjectEvent projectEvent = JsonSerializer.Deserialize(eventDto.Payload, eventType, JsonOptions) as IProjectEvent
                ?? throw new InvalidOperationException("Projection event payload is invalid.");
            envelopes.Add(new ProjectProjectionEnvelope(request.TenantId, eventDto.SequenceNumber, projectEvent));
        }

        ProjectDetailItem? detail = ProjectDetailProjection.Rebuild(envelopes).Get(request.TenantId, request.AggregateId);
        if (detail is null)
        {
            return DomainProjectionHandlerResult.Completed();
        }

        await ReadModelWritePolicy.UpdateAsync<ProjectDetailItem>(
            _readModelStore,
            StoreName,
            $"{request.TenantId}:projects:{request.AggregateId}",
            current => current is not null && current.Sequence >= detail.Sequence ? current : detail,
            new ReadModelWriteContext("projection", ProjectionType, dispatchId),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return DomainProjectionHandlerResult.Completed();
    }
}
