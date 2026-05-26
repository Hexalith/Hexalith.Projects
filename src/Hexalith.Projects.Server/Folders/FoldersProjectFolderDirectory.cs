// <copyright file="FoldersProjectFolderDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System;
using System.Linq;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;
using Hexalith.Folders.Client.Generated;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>Projects ACL adapter over the Folders generated lifecycle/effective-permissions client.</summary>
public sealed class FoldersProjectFolderDirectory(FoldersClient foldersClient) : IProjectFolderDirectory
{
    private readonly FoldersClient _foldersClient = foldersClient ?? throw new ArgumentNullException(nameof(foldersClient));

    /// <inheritdoc />
    public async Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
        ProjectId projectId,
        string folderId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);

        if (string.IsNullOrWhiteSpace(folderId) || string.IsNullOrWhiteSpace(correlationId))
        {
            return new(ProjectFolderValidationOutcome.ValidationFailed, correlationId);
        }

        try
        {
            FolderLifecycleStatus lifecycle = await _foldersClient
                .GetFolderLifecycleStatusAsync(
                    folderId,
                    correlationId,
                    ReadConsistencyClass.Eventually_consistent,
                    cancellationToken)
                .ConfigureAwait(false);

            ProjectFolderValidationOutcome lifecycleOutcome = ValidateLifecycle(folderId, lifecycle);
            if (lifecycleOutcome != ProjectFolderValidationOutcome.Accepted)
            {
                return new(lifecycleOutcome, correlationId);
            }

            EffectivePermissions permissions = await _foldersClient
                .GetEffectivePermissionsAsync(
                    folderId,
                    correlationId,
                    ReadConsistencyClass.Eventually_consistent,
                    cancellationToken)
                .ConfigureAwait(false);

            ProjectFolderValidationOutcome permissionOutcome = ValidatePermissions(folderId, permissions);
            return permissionOutcome == ProjectFolderValidationOutcome.Accepted
                ? ProjectFolderValidationResult.Accepted(correlationId)
                : new ProjectFolderValidationResult(permissionOutcome, correlationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithFoldersApiException ex)
        {
            return new(MapFoldersStatus(ex.StatusCode), correlationId);
        }
        catch (Exception)
        {
            return new(ProjectFolderValidationOutcome.Unavailable, correlationId);
        }
    }

    private static ProjectFolderValidationOutcome ValidateLifecycle(string folderId, FolderLifecycleStatus? lifecycle)
    {
        if (lifecycle is null || !string.Equals(lifecycle.FolderId, folderId, StringComparison.Ordinal))
        {
            return ProjectFolderValidationOutcome.Unavailable;
        }

        if (lifecycle.Freshness?.Stale == true)
        {
            return ProjectFolderValidationOutcome.Stale;
        }

        if (lifecycle.Archived
            || lifecycle.LifecycleState is LifecycleState.Failed
                or LifecycleState.Inaccessible
                or LifecycleState.Unknown_provider_outcome)
        {
            return ProjectFolderValidationOutcome.Archived;
        }

        return ProjectFolderValidationOutcome.Accepted;
    }

    private static ProjectFolderValidationOutcome ValidatePermissions(string folderId, EffectivePermissions? permissions)
    {
        if (permissions is null || !string.Equals(permissions.FolderId, folderId, StringComparison.Ordinal))
        {
            return ProjectFolderValidationOutcome.Unavailable;
        }

        if (permissions.Freshness?.Stale == true)
        {
            return ProjectFolderValidationOutcome.Stale;
        }

        if (permissions.AuthorizationOutcome != EffectivePermissionsAuthorizationOutcome.Allowed)
        {
            return ProjectFolderValidationOutcome.Denied;
        }

        bool hasUsablePermission = permissions.Permissions.Any(permission =>
            permission is FolderPermissionLevel.Read
                or FolderPermissionLevel.Write
                or FolderPermissionLevel.Administer);

        return hasUsablePermission
            ? ProjectFolderValidationOutcome.Accepted
            : ProjectFolderValidationOutcome.Denied;
    }

    private static ProjectFolderValidationOutcome MapFoldersStatus(int statusCode)
        => statusCode switch
        {
            400 => ProjectFolderValidationOutcome.ValidationFailed,
            401 or 403 or 404 => ProjectFolderValidationOutcome.Denied,
            409 => ProjectFolderValidationOutcome.Archived,
            503 => ProjectFolderValidationOutcome.Unavailable,
            >= 500 and < 600 => ProjectFolderValidationOutcome.Unavailable,
            _ => ProjectFolderValidationOutcome.Denied,
        };
}
