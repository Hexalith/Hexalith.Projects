// <copyright file="IProjectFolderProvisioningClient.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.SdkWriter.Workflow;

using System.Threading;
using System.Threading.Tasks;

using Hexalith.Folders.Server.Provisioning;

/// <summary>Adapter interface for create-only Folder provisioning, receipt lookup, and supplied-Folder validation.</summary>
public interface IProjectFolderProvisioningClient
{
    /// <summary>Gets the count of Folder delete attempts made by Projects (must always remain 0).</summary>
    int DeleteAttemptCount { get; }

    /// <summary>Provisions a new Folder using the 7.1-P2 create-only contract.</summary>
    /// <param name="tenantId">The owning Tenant identifier.</param>
    /// <param name="actorPrincipalId">The original actor principal identifier.</param>
    /// <param name="request">The scoped 7.1-P2 provisioning request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The provisioning result and security proof.</returns>
    Task<FolderProvisioningResult> ProvisionFolderAsync(
        string tenantId,
        string actorPrincipalId,
        FolderProvisioningRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Looks up the authoritative provisioning receipt for a previously attempted step.</summary>
    /// <param name="tenantId">The owning Tenant identifier.</param>
    /// <param name="actorPrincipalId">The original actor principal identifier.</param>
    /// <param name="request">The scoped 7.1-P2 provisioning request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative lookup result (<see cref="FolderProvisioningOutcome.Absent"/> when not yet created).</returns>
    Task<FolderProvisioningResult> LookupProvisioningAsync(
        string tenantId,
        string actorPrincipalId,
        FolderProvisioningRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Validates an existing actor-supplied Folder under current same-Tenant actor authority.</summary>
    /// <param name="tenantId">The owning Tenant identifier.</param>
    /// <param name="actorPrincipalId">The original actor principal identifier.</param>
    /// <param name="folderId">The supplied Folder identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result preserving the Folder's own display name.</returns>
    Task<FolderProvisioningResult> ValidateSuppliedFolderAsync(
        string tenantId,
        string actorPrincipalId,
        string folderId,
        CancellationToken cancellationToken = default);
}
#endif
