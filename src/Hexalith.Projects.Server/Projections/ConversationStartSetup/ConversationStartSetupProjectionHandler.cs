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
    /// <summary>The named persisted read-model store backing this projection.</summary>
    internal const string StoreName = "projects-conversation-start-setup";
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

    /// <summary>Derives the persisted read-model key for a tenant-scoped project.</summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="projectId">The project identifier.</param>
    /// <returns>The store key.</returns>
    internal static string Key(string tenantId, string projectId) => $"{tenantId}:projects:{projectId}";

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

            IProjectEvent? projectEvent;
            try
            {
                projectEvent = JsonSerializer.Deserialize(eventDto.Payload, eventType, JsonOptions) as IProjectEvent;
            }
            catch (JsonException)
            {
                return DomainProjectionHandlerResult.Failed("invalid-event-payload");
            }

            if (projectEvent is null)
            {
                return DomainProjectionHandlerResult.Failed("invalid-event-payload");
            }

            envelopes.Add(new ProjectProjectionEnvelope(request.TenantId, eventDto.SequenceNumber, projectEvent));
        }

        string key = Key(request.TenantId, request.AggregateId);

        // Fold this slice onto whatever is already persisted (Seed), not from Empty (Rebuild) --
        // request.Events is an incremental slice, and folding from Empty would discard every field
        // established by earlier events that this slice does not repeat.
        ReadModelEntry<ProjectDetailItem> existing = await _readModelStore
            .GetAsync<ProjectDetailItem>(StoreName, key, cancellationToken)
            .ConfigureAwait(false);
        ProjectDetailProjection seeded = existing.Value is null
            ? ProjectDetailProjection.Empty
            : ProjectDetailProjection.Seed(existing.Value);
        ProjectDetailItem? detail = seeded.Apply(envelopes).Get(request.TenantId, request.AggregateId);
        if (detail is null)
        {
            return DomainProjectionHandlerResult.Completed();
        }

        await ReadModelWritePolicy.UpdateAsync<ProjectDetailItem>(
            _readModelStore,
            StoreName,
            key,
            current =>
            {
                if (current is null)
                {
                    return detail;
                }

                ProjectDetailItem? refolded = ProjectDetailProjection.Seed(current).Apply(envelopes).Get(request.TenantId, request.AggregateId);
                return refolded is null || current.Sequence >= refolded.Sequence ? current : refolded;
            },
            new ReadModelWriteContext("projection", ProjectionType, dispatchId),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return DomainProjectionHandlerResult.Completed();
    }
}
