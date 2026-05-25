// <copyright file="ProjectsTenantEventHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Workers.Tenants.TenantEventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Projects worker handler for Tenants lifecycle, membership, and configuration events.</summary>
public sealed class ProjectsTenantEventHandler(
    ProjectTenantAccessHandler handler,
    ProjectsTenantAccessEventMapper mapper,
    IOptions<ProjectTenantEventOptions> options,
    ILogger<ProjectsTenantEventHandler>? logger = null) :
    ITenantEventHandler<TenantCreated>,
    ITenantEventHandler<TenantUpdated>,
    ITenantEventHandler<TenantDisabled>,
    ITenantEventHandler<TenantEnabled>,
    ITenantEventHandler<UserAddedToTenant>,
    ITenantEventHandler<UserRemovedFromTenant>,
    ITenantEventHandler<UserRoleChanged>,
    ITenantEventHandler<TenantConfigurationSet>,
    ITenantEventHandler<TenantConfigurationRemoved>
{
    /// <inheritdoc/>
    public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(ToProjectionEvent(ProjectTenantAccessEventKind.TenantCreated, @event.TenantId, context, fingerprintParts: [@event.Name]), cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantUpdated @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(ToProjectionEvent(ProjectTenantAccessEventKind.TenantUpdated, @event.TenantId, context, fingerprintParts: [@event.Name]), cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantDisabled @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(ToProjectionEvent(ProjectTenantAccessEventKind.TenantDisabled, @event.TenantId, context), cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantEnabled @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(ToProjectionEvent(ProjectTenantAccessEventKind.TenantEnabled, @event.TenantId, context), cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(
            ToProjectionEvent(
                ProjectTenantAccessEventKind.UserAddedToTenant,
                @event.TenantId,
                context,
                principalId: @event.UserId,
                role: @event.Role.ToString(),
                fingerprintParts: [@event.UserId, @event.Role.ToString()]),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(UserRemovedFromTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(
            ToProjectionEvent(
                ProjectTenantAccessEventKind.UserRemovedFromTenant,
                @event.TenantId,
                context,
                principalId: @event.UserId,
                fingerprintParts: [@event.UserId]),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(UserRoleChanged @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(
            ToProjectionEvent(
                ProjectTenantAccessEventKind.UserRoleChanged,
                @event.TenantId,
                context,
                principalId: @event.UserId,
                role: @event.NewRole.ToString(),
                previousRole: @event.OldRole.ToString(),
                fingerprintParts: [@event.UserId, @event.OldRole.ToString(), @event.NewRole.ToString()]),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationSet @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(
            ToProjectionEvent(
                ProjectTenantAccessEventKind.TenantConfigurationSet,
                @event.TenantId,
                context,
                configurationKey: @event.Key,
                fingerprintParts: [@event.Key]),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationRemoved @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        return HandleAsync(
            ToProjectionEvent(
                ProjectTenantAccessEventKind.TenantConfigurationRemoved,
                @event.TenantId,
                context,
                configurationKey: @event.Key,
                fingerprintParts: [@event.Key]),
            cancellationToken);
    }

    private Task HandleAsync(ProjectTenantAccessEvent @event, CancellationToken cancellationToken)
    {
        if (options.Value.ProjectionWriter != ProjectTenantEventProjectionWriter.Workers)
        {
            logger?.LogDebug(
                "Skipping projects tenant-event projection in Workers host because ProjectionWriter={ProjectionWriter}.",
                options.Value.ProjectionWriter);
            return Task.CompletedTask;
        }

        return handler.HandleAsync(@event, cancellationToken);
    }

    private ProjectTenantAccessEvent ToProjectionEvent(
        ProjectTenantAccessEventKind kind,
        string eventTenantId,
        TenantEventContext context,
        string? principalId = null,
        string? role = null,
        string? previousRole = null,
        string? configurationKey = null,
        string?[]? fingerprintParts = null)
        => mapper.Map(
            kind,
            eventTenantId,
            context.TenantId,
            context.MessageId,
            context.SequenceNumber,
            context.Timestamp,
            context.CorrelationId,
            principalId,
            role,
            previousRole,
            configurationKey,
            fingerprintParts);
}
