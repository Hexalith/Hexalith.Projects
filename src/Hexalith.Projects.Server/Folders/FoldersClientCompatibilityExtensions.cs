// <copyright file="FoldersClientCompatibilityExtensions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;
using Hexalith.Folders.Client.Generated;

/// <summary>Bridges the supported Folders effective-permissions client signatures.</summary>
/// <remarks>Native instance overloads take precedence, so forwarding terminates at the resolved client API.</remarks>
internal static class FoldersClientCompatibilityExtensions
{
    /// <summary>Forwards to clients whose effective-permissions contract has no task identity.</summary>
    /// <param name="client">The resolved Folders client.</param>
    /// <param name="folderId">The folder whose permissions are requested.</param>
    /// <param name="correlationId">The request correlation identity.</param>
    /// <param name="freshness">The requested read consistency.</param>
    /// <param name="taskId">The task identity, carried by clients that support it natively.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The client's effective-permissions result.</returns>
    internal static Task<EffectivePermissions> GetEffectivePermissionsAsync(
        this FoldersClient client,
        string folderId,
        string correlationId,
        ReadConsistencyClass? freshness,
        string taskId,
        CancellationToken cancellationToken)
        => client.GetEffectivePermissionsAsync(folderId, correlationId, freshness, cancellationToken);

    /// <summary>Forwards to task-aware clients using the correlation identity as task identity.</summary>
    /// <param name="client">The resolved Folders client.</param>
    /// <param name="folderId">The folder whose permissions are requested.</param>
    /// <param name="correlationId">The request correlation and task identity.</param>
    /// <param name="freshness">The requested read consistency.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The client's effective-permissions result.</returns>
    internal static Task<EffectivePermissions> GetEffectivePermissionsAsync(
        this FoldersClient client,
        string folderId,
        string correlationId,
        ReadConsistencyClass? freshness,
        CancellationToken cancellationToken)
        => client.GetEffectivePermissionsAsync(folderId, correlationId, freshness, correlationId, cancellationToken);
}
