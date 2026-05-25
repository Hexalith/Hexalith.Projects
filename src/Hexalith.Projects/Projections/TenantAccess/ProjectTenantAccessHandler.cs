// <copyright file="ProjectTenantAccessHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Authorization;

using Microsoft.Extensions.Logging;

/// <summary>Applies normalized Tenants events to the local Projects tenant-access projection.</summary>
public sealed class ProjectTenantAccessHandler(
    IProjectTenantAccessProjectionStore store,
    IUtcClock clock,
    TenantAccessOptions options,
    ILogger<ProjectTenantAccessHandler>? logger = null)
{
    /// <summary>Applies one event using bounded optimistic-concurrency retries.</summary>
    public async Task HandleAsync(ProjectTenantAccessEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.TenantId))
        {
            return;
        }

        int attempts = Math.Max(1, options.ConcurrencyRetryAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                await ApplyOnceAsync(@event, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (TenantAccessConcurrencyException) when (attempt + 1 < attempts)
            {
                logger?.LogDebug(
                    "Optimistic concurrency conflict applying tenant event {EventKind} for tenant {TenantId}; retry {Attempt} of {Attempts}.",
                    @event.Kind,
                    @event.TenantId,
                    attempt + 2,
                    attempts);
            }
            catch (TenantAccessTransientPersistenceException) when (attempt + 1 < attempts)
            {
                logger?.LogDebug(
                    "Transient persistence failure applying tenant event {EventKind} for tenant {TenantId}; retry {Attempt} of {Attempts}.",
                    @event.Kind,
                    @event.TenantId,
                    attempt + 2,
                    attempts);
            }
            catch (TimeoutException) when (attempt + 1 < attempts)
            {
                logger?.LogDebug(
                    "Timeout applying tenant event {EventKind} for tenant {TenantId}; retry {Attempt} of {Attempts}.",
                    @event.Kind,
                    @event.TenantId,
                    attempt + 2,
                    attempts);
            }
        }
    }

    private async Task ApplyOnceAsync(ProjectTenantAccessEvent @event, CancellationToken cancellationToken)
    {
        ProjectTenantAccessProjection projection = await store.GetAsync(@event.TenantId, cancellationToken).ConfigureAwait(false)
            ?? new ProjectTenantAccessProjection { TenantId = @event.TenantId };

        if (IsMalformed(@event))
        {
            projection.MalformedEvidence = true;
            await store.SaveAsync(projection, cancellationToken).ConfigureAwait(false);
            return;
        }

        ProjectTenantEventEvidence evidence = CreateEvidence(@event);
        if (projection.ProcessedMessages.TryGetValue(@event.MessageId, out ProjectTenantEventEvidence? existing))
        {
            if (existing != evidence)
            {
                projection.ReplayConflict = true;
                await store.SaveAsync(projection, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (@event.SequenceNumber <= projection.Watermark)
        {
            logger?.LogDebug(
                "Dropping out-of-order tenant event {EventKind} for tenant {TenantId}: sequence {Sequence} <= watermark {Watermark}.",
                @event.Kind,
                @event.TenantId,
                @event.SequenceNumber,
                projection.Watermark);
            return;
        }

        Apply(projection, @event);
        projection.ProcessedMessages[@event.MessageId] = evidence;
        projection.Watermark = @event.SequenceNumber;
        projection.LastEventTimestamp = @event.Timestamp;
        projection.ProjectionWatermark = $"{@event.TenantId}:{@event.SequenceNumber}";

        await store.SaveAsync(projection, cancellationToken).ConfigureAwait(false);
    }

    private static void Apply(ProjectTenantAccessProjection projection, ProjectTenantAccessEvent @event)
    {
        switch (@event.Kind)
        {
            case ProjectTenantAccessEventKind.TenantCreated:
            case ProjectTenantAccessEventKind.TenantEnabled:
                projection.Enabled = true;
                break;
            case ProjectTenantAccessEventKind.TenantDisabled:
                projection.Enabled = false;
                break;
            case ProjectTenantAccessEventKind.UserAddedToTenant:
            case ProjectTenantAccessEventKind.UserRoleChanged:
                if (!string.IsNullOrWhiteSpace(@event.PrincipalId) && !string.IsNullOrWhiteSpace(@event.Role))
                {
                    projection.Principals[@event.PrincipalId] = new ProjectTenantPrincipalEvidence(@event.PrincipalId, @event.Role);
                }

                break;
            case ProjectTenantAccessEventKind.UserRemovedFromTenant:
                if (!string.IsNullOrWhiteSpace(@event.PrincipalId))
                {
                    _ = projection.Principals.Remove(@event.PrincipalId);
                }

                break;
            case ProjectTenantAccessEventKind.TenantConfigurationSet:
                AddConfigurationKey(projection, @event.ConfigurationKey);
                break;
            case ProjectTenantAccessEventKind.TenantConfigurationRemoved:
                RemoveConfigurationKey(projection, @event.ConfigurationKey);
                break;
            case ProjectTenantAccessEventKind.TenantUpdated:
            default:
                break;
        }
    }

    private static void AddConfigurationKey(ProjectTenantAccessProjection projection, string? key)
    {
        if (key is null || !IsProjectsConfigurationKey(key))
        {
            return;
        }

        _ = projection.ConfigurationKeys.Add(key);
        _ = projection.RemovedConfigurationKeys.Remove(key);
    }

    private static void RemoveConfigurationKey(ProjectTenantAccessProjection projection, string? key)
    {
        if (key is null || !IsProjectsConfigurationKey(key))
        {
            return;
        }

        _ = projection.ConfigurationKeys.Remove(key);
        _ = projection.RemovedConfigurationKeys.Add(key);
    }

    private bool IsMalformed(ProjectTenantAccessEvent @event)
        => string.IsNullOrWhiteSpace(@event.MessageId)
            || @event.SequenceNumber <= 0
            || @event.Timestamp - clock.UtcNow > options.ClockSkewTolerance
            || ((@event.Kind is ProjectTenantAccessEventKind.UserAddedToTenant
                or ProjectTenantAccessEventKind.UserRemovedFromTenant
                or ProjectTenantAccessEventKind.UserRoleChanged)
                && string.IsNullOrWhiteSpace(@event.PrincipalId));

    private static bool IsProjectsConfigurationKey(string key)
        => key.StartsWith("projects.", StringComparison.Ordinal);

    private static ProjectTenantEventEvidence CreateEvidence(ProjectTenantAccessEvent @event)
        => new(
            @event.MessageId,
            @event.TenantId,
            @event.Kind.ToString(),
            @event.SequenceNumber,
            @event.Timestamp,
            @event.PayloadFingerprint);
}
